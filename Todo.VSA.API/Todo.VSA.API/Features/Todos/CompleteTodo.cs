using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Todo.Vsa.Api.Infrastructure.ResultHelper;
using Todo.Vsa.DataAccess.Context;

namespace Todo.Vsa.Api.Features.Todos;

/// <summary>
/// CompleteTodo feature slice. Encapsulates the command, validator, handler, and endpoint
/// mapping used to mark an existing Todo item as completed, following the
/// Vertical Slice Architecture.
/// </summary>
public static class CompleteTodo
{
    /// <summary>
    /// Command record representing the request to complete a Todo item.
    /// Implements <see cref="IRequest{TResponse}"/> so it can be dispatched via MediatR.
    /// </summary>
    /// <param name="Id">The unique identifier of the Todo item to complete.</param>
    public record Command(Guid Id) : IRequest<Result<Guid>>;

    /// <summary>
    /// Validator for the <see cref="Command"/>. Ensures the supplied Id is not the empty Guid.
    /// Executed automatically by the ValidationBehavior MediatR pipeline.
    /// </summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }

    /// <summary>
    /// Handler that processes the <see cref="Command"/> by locating the specified Todo item
    /// and marking it as completed. Returns a failure result if the Todo item does not exist
    /// or is already completed.
    /// </summary>
    /// <param name="context">The database context used to interact with Todo items.</param>
    /// <param name="logger">The logger used to record diagnostic information.</param>
    internal sealed class Handler(TodoDbContext context, ILogger<Handler> logger)
        : IRequestHandler<Command, Result<Guid>>
    {
        public async Task<Result<Guid>> Handle(Command request, CancellationToken cancellationToken)
        {
            // Load the tracked TodoItem so we can mutate and persist changes.
            var todoItem = await context.TodoItems
                .SingleOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

            if (todoItem is null)
            {
                logger.LogInformation("TodoItem {TodoItemId} was not found", request.Id);
                return Result.Failure<Guid>(new Error(
                    "TodoItem.NotFound",
                    $"TodoItem with Id '{request.Id}' was not found."));
            }

            if (todoItem.IsCompleted)
            {
                logger.LogInformation("TodoItem {TodoItemId} is already completed", request.Id);
                return Result.Failure<Guid>(new Error(
                    "TodoItem.AlreadyCompleted",
                    $"TodoItem with Id '{request.Id}' is already completed."));
            }

            // Mark the Todo item as completed and stamp the completion date.
            todoItem.IsCompleted = true;
            todoItem.CompletedDate = DateTime.UtcNow;

            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Completed TodoItem {TodoItemId} at {TodoItemCompletedDate}",
                todoItem.Id, todoItem.CompletedDate);

            return Result<Guid>.Success(todoItem.Id);
        }
    }

    /// <summary>
    /// Maps the CompleteTodo command to an HTTP POST endpoint at "/api/todos/{id:guid}/complete".
    /// </summary>
    /// <param name="app">The WebApplication instance used to map the endpoint.</param>
    public static WebApplication MapCompleteTodoEndpoint(this WebApplication app)
    {
        app.MapPost("/api/todos/{id:guid}/complete", async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
        {
            Result<Guid> result = await mediator.Send(new Command(id), cancellationToken);

            if (result.IsSuccess)
            {
                return Results.NoContent();
            }

            return result.Error.Code == "TodoItem.NotFound"
                ? Results.NotFound(new { error = result.Error })
                : Results.BadRequest(new { error = result.Error });
        })
        .WithName("CompleteTodo")
        .WithTags("todos");

        return app;
    }
}
