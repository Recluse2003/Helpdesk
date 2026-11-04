using FluentValidation;
using MediatR;

namespace Helpdesk.Api.Shared.Behaviors
{
    /// <summary>
    /// A MediatR pipeline behaviour that validates requests before they reach their handlers.
    /// </summary>
    /// <typeparam name="TRequest">The type of request being validated.</typeparam>
    /// <typeparam name="TResponse">The type of response returned by the request handler.</typeparam>
    /// <remarks>
    /// Executes all FluentValidation validators registered for the incoming request. If validation fails, a 
    /// <see cref="ValidationException"/> is thrown containing the collected validation failures, preventing the request 
    /// handler from executing.
    /// </remarks>
    public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }

        /// <summary>
        /// Validates the request before calling the next behaviour or request handler.
        /// </summary>
        /// <param name="request">The request being executed.</param>
        /// <param name="next">The delegate that calls the next pipeline behaviour or request handler.</param>
        /// <param name="cancellationToken">A token used to cancel the asynchronous operation.</param>
        /// <returns>The response produced by the next pipeline behaviour or request handler.</returns>
        /// <exception cref="ValidationException">
        /// Thrown when one or more validators report validation errors.
        /// </exception>
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var context = new ValidationContext<TRequest>(request);

            var failures = (await Task.WhenAll(
                _validators.Select(validator =>
                    validator.ValidateAsync(context, cancellationToken))))
                .SelectMany(result => result.Errors)
                .Where(error => error is not null)
                .ToList();

            if (failures.Count != 0)
            {
                throw new ValidationException(failures);
            }

            return await next(cancellationToken);
        }
    }
}
