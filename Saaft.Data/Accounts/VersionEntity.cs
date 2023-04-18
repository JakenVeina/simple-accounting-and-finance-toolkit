namespace Saaft.Data.Accounts
{
    public record VersionEntity
    {
        public required ulong Id { get; init; }

        public required ulong AccountId { get; init; }

        public required ulong CreationId { get; init; }

        public ulong? PreviousVersionId { get; init; }

        public ulong? ParentAccountId { get; set; }

        public string? Description { get; set; }

        public required string Name { get; set; }

        public required Type Type { get; set; }
    }
}
