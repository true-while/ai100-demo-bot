using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace Microsoft.BotBuilderSamples.Responses
{
    public class MainResponses
    {
  
        public static async Task ReplyWithGreeting(ITurnContext context, Func<string, string> translate)
        {
            // Add a greeting
            await context.SendActivityAsync(translate.Invoke($"Hi, I'm PictureBot!"));
        }

        public static async Task ReplyWithLanguage(ITurnContext context, string lang)
        {
            await context.SendActivityAsync($"Your language has been detected as '{lang}'");
        }

        public static async Task ReplyThumbnailCard(ITurnContext context)
        {
            Activity replyToConversation = context.Activity.CreateReply("Your language has beed detected");
            replyToConversation.AttachmentLayout = AttachmentLayoutTypes.List;
            replyToConversation.Attachments = new List<Attachment>();

            Dictionary<string, string> cardContentList = new Dictionary<string, string>();
            cardContentList.Add("Lisp", "https://en.wikipedia.org/wiki/Lisp_(programming_language)#/media/File:Lisplogo.png");
            cardContentList.Add("Java", "https://en.wikipedia.org/wiki/Java_(programming_language)#/media/File:Java_programming_language_logo.svg");
            cardContentList.Add("Python", "https://en.wikipedia.org/wiki/Python_(programming_language)#/media/File:Python_logo_and_wordmark.svg");

            foreach (KeyValuePair<string, string> cardContent in cardContentList)
            {
                List<CardImage> cardImages = new List<CardImage>();
                cardImages.Add(new CardImage(url: cardContent.Value));

                List<CardAction> cardButtons = new List<CardAction>();

                CardAction plButton = new CardAction()
                {
                    Value = $"https://en.wikipedia.org/wiki/{cardContent.Key}",
                    Type = "openUrl",
                    Title = "WikiPedia Page"
                };

                cardButtons.Add(plButton);

                ThumbnailCard plCard = new ThumbnailCard()
                {
                    Title = $"I'm a thumbnail card about {cardContent.Key}",
                    Subtitle = $"{cardContent.Key} Wikipedia Page",
                    Images = cardImages,
                    Buttons = cardButtons
                };

                Attachment plAttachment = plCard.ToAttachment();
                replyToConversation.Attachments.Add(plAttachment);
            }

           await context.SendActivityAsync(replyToConversation);

        }
        public static async Task ReplyWithReceiptCard(ITurnContext context)
        {
            Activity replyToConversation = context.Activity.CreateReply("Thank you for your order");
            replyToConversation.Attachments = new List<Attachment>();

            List<CardImage> cardImages = new List<CardImage>();
            cardImages.Add(new CardImage(url: "https://en.wikipedia.org/wiki/Pizza#/media/File:Eq_it-na_pizza-margherita_sep2005_sml.jpg"));

            List<CardAction> cardButtons = new List<CardAction>();

            CardAction plButton = new CardAction()
            {
                Value = $"https://en.wikipedia.org/wiki/Pizza",
                Type = "openUrl",
                Title = "WikiPedia Page"
            };

            cardButtons.Add(plButton);

            ReceiptItem lineItem1 = new ReceiptItem()
            {
                Title = "Pizza margherita",
                Subtitle = "1 large",
                Text = null,
                Image = new CardImage(url: "https://en.wikipedia.org/wiki/Pizza_Margherita#/media/File:Eataly_Las_Vegas_-_Feb_2019_-_Stierch_12.jpg"),
                Price = "16.25",
                Quantity = "1",
                Tap = null
            };

            ReceiptItem lineItem2 = new ReceiptItem()
            {
                Title = "Soda",
                Subtitle = "3 glass",
                Text = null,
                Image = new CardImage(url: "https://en.wikipedia.org/wiki/Carbonated_water#/media/File:Drinking_glass_00118.gif"),
                Price = "2.99",
                Quantity = "3",
                Tap = null
            };

            List<ReceiptItem> receiptList = new List<ReceiptItem>();
            receiptList.Add(lineItem1);
            receiptList.Add(lineItem2);

            ReceiptCard plCard = new ReceiptCard()
            {
                Title = "I'm a receipt card, did you like our pizza?",
                Buttons = cardButtons,
                Items = receiptList,
                Total = "112.77",
                Tax = "27.52"
            };

            Attachment plAttachment = plCard.ToAttachment();
            replyToConversation.Attachments.Add(plAttachment);

            await context.SendActivityAsync(replyToConversation);
        }

        public static async Task ReplyWithCatWithTie(ITurnContext context)
        {
            var reply = context.Activity.CreateReply("The card with Image");
            var attachment = new Attachment
            {
                ContentUrl = "https://aka.ms/catwithtie",
                ContentType = "image/jpeg",
                Name = "cat with a tie",
            };
            reply.Attachments = new List<Attachment>() { attachment };
            await context.SendActivityAsync(reply);
        }
        public static async Task ReplyWithRichCard(ITurnContext context)
        {
            Activity replyToConversation = context.Activity.CreateReply("This card should go to conversation");
            replyToConversation.Attachments = new List<Attachment>();

            AdaptiveCard card = new AdaptiveCard();

            // Specify speech for the card.
            card.Speak = "<s>Your  meeting about \"Adaptive Card design session\"<break strength='weak'/> is starting at 12:30pm</s><s>Do you want to snooze <break strength='weak'/> or do you want to send a late notification to the attendees?</s>";

            // Add text to the card.
            card.Body.Add(new AdaptiveTextBlock()
            {
                Text = "Adaptive Card design session",
                Size = AdaptiveTextSize.Large,
                Weight = AdaptiveTextWeight.Bolder
            });

            // Add text to the card.
            card.Body.Add(new AdaptiveTextBlock()
            {
                Text = "Conf Room 112/3377 (10)"
            });

            // Add text to the card.
            card.Body.Add(new AdaptiveTextBlock()
            {
                Text = "12:30 PM - 1:30 PM"
            });

            // Add list of choices to the card.
            card.Body.Add(new AdaptiveChoiceSetInput()
            {
                Id = "snooze",
                Style = AdaptiveChoiceInputStyle.Compact,
                Choices = new List<AdaptiveChoice>()
    {
        new AdaptiveChoice() { Title = "5 minutes", Value = "5", IsSelected = true },
        new AdaptiveChoice() { Title = "15 minutes", Value = "15" },
        new AdaptiveChoice() { Title = "30 minutes", Value = "30" }
                }
            });

            // Add buttons to the card.
            card.Actions.Add(new AdaptiveOpenUrlAction()
            {
                Url = new Uri("http://foo.com"),
                Title = "Snooze"
            });

            card.Actions.Add(new AdaptiveOpenUrlAction()
            {
                Url = new Uri("http://foo.com"),
                Title = "I'll be late"
            });

            card.Actions.Add(new AdaptiveOpenUrlAction()
            {
                Url = new Uri("http://foo.com"),
                Title = "Dismiss"
            });

            // Create the attachment.
            Attachment attachment = new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = card
            };

            replyToConversation.Attachments.Add(attachment);

            await context.SendActivityAsync(replyToConversation);
        }
        public static async Task ReplyWithHelp(ITurnContext context, Func<string, string> translate)
        {
            await context.SendActivityAsync(translate.Invoke($"I can search for pictures, share pictures and order prints of pictures."));
        }
        public static async Task ReplyWithResumeTopic(ITurnContext context, Func<string, string> translate)
        {
            await context.SendActivityAsync(translate.Invoke($"What can I do for you?"));
        }

        public static async Task ReplyWithSentiment(ITurnContext context,  Func<string, decimal> detectSentiment)
        {
            await context.SendActivityAsync($"Sentiment: ({detectSentiment.Invoke(context.Activity.Text)}).");
        }
        public static async Task ReplyWithConfused(ITurnContext context, Func<string, string> translate) 
        {
            // Add a response for the user if Regex or LUIS doesn't know
            // What the user is trying to communicate
            await context.SendActivityAsync(translate.Invoke($"I'm sorry, I don't understand."));
        }
        public static async Task ReplyWithLuisScore(ITurnContext context, string key, double score)
        {
            await context.SendActivityAsync($"Intent: {key} ({score}).");
        }
        public static async Task ReplyWithShareConfirmation(ITurnContext context, Func<string, string> translate)
        {
            await context.SendActivityAsync(translate.Invoke($"Posting your picture(s) on twitter..."));
        }
        public static async Task ReplyWithOrderConfirmation(ITurnContext context, Func<string, string> translate)
        {
            await context.SendActivityAsync(translate.Invoke($"Ordering standard prints of your picture(s)..."));
        }
        public static async Task ReplyWithSearchingConfirmation(ITurnContext context, Func<string, string> translate)
        {
            await context.SendActivityAsync(translate.Invoke($"I'm searching for your picture(s)..."));
        }
    }
}