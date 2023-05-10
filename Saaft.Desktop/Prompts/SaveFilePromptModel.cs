namespace Saaft.Desktop.Prompts
{
    public sealed class SaveFilePromptModel
        : FilePromptModel
    {
        public SaveFilePromptModel(
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
