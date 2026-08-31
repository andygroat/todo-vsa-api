using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Todo.Vsa.Api.Features.ShoppingLists;
using Todo.Vsa.DataAccess.Context;

namespace Todo.Vsa.Api.Tests.Features.ShoppingLists;

/// <summary>
/// Tests for the <see cref="CreateShoppingList"/> vertical slice.
/// </summary>
public class CreateShoppingListTests
{
    private static TodoDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TodoDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new TodoDbContext(options);
    }

    private static CreateShoppingList.Handler CreateHandler(TodoDbContext context) =>
        new(context, NullLogger<CreateShoppingList.Handler>.Instance);

    [Test]
    public async Task Handle_WithValidCommand_PersistsShoppingListAndReturnsSuccessWithId()
    {
        // Arrange
        await using var context = CreateContext();
        var handler = CreateHandler(context);
        var command = new CreateShoppingList.CreateShoppingListCommand("Groceries");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsNotEqualTo(Guid.Empty);

        var stored = await context.ShoppingLists.SingleAsync();
        await Assert.That(stored.Id).IsEqualTo(result.Value);
        await Assert.That(stored.Title).IsEqualTo("Groceries");
    }
}

/// <summary>
/// Tests for the <see cref="CreateShoppingList.Validator"/> FluentValidation rules.
/// </summary>
public class CreateShoppingListValidatorTests
{
    [Test]
    public async Task Validator_WhenTitleIsEmpty_FailsValidation()
    {
        var validator = new CreateShoppingList.Validator();

        var result = await validator.ValidateAsync(new CreateShoppingList.CreateShoppingListCommand(string.Empty));

        await Assert.That(result.IsValid).IsFalse();
    }

    [Test]
    public async Task Validator_WhenTitleExceedsMaxLength_FailsValidation()
    {
        var validator = new CreateShoppingList.Validator();
        var tooLong = new string('x', 101);

        var result = await validator.ValidateAsync(new CreateShoppingList.CreateShoppingListCommand(tooLong));

        await Assert.That(result.IsValid).IsFalse();
    }

    [Test]
    public async Task Validator_WhenCommandIsValid_PassesValidation()
    {
        var validator = new CreateShoppingList.Validator();

        var result = await validator.ValidateAsync(new CreateShoppingList.CreateShoppingListCommand("valid"));

        await Assert.That(result.IsValid).IsTrue();
    }
}
