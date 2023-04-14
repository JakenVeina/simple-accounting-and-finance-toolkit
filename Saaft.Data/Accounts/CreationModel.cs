namespace Saaft.Data.Accounts
{
    public record CreationModel
    {
        public long? ParentAccountId { get; set; }

        public string? Description { get; set; }

        public required string Name { get; set; }

        public required Type Type { get; set; }
    }
}
