namespace Helpdesk.Api.Domain.Enums
{
    public enum TicketActivityType
    {
        Created,
        Assigned,
        Unassigned,
        StatusChanged,
        PriorityChanged,
        EscalationRequested,
        EscalationAccepted,
        EscalationRejected
    }
}
