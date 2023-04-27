using Saaft.Data.Auditing;

namespace Saaft.Data.Accounts
{
    public class AccountMutatedEvent
        : AuditedActionEvent
    {
        public required VersionEntity NewVersion { get; init; }
        
        public required VersionEntity OldVersion { get; init; }
    }
}
