using FluentValidation;
using MediatR;
using Todo.VSA.Api.Infrastructure.ResultHelper;
using Todo.VSA.DataAccess.Context;
using Todo.VSA.Model.Domain.Todos;

namespace Todo.VSA.Api.Features.Todos;

/// <summary>
/// CreateTodo feature class for handling the creation of Todo items. This class defines the necessary logic, commands, and handlers for creating new Todo items in the application.
/// </summary>
public static class CreateTodo
{
    /// <summary>
    /// Command record representing the data required to create a new Todo item. This record contains the description and due date of the Todo item, and it implements the IRequest 
    /// interface from MediatR, allowing it to be handled by a corresponding handler.
    /// </summary>
    /// <param name="Description">The description of the Todo item.</param>
    /// <param name="DueDate">The due date of the Todo item.</param>
    public record Command(string Description, DateTime? DueDate) : IRequest<Result<Guid>>;

    /// <summary>
    /// Validator class for validating the CreateTodo command. This class inherits from AbstractValidator provided by FluentValidation and defines validation rules for the command's 
    /// properties, ensuring that the description is not empty and has a maximum length of 100 characters, and that the due date is greater than or equal to today's date if it is 
    /// provided.
    /// </summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Description).NotEmpty().MaximumLength(100);
            RuleFor(x => x.DueDate).GreaterThanOrEqualTo(DateTime.Today).When(x => x.DueDate.HasValue);
        }
    }

    /// <summary>
    /// Handler class for processing the CreateTodo command. This class implements the IRequestHandler interface from MediatR, allowing it to handle the command and return a Result 
    /// containing the ID of the newly created Todo item. The handler uses the TodoDbContext to interact with the database and ILogger for logging information about the creation of 
    /// the Todo item.
    /// </summary>
    /// <param name="context">The database context used to interact with the Todo items in the database.</param>
    /// <param name="logger">The logger used to log information about the creation of the Todo item.</param>
    internal sealed class Handler (TodoDbContext context, ILogger<Handler> logger) : IRequestHandler<Command, Result<Guid>>
    {
        
        public async Task<Result<Guid>> Handle(Command request, CancellationToken cancellationToken)
        {
            // Create the new TodoItem entity based on the request data
            var todoItem = new TodoItem
            {
                Id = Guid.NewGuid(),
                Description = request.Description,
                DueDate = request.DueDate,
                IsCompleted = false
            };
            // Add the new TodoItem to the database context
            context.TodoItems.Add(todoItem);
            // Save the changes to the database
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Created TodoItem {TodoItemId}: {TodoItemDescription}",
                todoItem.Id, todoItem.Description);

            return Result<Guid>.Success(todoItem.Id);
        }
    }

    /// <summary>
    /// Maps the CreateTodo command to an HTTP POST endpoint at "/api/todos". This method uses MediatR to send the command to the appropriate handler and returns a response indicating
    /// the result of the operation.
    /// </summary>
    /// <param name="app">The WebApplication instance used to map the endpoint.</param>
    public static WebApplication MapCreateTodoEndpoint(this WebApplication app)
    {
        // Map the HTTP POST endpoint for creating a new Todo item
        app.MapPost("/api/todos", async (Command command, IMediator mediator, CancellationToken cancellationToken) => 
        {
            // Send the command to the MediatR handler and await the result
            Result<Guid> result = await mediator.Send(command, cancellationToken);

            // Return the appropriate HTTP response based on the result of the operation
            return result.IsSuccess
                ? Results.Created($"/api/todos/{result.Value}", new { id = result.Value })
                : Results.BadRequest(new { error = result.Error });
        })
        .WithName("CreateTodo")
        .WithTags("todos");

        return app;
    }
}
