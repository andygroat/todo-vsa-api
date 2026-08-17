using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Todo.VSA.Api.Features.Todos;
using Todo.VSA.DataAccess.Context;
using Todo.VSA.Model.Domain.Todos;

namespace Todo.VSA.Api.Tests.Features.Todos;

/// <summary>
/// Tests for the <see cref="GetTodoById"/> vertical slice.
/// </summary>
public class GetTodoByIdTests
{
    private static TodoDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TodoDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new TodoDbContext(options);
    }

    private static GetTodoById.Handler CreateHandler(TodoDbContext context) =>
        new(context, NullLogger<GetTodoById.Handler>.Instance);

    [Test]
    public async Task Handle_WhenTodoExists_ReturnsSuccessWithMappedResponse()
    {
        // Arrange
        await using var context = CreateContext();
        var todo = new TodoItem
        {
            Id = Guid.NewGuid(),
            Description = "Find me",
            DueDate = DateTime.Today.AddDays(3),
            IsCompleted = false
        };
        context.TodoItems.Add(todo);
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);

        // Act
        var result = await handler.Handle(new GetTodoById.Query(todo.Id), CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value.Id).IsEqualTo(todo.Id);
        await Assert.That(result.Value.Description).IsEqualTo(todo.Description);
        await Assert.That(result.Value.DueDate).IsEqualTo(todo.DueDate);
        await Assert.That(result.Value.IsCompleted).IsEqualTo(todo.IsCompleted);
    }

    [Test]
    public async Task Handle_WhenTodoDoesNotExist_ReturnsNotFoundFailure()
    {
        // Arrange
        await using var context = CreateContext();
        var handler = CreateHandler(context);

        // Act
        var result = await handler.Handle(new GetTodoById.Query(Guid.NewGuid()), CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Error.Code).IsEqualTo("TodoItem.NotFound");
    }
}

/// <summary>
/// Tests for the <see cref="GetTodoById.Validator"/> FluentValidation rules.
/// </summary>
public class GetTodoByIdValidatorTests
{
    [Test]
    public async Task Validator_WhenIdIsEmpty_FailsValidation()
    {
        var validator = new GetTodoById.Validator();

        var result = await validator.ValidateAsync(new GetTodoById.Query(Guid.Empty));

        await Assert.That(result.IsValid).IsFalse();
    }

    [Test]
    public async Task Validator_WhenIdIsNotEmpty_PassesValidation()
    {
        var validator = new GetTodoById.Validator();

        var result = await validator.ValidateAsync(new GetTodoById.Query(Guid.NewGuid()));

        await Assert.That(result.IsValid).IsTrue();
    }
}
