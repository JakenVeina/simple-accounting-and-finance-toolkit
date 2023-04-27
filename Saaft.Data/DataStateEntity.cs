namespace Saaft.Data
{
    public record DataStateEntity
    {
        public static readonly DataStateEntity Default
            = new()
            {
                LatestEvent = DataStateEvent.None,
                LoadedFile  = Database.FileEntity.None
            };

        public required DataStateEvent LatestEvent { get; init; }

        public required Database.FileEntity LoadedFile { get; init; }
    }
}
