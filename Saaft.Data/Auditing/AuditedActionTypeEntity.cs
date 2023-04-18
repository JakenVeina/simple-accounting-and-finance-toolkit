namespace Saaft.Data.Auditing
{
    public class AuditedActionTypeEntity
    {
        public required uint Id { get; init; }

        public required uint CategoryId { get; init; }

        public required string Name { get; init; }
    }
}
