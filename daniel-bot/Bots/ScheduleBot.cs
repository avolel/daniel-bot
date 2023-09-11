using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using daniel_bot;
using Microsoft.Recognizers.Text.DateTime;
using Microsoft.Recognizers.Text.Number;
using Microsoft.Recognizers.Text;

namespace Bots
{
    public class ScheduleBot : ActivityHandler
    {
        protected readonly BotState conversationState;
        protected readonly BotState userState;

        public ScheduleBot(IConfiguration configuration, ConversationState ConversationState, UserState UserState)
        {
            conversationState = ConversationState;
            userState = UserState;
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
                        .AppendLine("Are you ready to book your cab to the airport?")
                        .AppendLine("Say 'yes' or 'get started' to begin. Or just say 'no' if you are not ready.");
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
                    if (ValidateYesNo(input, out string yn, out message))
                    {
                        if (yn.ToLower() == "yes" || yn.ToLower() == "get started" || yn.ToLower().Contains("get started") || yn.ToLower() == "i am ready" || yn.ToLower() == "i'm ready")
                        {
                            await turnContext.SendActivityAsync("Let's get started. What is your name?", null, null, cancellationToken);
                            flow.LastQuestionAsked = ConversationFlow.Question.Name;
                        }
                        else if (yn.ToLower() == "no" || yn.ToLower().Contains("no"))
                        {
                            await turnContext.SendActivityAsync("Ok! When you are ready to get started just say yes.", null, null, cancellationToken);
                            flow.LastQuestionAsked = ConversationFlow.Question.None;
                        }
                        else if (yn.ToLower() == "thanks" || yn.ToLower() == "thank you")
                        {
                            await turnContext.SendActivityAsync("Your Welcome", null, null, cancellationToken);
                            flow.LastQuestionAsked = ConversationFlow.Question.None;
                        }
                        else
                        {
                            await turnContext.SendActivityAsync("Remember! Say 'yes' or 'get started' to begin or just say 'no' if you aren't ready.", null, null, cancellationToken);
                            flow.LastQuestionAsked = ConversationFlow.Question.None;
                        }
                    }
                    break;
                case ConversationFlow.Question.Name:
                    if (ValidateName(input, out var name, out message))
                    {
                        profile.Name = name;
                        await turnContext.SendActivityAsync($"Hi {profile.Name}.", null, null, cancellationToken);
                        await turnContext.SendActivityAsync("How old are you?", null, null, cancellationToken);
                        flow.LastQuestionAsked = ConversationFlow.Question.Age;
                        break;
                    }
                    else
                    {
                        await turnContext.SendActivityAsync(message ?? "I'm sorry, I didn't understand that.", null, null, cancellationToken);
                        break;
                    }
                case ConversationFlow.Question.Age:
                    if (ValidateAge(input, out var age, out message))
                    {
                        profile.Age = age;
                        await turnContext.SendActivityAsync($"I have your age as {profile.Age}.", null, null, cancellationToken);
                        await turnContext.SendActivityAsync("When is your flight?", null, null, cancellationToken);
                        flow.LastQuestionAsked = ConversationFlow.Question.Date;
                        break;
                    }
                    else
                    {
                        await turnContext.SendActivityAsync(message ?? "I'm sorry, I didn't understand that.", null, null, cancellationToken);
                        break;
                    }
                case ConversationFlow.Question.Date:
                    if (ValidateDate(input, out var date, out message))
                    {
                        profile.Date = date;
                        await turnContext.SendActivityAsync($"Your cab ride to the airport is scheduled for {profile.Date}.");
                        await turnContext.SendActivityAsync($"Thanks for completing the booking {profile.Name}.");
                        await turnContext.SendActivityAsync($"Say yes to book a cab or no if you don't need a cab.");
                        flow.LastQuestionAsked = ConversationFlow.Question.None;
                        profile = new UserProfile();
                        break;
                    }
                    else
                    {
                        await turnContext.SendActivityAsync(message ?? "I'm sorry, I didn't understand that.", null, null, cancellationToken);
                        break;
                    }
            }
        }

        private static bool ValidateYesNo(string input, out string name, out string message)
        {
            name = null;
            message = null;

            if (string.IsNullOrWhiteSpace(input))
            {
                message = "Please say 'yes' or 'get started' to begin, Or just say 'no' if you are not ready?";
            }
            else
            {
                name = input.Trim();
            }
            return message is null;
        }

        private static bool ValidateName(string input, out string name, out string message)
        {
            name = null;
            message = null;

            if (string.IsNullOrWhiteSpace(input))
            {
                message = "Please enter a name that contains at least one character.";
            }
            else
            {
                name = input.Trim();
            }
            return message is null;
        }

        private static bool ValidateAge(string input, out int age, out string message)
        {
            age = 0;
            message = null;

            // Try to recognize the input as a number. This works for responses such as "twelve" as well as "12".
            try
            {
                // Attempt to convert the Recognizer result to an integer. This works for "a dozen", "twelve", "12", and so on.
                // The recognizer returns a list of potential recognition results, if any.
                var results = NumberRecognizer.RecognizeNumber(input, Culture.English);

                foreach (var result in results)
                {
                    // The result resolution is a dictionary, where the "value" entry contains the processed string.
                    if (result.Resolution.TryGetValue("value", out var value))
                    {
                        age = Convert.ToInt32(value);
                        if (age >= 18 && age <= 120)
                        {
                            return true;
                        }
                    }
                }
                message = "Please enter an age between 18 and 120.";
            }
            catch
            {
                message = "I'm sorry, I could not interpret that as an age. Please enter an age between 18 and 120.";
            }
            return message is null;
        }

        private static bool ValidateDate(string input, out string date, out string message)
        {
            date = null;
            message = null;

            // Try to recognize the input as a date-time. This works for responses such as "11/14/2018", "9pm", "tomorrow", "Sunday at 5pm", and so on.
            // The recognizer returns a list of potential recognition results, if any.
            try
            {
                var results = DateTimeRecognizer.RecognizeDateTime(input, Culture.English);

                // Check whether any of the recognized date-times are appropriate,
                // and if so, return the first appropriate date-time. We're checking for a value at least an hour in the future.
                var earliest = DateTime.Now.AddHours(1.0);

                foreach (var result in results)
                {
                    // The result resolution is a dictionary, where the "values" entry contains the processed input.
                    var resolutions = result.Resolution["values"] as List<Dictionary<string, string>>;

                    foreach (var resolution in resolutions)
                    {
                        // The processed input contains a "value" entry if it is a date-time value, or "start" and
                        // "end" entries if it is a date-time range.
                        if (resolution.TryGetValue("value", out var dateString)
                            || resolution.TryGetValue("start", out dateString))
                        {
                            if (DateTime.TryParse(dateString, out var candidate)
                                && earliest < candidate)
                            {
                                date = candidate.ToShortDateString();
                                return true;
                            }
                        }
                    }
                }
                message = "I'm sorry, please enter a date at least an hour out.";
            }
            catch
            {
                message = "I'm sorry, I could not interpret that as an appropriate date. Please enter a date at least an hour out.";
            }
            return false;
        }
    }
}