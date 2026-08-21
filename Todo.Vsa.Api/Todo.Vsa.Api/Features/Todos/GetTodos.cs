using MediatR;
using Microsoft.EntityFrameworkCore;
using Todo.Vsa.Api.Infrastructure.ResultHelper;
using Todo.Vsa.DataAccess.Context;
using Todo.Vsa.Model.Domain.Todos;

namespace Todo.Vsa.Api.Features.Todos;

/// <summary>
/// GetTodos feature slice. Encapsulates the query, response DTO, handler, and endpoint
/// mapping used to retrieve the list of Todo items following the Vertical Slice Architecture.
/// </summary>
public static class GetTodos
{
    /// <summary>
    /// Query record representing the request to fetch all Todo items. Implements
    /// <see cref="IRequest{TResponse}"/> so it can be dispatched via MediatR.
    /// </summary>
    public record Query(string? Search) : IRequest<Result<List<Response>>>;

    /// <summary>
    /// Response DTO returned per Todo item. Kept local to the slice to decouple the
    /// API contract from the underlying domain entity.
    /// </summary>
    /// <param name="Id">The unique identifier of the Todo item.</param>
    /// <param name="Description">The description of the Todo item.</param>
    /// <param name="DueDate">The due date of the Todo item, if any.</param>
    /// <param name="IsCompleted">Whether the Todo item has been completed.</param>
    public record Response(Guid Id, string Description, DateTime? DueDate, bool IsCompleted);

    /// <summary>
    /// Handler that processes the <see cref="Query"/> and returns the collection of Todo items.
    /// </summary>
    /// <param name="context">The database context used to query Todo items.</param>
    /// <param name="logger">The logger used to record diagnostic information.</param>
    internal sealed class Handler(TodoDbContext context, ILogger<Handler> logger) : IRequestHandler<Query, Result<List<Response>>>
    {
        public async Task<Result<List<Response>>> Handle(Query request, CancellationToken cancellationToken)
        {
            // Project directly into the response DTO to avoid materializing entities we don't need.
            IQueryable<TodoItem> todos = context.TodoItems.AsQueryable();

            // Apply search filter if provided
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                string search = request.Search;
                todos = todos.Where(todoItem => todoItem.Description.Contains(search));
            }

            // Order by DueDate and project to Response DTO
            var result = await todos
                .OrderBy(t => t.DueDate)
                .Select(t => new Response(t.Id, t.Description, t.DueDate, t.IsCompleted))
                .ToListAsync(cancellationToken);

            logger.LogInformation("Retrieved {TodoItemCount} TodoItem(s)", result.Count);

            return Result<List<Response>>.Success(result);
        }
    }

    /// <summary>
    /// Maps the GetTodos query to an HTTP GET endpoint at "/api/todos".
    /// </summary>
    /// <param name="app">The WebApplication instance used to map the endpoint.</param>
    public static WebApplication MapGetTodosEndpoint(this WebApplication app)
    {
        app.MapGet("/api/todos", async (string? search, IMediator mediator, CancellationToken cancellationToken) =>
        {
            // Send the query to the MediatR pipeline and await the result
            Result<List<Response>> result = await mediator.Send(new Query(search), cancellationToken);
            // Return the appropriate HTTP response based on the result
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        })
        .WithName("GetTodos")
        .WithTags("todos");

        return app;
    }
}
