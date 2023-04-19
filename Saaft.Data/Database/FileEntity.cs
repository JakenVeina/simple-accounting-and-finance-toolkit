namespace Saaft.Data.Database
{
    public record FileEntity
    {
        public const string DefaultFilename
            = "Untitled.saaft";

        public static readonly FileEntity New
            = new()
            { 
                Database    = Entity.Empty,
                HasChanges  = true
            };

        public required Entity Database { get; init; }

        public string? FilePath { get; init; }

        public bool HasChanges { get; init; }
    }
}
