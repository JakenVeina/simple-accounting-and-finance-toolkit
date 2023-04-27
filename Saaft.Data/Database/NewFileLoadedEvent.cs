namespace Saaft.Data.Database
{
    public class NewFileLoadedEvent
        : DataStateEvent
    {
        public required FileEntity OldFile { get; init; }
    }
}
