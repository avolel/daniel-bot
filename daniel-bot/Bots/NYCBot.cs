using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using daniel_bot;
//using Microsoft.Recognizers.Text.DateTime;
//using Microsoft.Recognizers.Text.Number;
//using Microsoft.Recognizers.Text;
using daniel_bot.Model;
using daniel_bot.DataService;
using System.Linq;

namespace Bots
{
    public class NYCBot : ActivityHandler
    {
        protected readonly BotState conversationState;
        protected readonly BotState userState;

        private static BotDataService DataService { get; set; }

        public NYCBot(IConfiguration configuration, ConversationState ConversationState, UserState UserState, BotDataService dataService)
        {
            conversationState = ConversationState;
            userState = UserState;
            DataService = dataService;
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            await base.OnTurnAsync(turnContext, cancellationToken);
            await conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await userState.SaveChangesAsync(turnContext,false, cancellationToken);
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var conversationStateAccessors = conversationState.CreateProperty<ConversationFlow>(nameof(ConversationFlow));
            var flow = await conversationStateAccessors.GetAsync(turnContext, () => new ConversationFlow(), cancellationToken);

            var userStateAccessors = userState.CreateProperty<UserProfile>(nameof(UserProfile));
            var profile = await userStateAccessors.GetAsync(turnContext, () => new UserProfile(), cancellationToken);

            await FillOutUserProfileAsync(flow, profile, turnContext, cancellationToken);

            //Save Changes
            await conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await userState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    StringBuilder sb = new StringBuilder();

                    sb.AppendLine($"{Greeting()}")
                        .AppendLine("My name is Daniel.")
                        .AppendLine("I can look up any Parking and Camera Violations tickets in NYC for you.")
                        .AppendLine("Are you ready to begin?");
                    await turnContext.SendActivityAsync(MessageFactory.Text(sb.ToString()), cancellationToken);
                }
            }
        }

        private string Greeting()
        {
            DateTime day = DateTime.Now;
            string greeting = string.Empty;

            if (day.Hour >= 5 && day.Hour < 12)
                greeting = "Good morning!";
            else if (day.Hour >= 12 && day.Hour <= 14)
                greeting = "Good afternoon!";
            else if (day.Hour >= 15 && day.Hour <= 17)
                greeting = "Good evening!";
            else
                greeting = "Good night!";
            return greeting;
        }

        private static async Task FillOutUserProfileAsync(ConversationFlow flow, UserProfile profile, ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var input = turnContext.Activity.Text?.Trim();
            string message;

            switch (flow.LastQuestionAsked)
            {
                case ConversationFlow.Question.None:
                    if (input.ToLower() == "yes")
                    {
                        await turnContext.SendActivityAsync("What is the license plate you want me to look up?", null, null, cancellationToken);
                        flow.LastQuestionAsked = ConversationFlow.Question.Plate;
                    }
                    else if (input.ToLower() == "no")
                    {
                        await turnContext.SendActivityAsync("Ok no problem. When you are ready to begin just enter the license plate you want me to look up for you?", null, null, cancellationToken);
                        flow.LastQuestionAsked = ConversationFlow.Question.Plate;
                    }
                    else
                    {
                        await turnContext.SendActivityAsync("Either say Yes or No.", null, null, cancellationToken);
                        flow.LastQuestionAsked = ConversationFlow.Question.None;
                    }
                    break;
                case ConversationFlow.Question.Plate:
                    if (ValidatePlate(input, out var plate, out message))
                    {
                        profile.Plate = plate;
                        await turnContext.SendActivityAsync($"Let me look up {profile.Plate} for you.", null, null, cancellationToken);
                        string messagefound =  await DataService.LookUpPlateInfoAsync(plate.ToUpper());
                        await turnContext.SendActivityAsync(messagefound, null, null, cancellationToken);
                        await turnContext.SendActivityAsync("Enter another license plate for me to lookup?", null, null, cancellationToken);
                        flow.LastQuestionAsked = ConversationFlow.Question.Plate;
                        break;
                    }
                    else
                    {
                        await turnContext.SendActivityAsync(message ?? "I'm sorry, I didn't understand that.", null, null, cancellationToken);
                        break;
                    }              
            }
        }

        private static bool ValidatePlate(string input, out string plate, out string message)
        {
            plate = null;
            message = null;

            if (string.IsNullOrWhiteSpace(input))
            {
                message = "Please enter a plate number that contains at least one character.";
            }
            else
            {
                plate = input.Trim();
            }
            return message is null;
        }        
    }
}