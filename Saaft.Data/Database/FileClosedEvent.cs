namespace Saaft.Data.Database
{
    public class FileClosedEvent
        : FileStateEvent
    {
        public required FileEntity File { get; init; }
    }
}
