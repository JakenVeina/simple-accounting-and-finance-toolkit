namespace Saaft.Data.Auditing
{
    public class AuditedActionEvent
        : DataStateEvent
    {
        public required AuditedActionEntity Action { get; init; }
    }
}
