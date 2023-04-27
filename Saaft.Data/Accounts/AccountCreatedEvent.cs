using Saaft.Data.Auditing;

namespace Saaft.Data.Accounts
{
    public class AccountCreatedEvent
        : AuditedActionEvent
    {
        public required VersionEntity Version { get; init; }
    }
}
