namespace Saaft.Desktop.Prompts
{
    public class FilePromptModel
        : ImperativePromptModel<string>
    {
        public FilePromptModel(
                string  title,
                string? initialFilePath,
                string  filter)
            : base(title)
        {
            _filter             = filter;
            _initialFilePath    = initialFilePath;
        }

        public string Filter
            => _filter;

        public string? InitialFilePath
            => _initialFilePath;

        private readonly string     _filter;
        private readonly string?    _initialFilePath;
    }
}
