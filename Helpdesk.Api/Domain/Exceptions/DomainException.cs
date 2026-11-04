namespace Helpdesk.Api.Domain.Exceptions
{
    /// <summary>
    /// Represents errors that occur in the domain layer that were not caught by the handlers and validators. 
    /// </summary>
    public sealed class DomainException : Exception
    {
        public DomainException(string message)
            : base(message)
        {
        }
    }
}
