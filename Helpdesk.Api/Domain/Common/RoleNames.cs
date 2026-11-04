namespace Helpdesk.Api.Domain.Common
{
    /// <summary>
    /// Defines the application role names used for user authorisation.
    /// </summary>
    /// <remarks>
    /// These constants should be used wherever role names are referenced to avoid duplicated string literals and 
    /// inconsistent role naming.
    /// </remarks>
    public static class RoleNames
    {
        /// <summary>
        /// Represents a customer who can create and manage their own support tickets.
        /// </summary>
        public const string Customer = "Customer";

        /// <summary>
        /// Represents a support agent who can work on and respond to assigned customer tickets.
        /// </summary>
        public const string SupportAgent = "SupportAgent";

        /// <summary>
        /// Represents an administrator who has elevated permissions to manage users, tickets, assignments and escalation
        /// requests.
        /// </summary>
        public const string Administrator = "Administrator";
    }
}
