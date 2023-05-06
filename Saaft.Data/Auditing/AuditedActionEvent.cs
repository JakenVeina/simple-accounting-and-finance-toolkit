using Saaft.Data.Database;

namespace Saaft.Data.Auditing
{
    public class AuditedActionEvent
        : FileStateEvent
    {
        public required AuditedActionEntity Action { get; init; }
    }
}
