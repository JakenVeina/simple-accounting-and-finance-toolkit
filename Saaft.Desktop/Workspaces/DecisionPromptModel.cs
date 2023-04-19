namespace Saaft.Desktop.Workspaces
{
    public class DecisionPromptModel
        : PromptModelBase<bool>
    {
        public required string Message { get; init; }

        public required string Title { get; init; }
    }
}
