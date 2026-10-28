namespace Helpdesk.Domain.Enums
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
