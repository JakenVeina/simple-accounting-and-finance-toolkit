namespace Saaft.Desktop.Prompts
{
    public sealed class DecisionPromptModel
        : ImperativePromptModel<bool>
    {
        public DecisionPromptModel(
                    string title,
                    string message)
                : base(title)
            => _message = message;

        public string Message
            => _message;

        private readonly string _message;
    }
}
