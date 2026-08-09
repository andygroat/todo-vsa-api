using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Todo.VSA.Api.Infrastructure.ResultHelper;
using Todo.VSA.DataAccess.Context;

namespace Todo.VSA.Api.Features.Todos;

/// <summary>
/// GetTodoById feature slice. Encapsulates the query, validator, response DTO, handler, and
/// endpoint mapping used to retrieve a single Todo item by its identifier, following the
/// Vertical Slice Architecture.
/// </summary>
public static class GetTodoById
{
    /// <summary>
    /// Query record representing the request to fetch a single Todo item by its Id.
    /// Implements <see cref="IRequest{TResponse}"/> so it can be dispatched via MediatR.
    /// </summary>
    /// <param name="Id">The unique identifier of the Todo item to retrieve.</param>
    public record Query(Guid Id) : IRequest<Result<Response>>;

    /// <summary>
    /// Response DTO returned for a single Todo item. Kept local to the slice to decouple
    /// the API contract from the underlying domain entity.
    /// </summary>
    /// <param name="Id">The unique identifier of the Todo item.</param>
    /// <param name="Description">The description of the Todo item.</param>
    /// <param name="DueDate">The due date of the Todo item, if any.</param>
    /// <param name="IsCompleted">Whether the Todo item has been completed.</param>
    public record Response(Guid Id, string Description, DateTime? DueDate, bool IsCompleted);

    /// <summary>
    /// Validator for the <see cref="Query"/>. Ensures the supplied Id is not the empty Guid.
    /// Executed automatically by the ValidationBehavior MediatR pipeline.
    /// </summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }

    /// <summary>
    /// Handler that processes the <see cref="Query"/> and returns the matching Todo item,
    /// or a not-found <see cref="Result{T}"/> if no Todo item with the specified Id exists.
    /// </summary>
    /// <param name="context">The database context used to query Todo items.</param>
    /// <param name="logger">The logger used to record diagnostic information.</param>
    internal sealed class Handler(TodoDbContext context, ILogger<Handler> logger)
        : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            // Project directly into the response DTO for an efficient read-only query.
            var todo = await context.TodoItems
                .Where(t => t.Id == request.Id)
                .Select(t => new Response(t.Id, t.Description, t.DueDate, t.IsCompleted))
                .SingleOrDefaultAsync(cancellationToken);

            if (todo is null)
            {
                logger.LogInformation("TodoItem {TodoItemId} was not found", request.Id);
                return Result.Failure<Response>(new Error("TodoItem.NotFound", $"TodoItem with Id '{request.Id}' was not found."));
            }

            logger.LogInformation("Retrieved TodoItem {TodoItemId}", todo.Id);
            return Result<Response>.Success(todo);
        }
    }

    /// <summary>
    /// Maps the GetTodoById query to an HTTP GET endpoint at "/api/todos/{id:guid}".
    /// </summary>
    /// <param name="app">The WebApplication instance used to map the endpoint.</param>
    public static WebApplication MapGetTodoByIdEndpoint(this WebApplication app)
    {
        app.MapGet("/api/todos/{id:guid}", async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
        {
            Result<Response> result = await mediator.Send(new Query(id), cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { error = result.Error });
        })
        .WithName("GetTodoById")
        .WithTags("todos");

        return app;
    }
}
