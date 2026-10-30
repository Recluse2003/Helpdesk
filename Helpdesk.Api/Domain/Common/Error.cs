namespace Helpdesk.Api.Domain.Common
{
    public enum ErrorType
    {
        Failure = 0,
        Validation = 1,
        NotFound = 2,
        Conflict = 3,
        Unauthorized = 4
    }

    public record Error(string Id, ErrorType Type, string Description)
    {
        public static Error Failure(string id, string description)
            => new(id, ErrorType.Failure, description);

        public static Error Validation(string id, string description)
            => new(id, ErrorType.Validation, description);

        public static Error NotFound(string id, string description)
            => new(id, ErrorType.NotFound, description);

        public static Error Conflict(string id, string description)
            => new(id, ErrorType.Conflict, description);

        public static Error Unauthorized(string id, string description)
            => new(id, ErrorType.Unauthorized, description);
    }
}
