namespace Helpdesk.Api.Domain.Common
{
    /// <summary>
    /// Represents the types of errors that can occur within the application.
    /// </summary>
    public enum ErrorType
    {
        /// <summary>
        /// Represents an unexpected or general application failure.
        /// </summary>
        Failure = 0,

        /// <summary>
        /// Represents invalid input or data that fails validation.
        /// </summary>
        Validation = 1,

        /// <summary>
        /// Represents a resource that could not be found.
        /// </summary>
        NotFound = 2,

        /// <summary>
        /// Represents a conflict with the current state of a resource.
        /// </summary>
        Conflict = 3,

        /// <summary>
        /// Represents a request made by an unauthenticated user.
        /// </summary>
        Unauthorized = 4,

        /// <summary>
        /// Represents an authenticated user attempting an operation they are not permitted to perform.
        /// </summary>
        Forbidden = 5
    }

    /// <summary>
    /// Represents an application error containing an identifier, type and description.
    /// </summary>
    /// <param name="Id">A unique identifier used to distinguish the error.</param>
    /// <param name="Type">The type of the error.</param>
    /// <param name="Description">A human-readable description of the error.</param>
    public record Error(string Id, ErrorType Type, string Description)
    {
        /// <summary>
        /// Creates an error representing a general application failure.
        /// </summary>
        /// <param name="id">The error identifier.</param>
        /// <param name="description">A description of the failure.</param>
        /// <returns>A new error with the Failure type.</returns>
        public static Error Failure(string id, string description)
            => new(id, ErrorType.Failure, description);

        /// <summary>
        /// Creates an error representing invalid input or validation failure.
        /// </summary>
        /// <param name="id">The error identifier.</param>
        /// <param name="description">A description of the validation failure.</param>
        /// <returns>A new error with the Validation type.</returns>
        public static Error Validation(string id, string description)
            => new(id, ErrorType.Validation, description);

        /// <summary>
        /// Creates an error representing a resource that could not be found.
        /// </summary>
        /// <param name="id">The error identifier.</param>
        /// <param name="description">A description of the missing resource.</param>
        /// <returns>A new error with the NotFound type.</returns>
        public static Error NotFound(string id, string description)
            => new(id, ErrorType.NotFound, description);

        /// <summary>
        /// Creates an error representing a conflict with the current resource state.
        /// </summary>
        /// <param name="id">The error identifier.</param>
        /// <param name="description">A description of the conflict.</param>
        /// <returns>A new error with the Conflict type.</returns>
        public static Error Conflict(string id, string description)
            => new(id, ErrorType.Conflict, description);

        /// <summary>
        /// Creates an error representing an unauthenticated request.
        /// </summary>
        /// <param name="id">The error identifier.</param>
        /// <param name="description">A description of the authentication failure.</param>
        /// <returns>A new error with the Unauthorized type.</returns>
        public static Error Unauthorized(string id, string description)
            => new(id, ErrorType.Unauthorized, description);

        /// <summary>
        /// Creates an error representing an operation the user is not permitted to perform.
        /// </summary>
        /// <param name="id">The error identifier.</param>
        /// <param name="description">A description of the authorisation failure.</param>
        /// <returns>A new error with the Forbidden type.</returns>
        public static Error Forbidden(string id, string description)
            => new(id, ErrorType.Forbidden, description);
    }
}
