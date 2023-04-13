namespace Saaft.Data.Database
{
    public record FileEntity
    {
        public static readonly FileEntity New
            = new()
            { 
                Database = Entity.Empty
            };

        public required Entity Database { get; init; }

        public string? FilePath { get; init; }
    }
}
