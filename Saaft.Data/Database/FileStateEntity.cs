namespace Saaft.Data.Database
{
    public record FileStateEntity
        : StateEntity<FileStateEvent>
    {
        public static readonly FileStateEntity Default
            = new()
            {
                LatestEvent = FileStateEvent.None,
                LoadedFile  = FileEntity.None
            };

        public required FileEntity LoadedFile { get; init; }
    }
}
