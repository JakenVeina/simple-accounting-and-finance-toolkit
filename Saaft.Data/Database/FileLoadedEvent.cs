namespace Saaft.Data.Database
{
    public class FileLoadedEvent
        : FileStateEvent
    {
        public required FileEntity NewFile { get; init; }
        
        public required FileEntity OldFile { get; init; }
    }
}
