using System;

namespace Saaft.Data.Auditing
{
    public class AuditedActionEntity
    {
        public required ulong Id { get; init; }

        public required uint TypeId { get; init; }

        public required DateTimeOffset Performed { get; set; }
    }
}
