namespace daniel_bot
{
    public class ConversationFlow
    {
        public enum Question
        {
            Plate,
            None
        }

        public Question LastQuestionAsked { get; set; } = Question.None;
    }
}
