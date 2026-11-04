namespace Helpdesk.Api.Domain.Enums
{
    /// <summary>
    /// Defines the types of events that can be recorded in a ticket's activity history.
    /// </summary>
    public enum TicketActivityType
    {
        /// <summary>
        /// The ticket was created.
        /// </summary>
        Created,

        /// <summary>
        /// The ticket was assigned to a support agent. 
        /// </summary>
        Assigned,

        /// <summary>
        /// The ticket was unassigned from its current support agent.
        /// </summary>
        Unassigned,

        /// <summary>
        /// The ticket's status was changed.
        /// </summary>
        StatusChanged,

        /// <summary>
        /// The ticket's priority was changed.
        /// </summary>
        PriorityChanged,

        /// <summary>
        /// An escalation request was submitted for the ticket.
        /// </summary>
        EscalationRequested,

        /// <summary>
        /// An escalation request was approved.
        /// </summary>
        EscalationAccepted,

        /// <summary>
        /// An escalation request was declined.
        /// </summary>
        EscalationRejected
    }
}
