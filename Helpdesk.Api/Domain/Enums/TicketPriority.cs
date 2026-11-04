namespace Helpdesk.Api.Domain.Enums
{
    /// <summary>
    /// Defines the priority levels that can be assigned to a support ticket.
    /// </summary>
    public enum TicketPriority
    {
        /// <summary>
        /// A low priority issue that requires attention but isn't immediately important. 
        /// </summary>
        Low,

        /// <summary>
        /// A standard issue requiring attention within normal support procedures.
        /// </summary>
        Medium,

        /// <summary>
        /// An important issue requiring a quick response. 
        /// </summary>
        High,

        /// <summary>
        /// A critical issue requiring immediate response.
        /// </summary>
        Urgent
    }
}
