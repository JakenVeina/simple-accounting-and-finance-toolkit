namespace Saaft.Data.Database
{
    public class FileClosedEvent
        : DataStateEvent
    {
        public required FileEntity File { get; init; }
    }
}
