namespace Saaft.Data.Database
{
    public class NewFileLoadedEvent
        : FileStateEvent
    {
        public required FileEntity OldFile { get; init; }
    }
}
