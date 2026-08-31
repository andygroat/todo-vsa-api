using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Todo.Vsa.Api.Features.Todos;
using Todo.Vsa.DataAccess.Context;

namespace Todo.Vsa.Api.Tests.Features.Todos;

/// <summary>
/// Tests for the <see cref="CreateTodo"/> vertical slice.
/// </summary>
public class CreateTodoTests
{
    private static TodoDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TodoDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new TodoDbContext(options);
    }

    private static CreateTodo.Handler CreateHandler(TodoDbContext context) =>
        new(context, NullLogger<CreateTodo.Handler>.Instance);

    [Test]
    public async Task Handle_WithValidCommand_PersistsTodoAndReturnsSuccessWithId()
    {
        // Arrange
        await using var context = CreateContext();
        var handler = CreateHandler(context);
        var command = new CreateTodo.CreateTodoCommand("Buy milk", DateTime.Today.AddDays(1));

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsNotEqualTo(Guid.Empty);

        var stored = await context.TodoItems.SingleAsync();
        await Assert.That(stored.Id).IsEqualTo(result.Value);
        await Assert.That(stored.Description).IsEqualTo("Buy milk");
        await Assert.That(stored.DueDate).IsEqualTo(command.DueDate);
        await Assert.That(stored.IsCompleted).IsFalse();
    }

    [Test]
    public async Task Handle_WhenDueDateIsNull_PersistsTodoWithNullDueDate()
    {
        // Arrange
        await using var context = CreateContext();
        var handler = CreateHandler(context);
        var command = new CreateTodo.CreateTodoCommand("No due date", null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        var stored = await context.TodoItems.SingleAsync();
        await Assert.That(stored.DueDate).IsNull();
    }
}

/// <summary>
/// Tests for the <see cref="CreateTodo.Validator"/> FluentValidation rules.
/// </summary>
public class CreateTodoValidatorTests
{
    [Test]
    public async Task Validator_WhenDescriptionIsEmpty_FailsValidation()
    {
        var validator = new CreateTodo.Validator();

        var result = await validator.ValidateAsync(new CreateTodo.CreateTodoCommand(string.Empty, null));

        await Assert.That(result.IsValid).IsFalse();
    }

    [Test]
    public async Task Validator_WhenDescriptionExceedsMaxLength_FailsValidation()
    {
        var validator = new CreateTodo.Validator();
        var tooLong = new string('x', 101);

        var result = await validator.ValidateAsync(new CreateTodo.CreateTodoCommand(tooLong, null));

        await Assert.That(result.IsValid).IsFalse();
    }

    [Test]
    public async Task Validator_WhenDueDateIsInThePast_FailsValidation()
    {
        var validator = new CreateTodo.Validator();

        var result = await validator.ValidateAsync(new CreateTodo.CreateTodoCommand("valid", DateTime.Today.AddDays(-1)));

        await Assert.That(result.IsValid).IsFalse();
    }

    [Test]
    public async Task Validator_WhenCommandIsValid_PassesValidation()
    {
        var validator = new CreateTodo.Validator();

        var result = await validator.ValidateAsync(new CreateTodo.CreateTodoCommand("valid", DateTime.Today.AddDays(1)));

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Validator_WhenDueDateIsNull_PassesValidation()
    {
        var validator = new CreateTodo.Validator();

        var result = await validator.ValidateAsync(new CreateTodo.CreateTodoCommand("valid", null));

        await Assert.That(result.IsValid).IsTrue();
    }
}
