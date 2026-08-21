using FluentValidation;
using MediatR;

namespace Todo.Vsa.Api.Infrastructure.Behaviours;

/// <summary>
/// A pipeline behavior that logs the handling of requests and responses.
/// </summary>
/// <param name="logger">The logger instance for logging request and response handling.</param>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public class LoggingBehavior<TRequest, TResponse> (ILogger<LoggingBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Handles the request and logs the handling process.
    /// </summary>
    /// <param name="request">The request to handle.</param>
    /// <param name="next">The next delegate in the pipeline.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response from the next delegate in the pipeline.</returns>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Log the start of the request handling
        logger.LogInformation("Starting request '{RequestName}'", typeof(TRequest).Name);

        // Call the next delegate in the pipeline to handle the request
        var response = await next(cancellationToken);

        // Log the completion of the request handling
        logger.LogInformation("Completed request '{RequestName}'", typeof(TRequest).Name);

        return response;
    }
}
