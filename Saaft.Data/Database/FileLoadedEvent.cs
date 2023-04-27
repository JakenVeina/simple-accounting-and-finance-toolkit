namespace Saaft.Data.Database
{
    public class FileLoadedEvent
        : DataStateEvent
    {
        public required FileEntity NewFile { get; init; }
        
        public required FileEntity OldFile { get; init; }
    }
}
