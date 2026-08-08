using FluentValidation;
using MediatR;

namespace Todo.VSA.Api.Infrastructure.Behaviours;

/// <summary>
/// A pipeline behavior that validates requests using FluentValidation.
/// </summary>
/// <param name="validators">The collection of validators for the request type.</param>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public class ValidationBehavior<TRequest, TResponse> (IEnumerable<IValidator<TRequest>> validators) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Handles the validation of the request and invokes the next delegate in the pipeline.
    /// </summary>
    /// <param name="request">The request to validate.</param>
    /// <param name="next">The next delegate in the pipeline.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>The response from the next delegate in the pipeline.</returns>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken token)
    {
        // If there are no validators for the request type, continue to the next delegate in the pipeline.
        if (!validators.Any())
            return await next(token);

        // Create a validation (FluentValidation) context for the request and validate it using all registered validators.
        var context = new ValidationContext<TRequest>(request);
        
        // Asynchronously validate the request using all registered validators.
        var validationTasks = validators.Select(v => v.ValidateAsync(context, token))
                                        .ToList();
        // Wait for all validation tasks to complete.
        await Task.WhenAll(validationTasks);
        var failures = validationTasks.SelectMany(t => t.Result.Errors)
                                           .Where(f => f != null)
                                           .ToList();

        // If there are any validation failures, throw a ValidationException.
        if (failures.Any())
            throw new ValidationException(failures);
        
        // Validation passes, continue to the next delegate in the pipeline.
        return await next(token);
    }
}
