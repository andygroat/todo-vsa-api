using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Todo.VSA.Api.Infrastructure.Exceptions;

/// <summary>
/// Exception handler for handling unhandled exceptions in the application. This class implements the IExceptionHandler interface and provides logic to handle unhandled exceptions, log them, and return a standardized error response to the client.
/// </summary>
/// <param name="problemDetailsService">The service used to write problem details to the HTTP response.</param>
/// <param name="logger">The logger used to log unhandled exceptions.</param>
internal sealed class GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    /// <inheritdoc/>
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // Log the exception as an error for debugging and monitoring purposes.
        logger.LogError(exception, "An unhandled exception occurred");

        // Set the response status code to 500 Internal Server Error, indicating that an unexpected error occurred on the server.
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        // Create a ProblemDetailsContext to provide detailed information about the error in the response. Problem details will standardize the error response format for clients, complying with RFC 9457, https://www.rfc-editor.org/info/rfc9457/.
        var context = new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Detail = "An unexpected error occurred",
                Status = StatusCodes.Status500InternalServerError
            }
        };

        return await problemDetailsService.TryWriteAsync(context);
    }
}
