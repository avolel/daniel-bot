namespace daniel_bot
{
    public class ConversationFlow
    {
        public enum Question
        {
            Name,
            Age,
            Date,
            None
        }

        public Question LastQuestionAsked { get; set; } = Question.None;
    }
}
