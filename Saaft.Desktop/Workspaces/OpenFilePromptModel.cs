namespace Saaft.Desktop.Workspaces
{
    public sealed class OpenFilePromptModel
        : PromptModelBase<string>
    {
        public required string Filter { get; init; }

        public string? InitialFilePath { get; init; }
    }
}
