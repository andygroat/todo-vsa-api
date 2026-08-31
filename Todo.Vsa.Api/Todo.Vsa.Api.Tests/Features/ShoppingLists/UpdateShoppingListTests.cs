using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Todo.Vsa.Api.Features.ShoppingLists;
using Todo.Vsa.DataAccess.Context;
using Todo.Vsa.Model.Constants;
using Todo.Vsa.Model.Domain.ShoppingLists;

namespace Todo.Vsa.Api.Tests.Features.ShoppingLists;

/// <summary>
/// Tests for the <see cref="UpdateShoppingList"/> vertical slice.
/// </summary>
public class UpdateShoppingListTests
{
    private static TodoDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TodoDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new TodoDbContext(options);
    }

    private static UpdateShoppingList.Handler CreateHandler(TodoDbContext context) =>
        new(context, NullLogger<UpdateShoppingList.Handler>.Instance);

    [Test]
    public async Task Handle_WithValidCommand_UpdatesTitle()
    {
        // Arrange
        await using var context = CreateContext();
        var list = new ShoppingList { Id = Guid.NewGuid(), Title = "Old Title", Status = BusinessObjectStatus.Active };
        context.ShoppingLists.Add(list);
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);
        var command = new UpdateShoppingList.Command(list.Id, "New Title");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        var updated = await context.ShoppingLists.SingleAsync();
        await Assert.That(updated.Title).IsEqualTo("New Title");
    }

    [Test]
    public async Task Handle_WhenListNotFound_ReturnsFailure()
    {
        // Arrange
        await using var context = CreateContext();
        var handler = CreateHandler(context);
        var command = new UpdateShoppingList.Command(Guid.NewGuid(), "New Title");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Error.Code).IsEqualTo("ShoppingList.NotFound");
    }

    [Test]
    public async Task Handle_WhenListIsDeleted_ReturnsFailure()
    {
        // Arrange
        await using var context = CreateContext();
        var list = new ShoppingList { Id = Guid.NewGuid(), Title = "Deleted", Status = BusinessObjectStatus.Deleted };
        context.ShoppingLists.Add(list);
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);
        var command = new UpdateShoppingList.Command(list.Id, "New Title");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
    }
}

/// <summary>
/// Tests for the <see cref="UpdateShoppingList.Validator"/> FluentValidation rules.
/// </summary>
public class UpdateShoppingListValidatorTests
{
    [Test]
    public async Task Validator_WhenTitleIsEmpty_FailsValidation()
    {
        var validator = new UpdateShoppingList.Validator();

        var result = await validator.ValidateAsync(new UpdateShoppingList.UpdateShoppingListCommand(string.Empty));

        await Assert.That(result.IsValid).IsFalse();
    }

    [Test]
    public async Task Validator_WhenTitleExceedsMaxLength_FailsValidation()
    {
        var validator = new UpdateShoppingList.Validator();
        var tooLong = new string('x', 101);

        var result = await validator.ValidateAsync(new UpdateShoppingList.UpdateShoppingListCommand(tooLong));

        await Assert.That(result.IsValid).IsFalse();
    }
}
