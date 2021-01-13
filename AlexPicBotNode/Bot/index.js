// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const dotenv = require('dotenv');
const path = require('path');
const restify = require('restify');
const { CosmosDbPartitionedStorage } = require('botbuilder-azure');

const { PicBotDialogSet } = require('./picBotDialogSet');
const { RegExpRecognizerSettings, RegExpRecognizerMiddleware } = require('./regExp');
const { PicBotLuisRecognizer } = require('./picBotLuisRecognizer');
const { PicBotTextAnalytics } = require('./picBotTextAnalytics');

// Import required bot services.
// See https://aka.ms/bot-services to learn more about the different parts of a bot.
const { BotFrameworkAdapter, ConversationState, UserState } = require('botbuilder');

// This bot's main dialog.
const { EchoBot } = require('./bot');

// Import required bot configuration.
const ENV_FILE = path.join(__dirname, '.env');
dotenv.config({ path: ENV_FILE });

// Create HTTP server
const server = restify.createServer();
server.listen(process.env.port || process.env.PORT || 3978, () => {
    console.log(`\n${ server.name } listening to ${ server.url }`);
    console.log('\nGet Bot Framework Emulator: https://aka.ms/botframework-emulator');
    console.log('\nTo talk to your bot, open the emulator select "Open Bot"');
});

// Create adapter.
// See https://aka.ms/about-bot-adapter to learn more about how bots work.
const adapter = new BotFrameworkAdapter({
    appId: process.env.MicrosoftAppId,
    appPassword: process.env.MicrosoftAppPassword,
    channelService: process.env.ChannelService,
    openIdMetadata: process.env.BotOpenIdMetadata
});

// Catch-all for errors.
adapter.onTurnError = async (context, error) => {
    // This check writes out errors to console log .vs. app insights.
    // NOTE: In production environment, you should consider logging this to Azure
    //       application insights.
    console.error(`\n [onTurnError] unhandled error: ${ error }`);
    console.error(`${ error.stack }`);

    // Send a trace activity, which will be displayed in Bot Framework Emulator
    await context.sendTraceActivity(
        'OnTurnError Trace',
        `${ error }`,
        'https://www.botframework.com/schemas/error',
        'TurnError'
    );

    // Send a message to the user
    await context.sendActivity('The bot encounted an error or bug.');
    await context.sendActivity('To continue to run this bot, please fix the bot source code.');
};

// Define state store for your bot.
// See https://aka.ms/about-bot-state to learn more about bot state.

var blobStorage = new CosmosDbPartitionedStorage({
    cosmosDbEndpoint: process.env.DB_SERVICE_ENDPOINT,
    authKey: process.env.AUTH_KEY,
    databaseId: process.env.DATABASE_ID,
    containerId: process.env.CONTAINER
});

// Create conversation and user state with in-memory storage provider.
const userState = new UserState(blobStorage);
const conversationState = new ConversationState(blobStorage);

var settingsRegExp = new RegExpRecognizerSettings();
var middleware = new RegExpRecognizerMiddleware(settingsRegExp);
middleware.addIntent('search', new RegExp('search pictures?(.*)|search pics?(.*)', 'i'));
middleware.addIntent('share', new RegExp('share pictures?(.*)|share pics?(.*)', 'i'));
middleware.addIntent('order', new RegExp('order pictures?(.*)|order prints?(.*)|order pics?(.*)', 'i'));
middleware.addIntent('help', new RegExp('help(.*)', 'i'));

adapter.use(middleware);

const luisConfig = {
    applicationId: process.env.LuisAppId,
    endpointKey: process.env.LuisAPIKey,
    endpoint: process.env.LuisAPIHostName
};
const luisRecognizer = new PicBotLuisRecognizer(luisConfig);

const textAnalyticsConfig = {
    endpointKey: process.env.TextAnalyticsKey,
    endpoint: process.env.TextAnalyticsHostName
};
const textAnalytics = new PicBotTextAnalytics(textAnalyticsConfig);

// Create the main dialog.
const dialogSet = new PicBotDialogSet(conversationState, luisRecognizer, textAnalytics);
const myBot = new EchoBot(conversationState, userState, dialogSet);

// Listen for incoming requests.
server.post('/api/messages', (req, res) => {
    adapter.processActivity(req, res, async (context) => {
        // Route to main dialog.
        await myBot.run(context);
    });
});
