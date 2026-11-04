namespace Helpdesk.Api.Domain.Enums
{
    /// <summary>
    /// Defines the possible lifecycle states of a support ticket.
    /// </summary>
    public enum TicketStatus
    {
        /// <summary>
        /// The ticket has been submitted but work has not yet started.
        /// </summary>
        Open,

        /// <summary>
        /// A support agent is actively working on the ticket.
        /// </summary>
        InProgress,

        /// <summary>
        /// The support agent needs additional information or a response from the customer to continue. 
        /// </summary>
        AwaitingCustomer,

        /// <summary>
        /// The support agent believes the issue has been resolved but is waiting to see if the customer agrees. 
        /// </summary>
        Resolved,

        /// <summary>
        /// The ticket has been closed and no further work is expected.
        /// </summary>
        Closed
    }
}
