namespace Saaft.Desktop.Prompts
{
    public sealed class OpenFilePromptModel
        : FilePromptModel
    {
        public OpenFilePromptModel(
                string  title,
                string? initialFilePath,
                string  filter)
            : base(
                title,
                initialFilePath,
                filter)
        { }
    }
}
