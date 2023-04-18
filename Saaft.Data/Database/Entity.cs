using System.Collections.Immutable;
using System.Linq;

namespace Saaft.Data.Database
{
    public record Entity
    {
        public static readonly Entity Empty
            = new()
            {
                AccountVersions             = ImmutableList.Create<Accounts.VersionEntity>(),
                AuditingActionCategories    = ImmutableArray.Create(Accounts.Auditing.ActionCategory),
                AuditingActions             = ImmutableList.Create<Auditing.AuditedActionEntity>(),
                AuditingActionTypes         = Enumerable.Empty<Auditing.AuditedActionTypeEntity>()
                    .Concat(Accounts.Auditing.ActionTypes.Enumerate())
                    .ToImmutableArray()
            };

        public required ImmutableList<Accounts.VersionEntity> AccountVersions { get; init; }

        public required ImmutableArray<Auditing.AuditedActionCategoryEntity> AuditingActionCategories { get; init; }

        public required ImmutableList<Auditing.AuditedActionEntity> AuditingActions { get; init; }

        public required ImmutableArray<Auditing.AuditedActionTypeEntity> AuditingActionTypes { get; init; }
    }
}
