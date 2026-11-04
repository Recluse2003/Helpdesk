namespace Helpdesk.Api.Domain.Common
{
    /// <summary>
    /// Represents the outcome of an operation that may succeed or fail.
    /// </summary>
    /// <remarks>
    /// A failed result contains an <see cref="Helpdesk.Api.Domain.Common.Error"/> describing the failure.
    /// A successful result does not contain an error.
    /// </remarks>
    public record Result
    {
        /// <summary>
        /// Gets a value indicating whether the operation completed successfully.
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// Gets the error associated with the operation, or <see langword="null"/> if it succeeded.
        /// </summary>
        public Error? Error { get; }

        /// <summary>
        /// Initialises a new instance of the <see cref="Result"/> class.
        /// </summary>
        /// <param name="isSuccess">Indicates whether the operation succeeded.</param>
        /// <param name="error">The error associated with a failed operation, if any.</param>
        protected Result(bool isSuccess, Error? error)
        {
            IsSuccess = isSuccess;
            Error = error;
        }

        /// <summary>
        /// Creates a successful result without a return value.
        /// </summary>
        /// <returns>A successful result.</returns>
        public static Result Success() => new(true, null);

        /// <summary>
        /// Creates a failed result containing the specified error.
        /// </summary>
        /// <param name="error">The error describing why the operation failed.</param>
        /// <returns>A failed result.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="error"/> is <see langword="null"/>.
        /// </exception>
        public static Result Failure(Error error) =>
            new(false, error ?? throw new ArgumentNullException(nameof(error)));

        /// <summary>
        /// Converts an <see cref="Error"/> into a failed <see cref="Result"/>.
        /// </summary>
        /// <param name="error">The error describing the failure.</param>
        public static implicit operator Result(Error error) => Failure(error);
    }

    /// <summary>
    /// Represents the outcome of an operation that may succeed with a value or fail with an error.
    /// </summary>
    /// <typeparam name="T">The type of value returned when the operation succeeds.</typeparam>
    /// <remarks>
    /// A successful result contains a value of type <typeparamref name="T"/>.
    /// A failed result contains an <see cref="Error"/> and does not provide a meaningful value.
    /// </remarks>
    public record Result<T> : Result
    {
        /// <summary>
        /// Gets the value returned by a successful operation.
        /// </summary>
        /// <remarks>
        /// The value should only be accessed when <see cref="Result.IsSuccess"/> is <see langword="true"/>.
        /// </remarks>
        public T Value { get; }

        /// <summary>
        /// Initialises a successful result containing the specified value.
        /// </summary>
        /// <param name="value">The value returned by the operation.</param>
        private Result(T value) : base(true, null) => Value = value;

        /// <summary>
        /// Initialises a failed result containing the specified error.
        /// </summary>
        /// <param name="error">The error describing the failure.</param>
        private Result(Error error) : base(false, error) => Value = default!;

        /// <summary>
        /// Converts a value into a successful <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="value">The value returned by the successful operation.</param>
        public static implicit operator Result<T>(T value) => new(value);

        /// <summary>
        /// Converts an error into a failed <see cref="Result{T}"/>.
        /// </summary>
        /// <param name="error">The error describing the failure.</param>
        public static implicit operator Result<T>(Error error) => new(error);
    }
}