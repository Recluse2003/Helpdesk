namespace Helpdesk.Domain.Enums
{
    public enum TicketActivityType
    {
        Created,
        Assigned,
        Unassigned,
        StatusChanged,
        PriorityChanged,
        MessageAdded,
        EscalationRequested,
        EscalationAccepted,
        EscalationRejected
    }
}
