namespace Saaft.Data.Accounts
{
    public record CreationModel
    {
        public ulong? ParentAccountId { get; init; }

        public string? Description { get; init; }

        public required string Name { get; init; }

        public required Type Type { get; init; }
    }
}
