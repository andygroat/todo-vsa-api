using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Todo.Vsa.Api.Infrastructure.Exceptions;

/// <summary>
/// Exception handler for handling validation exceptions (FluentValidation) in the application. This class implements the IExceptionHandler interface and provides logic to handle validation exceptions,
/// </summary>
/// <param name="problemDetailsService">The service used to create and write problem details responses.</param>
/// <param name="logger">The logger used to log validation exceptions.</param>
internal sealed class ValidationExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<ValidationExceptionHandler> logger) : IExceptionHandler
{
    /// <inheritdoc/>
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // Check if the exception is a ValidationException, and if not, return false to indicate that this handler cannot handle the exception.
        if (exception is not ValidationException validationException)
            return false;

        // Log the validation exception as an error for debugging and monitoring purposes.
        logger.LogError(exception, "Validation exception occurred");

        // Set the response status code to 400 Bad Request, indicating that the request was invalid due to validation errors.
        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        // Create a ProblemDetailsContext to provide detailed information about the validation errors in the response. Problem details will standardize the error response format for clients, complying with RFC 9457, https://www.rfc-editor.org/info/rfc9457/.
        var context = new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Detail = "One or more validation errors occurred",
                Status = StatusCodes.Status400BadRequest
            }
        };
        
        // Extract validation errors and group them by property name.
        var errors = validationException.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key.ToLowerInvariant(),
                g => g.Select(e => e.ErrorMessage).ToArray()
            );
        // Add the validation errors to the ProblemDetails extensions, allowing clients to access detailed information about the validation failures.
        context.ProblemDetails.Extensions.Add("errors", errors);

        return await problemDetailsService.TryWriteAsync(context);
    }
}
