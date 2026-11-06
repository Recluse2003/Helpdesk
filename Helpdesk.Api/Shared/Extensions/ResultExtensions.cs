using Helpdesk.Api.Domain.Common;

namespace Helpdesk.Api.Shared.Extensions
{
    /// <summary>
    /// Provides extension methods for converting application results into HTTP responses.
    /// </summary>
    public static class ResultExtensions
    {
        /// <summary>
        /// Converts an application result into an appropriate HTTP response.
        /// </summary>
        /// <param name="result">The result to convert.</param>
        /// <returns>
        /// An HTTP 200 response if the operation succeeded, or a problem response
        /// containing the corresponding status code and error details if it failed.
        /// </returns>
        public static IResult ToHttpResult(this Result result)
        {
            if (result.IsSuccess)
                return Results.Ok();

            return CreateProblem(result.Error!);
        }

        /// <summary>
        /// Converts an application result containing a value into an appropriate HTTP response.
        /// </summary>
        /// <typeparam name="T">The type of value returned by the operation.</typeparam>
        /// <param name="result">The result to convert.</param>
        /// <returns>
        /// An HTTP 200 response containing the result value if the operation succeeded,
        /// or a problem response containing the corresponding status code and error details
        /// if it failed.
        /// </returns>
        public static IResult ToHttpResult<T>(this Result<T> result)
        {
            if (result.IsSuccess)
                return Results.Ok(result.Value);

            return CreateProblem(result.Error!);
        }

        /// <summary>
        /// Converts a successful result containing a value into an HTTP 201 Created response.
        /// </summary>
        /// <typeparam name="T">The type of value returned by the operation.</typeparam>
        /// <param name="result">The result containing the created resource.</param>
        /// <param name="location">The URI of the newly created resource.</param>
        /// <returns>
        /// An HTTP 201 Created response if successful, or the appropriate problem response if it failed.
        /// </returns>
        public static IResult ToCreatedResult<T>(this Result<T> result, string location)
        {
            if (!result.IsSuccess)
                return CreateProblem(result.Error!);

            return Results.Created(location, result.Value);
        }

        /// <summary>
        /// Converts a result into an HTTP 204 No Content response when successful.
        /// </summary>
        /// <param name="result">The operation result.</param>
        /// <returns>
        /// An HTTP 204 No Content response if successful; otherwise, the appropriate error response.
        /// </returns>
        public static IResult ToNoContentResult(this Result result)
        {
            if (result.IsSuccess)
                return Results.NoContent();

            return CreateProblem(result.Error!);
        }

        private static IResult CreateProblem(Error error)
        {
            var statusCode = error.Type switch
            {
                ErrorType.Validation => StatusCodes.Status400BadRequest,
                ErrorType.NotFound => StatusCodes.Status404NotFound,
                ErrorType.Conflict => StatusCodes.Status409Conflict,
                ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
                ErrorType.Forbidden => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status500InternalServerError
            };

            return Results.Problem(
                statusCode: statusCode,
                title: error.Id,
                detail: error.Description);
        }
    }
}
