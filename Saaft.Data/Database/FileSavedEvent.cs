namespace Saaft.Data.Database
{
    public class FileSavedEvent
        : DataStateEvent
    {
        public required string NewFilePath { get; init; }
        
        public required string? OldFilePath { get; init; }
    }
}
