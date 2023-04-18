namespace Saaft.Data.Accounts
{
    public record MutationModel
    {
        public ulong AccountId { get; set; }

        public ulong? ParentAccountId { get; set; }

        public string? Description { get; set; }

        public required string Name { get; set; }

        public required Type Type { get; set; }
    }
}
