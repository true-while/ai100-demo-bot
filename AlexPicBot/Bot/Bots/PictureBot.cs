// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AlexPicBot.Translator;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.BotBuilderSamples.Middleware;
using Microsoft.BotBuilderSamples.Models;
using Microsoft.BotBuilderSamples.Responses;
using Microsoft.Extensions.Logging;

namespace Microsoft.BotBuilderSamples.Bots
{
    public class PictureBot : ActivityHandler
    {

        private readonly PictureBotAccessors _accessors;
        // Initialize LUIS Recognizer
        private LuisRecognizer _recognizer { get; } = null;

        private readonly ILogger _logger;
        private DialogSet _dialogs;

        private Translator _translator;
        private Sentiments _sentiments;
        private string _language = "en";
        private Func<string, string> _translateDetectDelegate;
        private Func<string, string> _translateDelegate;
        private Func<string, decimal> _sentimentsDelegate;

        /// <summary>
        /// Initializes a new instance of the <see cref="PictureBot"/> class.
        /// </summary>
        /// <param name="accessors">A class containing <see cref="IStatePropertyAccessor{T}"/> used to manage state.</param>
        /// <param name="loggerFactory">A <see cref="ILoggerFactory"/> that is hooked to the Azure App Service provider.</param>
        /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-2.1#windows-eventlog-provider"/>
        public PictureBot(PictureBotAccessors accessors, 
            ILoggerFactory loggerFactory, 
            LuisRecognizer recognizer, 
            Translator translator,
            Sentiments sentiments)
        {
            _translator = translator;
            _sentiments = sentiments;
            if (loggerFactory == null)
            {
                throw new System.ArgumentNullException(nameof(loggerFactory));
            }

            _translateDetectDelegate = (input) => _translator.Detect(input);
            _translateDelegate = (input) => _translator.Translate(input, "en", _language);
            _sentimentsDelegate = (input) => _sentiments.Detect(input);

            // Lab 2.2.3 Add instance of LUIS Recognizer
            _recognizer = recognizer ?? throw new ArgumentNullException(nameof(recognizer));

            _logger = loggerFactory.CreateLogger<PictureBot>();
            _logger.LogTrace("PictureBot turn start.");
            _accessors = accessors ?? throw new System.ArgumentNullException(nameof(accessors));

            // The DialogSet needs a DialogState accessor, it will call it when it has a turn context.
            _dialogs = new DialogSet(_accessors.DialogStateAccessor);

            // This array defines how the Waterfall will execute.
            // We can define the different dialogs and their steps here
            // allowing for overlap as needed. In this case, it's fairly simple
            // but in more complex scenarios, you may want to separate out the different
            // dialogs into different files.
            var main_waterfallsteps = new WaterfallStep[]
            {
                GreetingAsync,
                MainMenuAsync,
            };
            var search_waterfallsteps = new WaterfallStep[]
            {
                // Add SearchDialog water fall steps

            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            _dialogs.Add(new WaterfallDialog("mainDialog", main_waterfallsteps));
            _dialogs.Add(new WaterfallDialog("searchDialog", search_waterfallsteps));
            // The following line allows us to use a prompt within the dialogs
            _dialogs.Add(new TextPrompt("searchPrompt"));
        }

        /// <summary>
        /// Every conversation turn for our PictureBot will call this method.
        /// There are no dialogs used, since it's "single turn" processing, meaning a single
        /// request and response. Later, when we add Dialogs, we'll have to navigate through this method.
        /// </summary>
        /// <param name="turnContext">A <see cref="ITurnContext"/> containing all the data needed
        /// for processing this conversation turn. </param>
        /// <param name="cancellationToken">(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> that represents the work queued to execute.</returns>
        /// <seealso cref="BotStateSet"/>
        /// <seealso cref="ConversationState"/>
        /// <seealso cref="IMiddleware"/>
        public override async Task OnTurnAsync(ITurnContext turnContext, 
            CancellationToken cancellationToken = default(CancellationToken))
        {
            // If the user sends us a message
            if (turnContext.Activity.Type is "message")
            {
                // Establish dialog context from the conversation state.
                var dc = await _dialogs.CreateContextAsync(turnContext);
                // Continue any current dialog.
                var results = await dc.ContinueDialogAsync(cancellationToken);

                // Every turn sends a response, so if no response was sent,
                // then there no dialog is currently active.
                if (!turnContext.Responded)
                {
                    // Start the main dialog
                    await dc.BeginDialogAsync("mainDialog", null, cancellationToken);
                }

             }

            await base.OnTurnAsync(turnContext, cancellationToken);

            await _accessors.ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await _accessors.UserState.SaveChangesAsync(turnContext, false, cancellationToken);

        }


        // Add MainDialog-related tasks
        // If we haven't greeted a user yet, we want to do that first, but for the rest of the
        // conversation we want to remember that we've already greeted them.
        private async Task<DialogTurnResult> GreetingAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Get the state for the current step in the conversation
            var state = await _accessors.PictureState.GetAsync(stepContext.Context, () => new PictureState());

            // If we haven't greeted the user
            if (state.Greeted == "not greeted")
            {
                _language = _translator.Detect(stepContext.Context.Activity.Text);
                if (_language != "en")
                {
                    string translated = _translator.Translate(stepContext.Context.Activity.Text, _language, "en");
                    stepContext.Context.Activity.Text = translated;
                }
                // Greet the user
                await MainResponses.ReplyWithGreeting(stepContext.Context, _translateDelegate);
                // Update the GreetedState to greeted
                state.Greeted = "greeted";
                // Save the new greeted state into the conversation state
                // This is to ensure in future turns we do not greet the user again
                await _accessors.ConversationState.SaveChangesAsync(stepContext.Context);
                // Ask the user what they want to do next
                await MainResponses.ReplyWithHelp(stepContext.Context, _translateDelegate);
                // Since we aren't explicitly prompting the user in this step, we'll end the dialog
                // When the user replies, since state is maintained, the else clause will move them
                // to the next waterfall step
                return await stepContext.EndDialogAsync();
            }
            else // We've already greeted the user
            {
                // Move to the next waterfall step, which is MainMenuAsync
                return await stepContext.NextAsync();
            }

        }

        // This step routes the user to different dialogs
        // In this case, there's only one other dialog, so it is more simple,
        // but in more complex scenarios you can go off to other dialogs in a similar
        public async Task<DialogTurnResult> MainMenuAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            //detect user language
            _language = _translator.Detect(stepContext.Context.Activity.Text);
            if (_language != "en")
            {
                string translated = _translator.Translate(stepContext.Context.Activity.Text, _language, "en");
                stepContext.Context.Activity.Text = translated;
            }

            // Check if we are currently processing a user's search
            var state = await _accessors.PictureState.GetAsync(stepContext.Context);

            // If Regex picks up on anything, store it
            var recognizedIntents = stepContext.Context.TurnState.Get<IRecognizedIntents>();
            // Based on the recognized intent, direct the conversation
            switch (recognizedIntents.TopIntent?.Name)
            {
                case "lang":
                    var userStateAccessors = _accessors.UserState.CreateProperty<UserProfile>(nameof(UserProfile));
                    var userProfile = await userStateAccessors.GetAsync(stepContext.Context, () => new UserProfile());
                    await MainResponses.ReplyWithLanguage(stepContext.Context, userProfile.Language);
                    return await stepContext.EndDialogAsync();
                case "pizza":
                    await MainResponses.ReplyWithReceiptCard(stepContext.Context);
                    return await stepContext.EndDialogAsync();
                case "ai-102":
                    await MainResponses.ReplyWithHeroCard(stepContext.Context);
                    return await stepContext.EndDialogAsync();
                case "thumb":
                    await MainResponses.ReplyThumbnailCard(stepContext.Context);
                    return await stepContext.EndDialogAsync();
                case "rich card":
                    await MainResponses.ReplyWithRichCard(stepContext.Context);
                    return await stepContext.EndDialogAsync();
                case "card":
                    await MainResponses.ReplyWithCatWithTie(stepContext.Context);
                    return await stepContext.EndDialogAsync();
                case "search":
                    // switch to the search dialog
                    return await stepContext.BeginDialogAsync("searchDialog", null, cancellationToken);
                case "share":
                    // respond that you're sharing the photo
                    await MainResponses.ReplyWithShareConfirmation(stepContext.Context, _translateDelegate);
                    return await stepContext.EndDialogAsync();
                case "order":
                    // respond that you're ordering
                    await MainResponses.ReplyWithOrderConfirmation(stepContext.Context, _translateDelegate);
                    return await stepContext.EndDialogAsync();
                case "help":
                    // show help
                    await MainResponses.ReplyWithHelp(stepContext.Context, _translateDelegate);
                    return await stepContext.EndDialogAsync();
                default:
                    {
                        // Call LUIS recognizer
                        var result = await _recognizer.RecognizeAsync(stepContext.Context, cancellationToken);
                        // Get the top intent from the results
                        var topIntent = result?.GetTopScoringIntent();
                        // Based on the intent, switch the conversation, similar concept as with Regex above
                        switch ((topIntent != null) ? topIntent.Value.intent : null)
                        {
                            case null:
                                // Add app logic when there is no result.
                                await MainResponses.ReplyWithConfused(stepContext.Context, _translateDelegate);
                                break;
                            case "None":
                                await MainResponses.ReplyWithConfused(stepContext.Context, _translateDelegate);
                                // with each statement, we're adding the LuisScore, purely to test, so we know whether LUIS was called or not
                                await MainResponses.ReplyWithLuisScore(stepContext.Context, topIntent.Value.intent, topIntent.Value.score);
                                break;
                            case "Greeting":
                                await MainResponses.ReplyWithGreeting(stepContext.Context, _translateDelegate);
                                await MainResponses.ReplyWithHelp(stepContext.Context, _translateDelegate);
                                await MainResponses.ReplyWithLuisScore(stepContext.Context, topIntent.Value.intent, topIntent.Value.score);
                                break;
                            case "OrderPic":
                                await MainResponses.ReplyWithOrderConfirmation(stepContext.Context, _translateDelegate);
                                await MainResponses.ReplyWithLuisScore(stepContext.Context, topIntent.Value.intent, topIntent.Value.score);
                                break;
                            case "SharePic":
                                await MainResponses.ReplyWithShareConfirmation(stepContext.Context, _translateDelegate);
                                await MainResponses.ReplyWithLuisScore(stepContext.Context, topIntent.Value.intent, topIntent.Value.score);
                                break;
                            case "SearchPic":
                                await MainResponses.ReplyWithSearchingConfirmation(stepContext.Context, _translateDelegate);
                                await MainResponses.ReplyWithLuisScore(stepContext.Context, topIntent.Value.intent, topIntent.Value.score);
                                break;
                            default:
                                
                                await MainResponses.ReplyWithConfused(stepContext.Context, _translateDelegate);
                                break;
                        }

                        await MainResponses.ReplyWithSentiment(stepContext.Context, _sentimentsDelegate);

                        return await stepContext.EndDialogAsync();
                    }
            }


        }
        // Add SearchDialog-related tasks


        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var conversationStateAccessors = _accessors.ConversationState.CreateProperty<ConversationData>(nameof(ConversationData));
            var conversationData = await conversationStateAccessors.GetAsync(turnContext, () => new ConversationData());

            var userStateAccessors = _accessors.UserState.CreateProperty<UserProfile>(nameof(UserProfile));
            var userProfile = await userStateAccessors.GetAsync(turnContext, () => new UserProfile());
            
            //log user's Utterance
            userProfile.UtteranceList.Add(turnContext.Activity.Text);

            //detect user language
            userProfile.Language = _language;

            //log conversation info
            var messageTimeOffset = (DateTimeOffset)turnContext.Activity.Timestamp;
            var localMessageTime = messageTimeOffset.ToLocalTime();
            conversationData.ChannelId = turnContext.Activity.ChannelId.ToString();
            conversationData.Timestamp = localMessageTime.ToString();
            conversationData.PromptedUserForName = false;
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text($"Hello and welcome!"), cancellationToken);
                }
            }
        }
    }
}
