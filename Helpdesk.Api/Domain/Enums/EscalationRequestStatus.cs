namespace Helpdesk.Api.Domain.Enums
{
    /// <summary>
    /// Defines the possible states of a ticket escalation request. 
    /// </summary>
    public enum EscalationRequestStatus
    {
        /// <summary>
        /// The escalation request has been submitted and is awaiting an administrator's decision.
        /// </summary>
        Waiting,

        /// <summary>
        /// The escalation request has been approved by an administrator.
        /// </summary>
        Accepted,

        /// <summary>
        /// The escalation request has been declined by an administrator.
        /// </summary>
        Rejected
    }
}
