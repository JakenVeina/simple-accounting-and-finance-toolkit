namespace Saaft.Data.Accounts
{
    public record VersionEntity
    {
        public required long Id { get; init; }

        public required long AccountId { get; init; }

        public long? PreviousVersionId { get; init; }

        public long? ParentAccountId { get; set; }

        public string? Description { get; set; }

        public required string Name { get; set; }

        public required Type Type { get; set; }
    }
}
