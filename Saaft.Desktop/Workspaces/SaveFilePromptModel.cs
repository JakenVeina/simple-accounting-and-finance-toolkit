namespace Saaft.Desktop.Workspaces
{
    public class SaveFilePromptModel
        : PromptModelBase<string>
    {
        public required string Filter { get; init; }

        public string? InitialFilePath { get; init; }
    }
}
