using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Helpdesk.Api.Infrastructure.Middleware
{
    /// <summary>
    /// Handles unhandled FluentValidation exceptions and converts them into HTTP validation responses. 
    /// TODO: Other exceptions to be expected in general use are be added to the handler later. 
    /// </summary>
    /// <remarks>
    /// Validation errors are grouped by property name and returned using <see cref="ValidationProblemDetails"/> to provide 
    /// a consistent API error response. Exceptions that are not FluentValidation validation exceptions are not handled
    /// and are passed to subsequent exception handlers.
    /// </remarks>
    public sealed class GlobalExceptionHandler : IExceptionHandler
    {
        /// <summary>
        /// Attempts to handle an exception raised during HTTP request processing.
        /// </summary>
        /// <param name="httpContext">The current HTTP context.</param>
        /// <param name="exception">The exception that was thrown.</param>
        /// <param name="cancellationToken">A token used to cancel the asynchronous operation.</param>
        /// <returns>
        /// <see langword="true"/> if the exception was handled, otherwise, <see langword="false"/>.
        /// </returns>
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            if (exception is not ValidationException validationException)
            {
                return false;
            }

            var errors = validationException.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .Select(error => error.ErrorMessage)
                        .ToArray());

            var problem = new ValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred."
            };

            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

            await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);

            return true;
        }
    }
}
