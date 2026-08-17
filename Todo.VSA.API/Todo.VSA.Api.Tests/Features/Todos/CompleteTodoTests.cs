using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Todo.VSA.Api.Features.Todos;
using Todo.VSA.DataAccess.Context;
using Todo.VSA.Model.Domain.Todos;

namespace Todo.VSA.Api.Tests.Features.Todos;

/// <summary>
/// Tests for the <see cref="CompleteTodo"/> vertical slice, exercising the handler
/// directly against an EF Core in-memory database.
/// </summary>
public class CompleteTodoTests
{
    /// <summary>
    /// Builds an isolated in-memory <see cref="TodoDbContext"/> per test.
    /// </summary>
    private static TodoDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TodoDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new TodoDbContext(options);
    }

    private static CompleteTodo.Handler CreateHandler(TodoDbContext context) =>
        new(context, NullLogger<CompleteTodo.Handler>.Instance);

    [Test]
    public async Task Handle_WhenTodoExistsAndIsNotCompleted_MarksAsCompletedAndReturnsSuccess()
    {
        // Arrange
        await using var context = CreateContext();
        var todo = new TodoItem
        {
            Id = Guid.NewGuid(),
            Description = "Write CompleteTodo tests",
            DueDate = DateTime.Today.AddDays(1),
            IsCompleted = false,
            CompletedDate = null
        };
        context.TodoItems.Add(todo);
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);
        var beforeUtc = DateTime.UtcNow;

        // Act
        var result = await handler.Handle(new CompleteTodo.Command(todo.Id), CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsEqualTo(todo.Id);

        var updated = await context.TodoItems.SingleAsync(t => t.Id == todo.Id);
        await Assert.That(updated.IsCompleted).IsTrue();
        await Assert.That(updated.CompletedDate).IsNotNull();
        await Assert.That(updated.CompletedDate!.Value).IsGreaterThanOrEqualTo(beforeUtc);
    }

    [Test]
    public async Task Handle_WhenTodoDoesNotExist_ReturnsNotFoundFailure()
    {
        // Arrange
        await using var context = CreateContext();
        var handler = CreateHandler(context);
        var missingId = Guid.NewGuid();

        // Act
        var result = await handler.Handle(new CompleteTodo.Command(missingId), CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error.Code).IsEqualTo("TodoItem.NotFound");
    }

    [Test]
    public async Task Handle_WhenTodoAlreadyCompleted_ReturnsAlreadyCompletedFailureAndDoesNotOverwriteCompletedDate()
    {
        // Arrange
        await using var context = CreateContext();
        var originalCompletedDate = DateTime.UtcNow.AddDays(-2);
        var todo = new TodoItem
        {
            Id = Guid.NewGuid(),
            Description = "Already done",
            DueDate = null,
            IsCompleted = true,
            CompletedDate = originalCompletedDate
        };
        context.TodoItems.Add(todo);
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);

        // Act
        var result = await handler.Handle(new CompleteTodo.Command(todo.Id), CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Error.Code).IsEqualTo("TodoItem.AlreadyCompleted");

        var unchanged = await context.TodoItems.SingleAsync(t => t.Id == todo.Id);
        await Assert.That(unchanged.IsCompleted).IsTrue();
        await Assert.That(unchanged.CompletedDate).IsEqualTo(originalCompletedDate);
    }
}

/// <summary>
/// Tests for the <see cref="CompleteTodo.Validator"/> FluentValidation rules.
/// </summary>
public class CompleteTodoValidatorTests
{
    [Test]
    public async Task Validator_WhenIdIsEmpty_FailsValidation()
    {
        var validator = new CompleteTodo.Validator();

        var result = await validator.ValidateAsync(new CompleteTodo.Command(Guid.Empty));

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors).IsNotEmpty();
    }

    [Test]
    public async Task Validator_WhenIdIsNotEmpty_PassesValidation()
    {
        var validator = new CompleteTodo.Validator();

        var result = await validator.ValidateAsync(new CompleteTodo.Command(Guid.NewGuid()));

        await Assert.That(result.IsValid).IsTrue();
    }
}
