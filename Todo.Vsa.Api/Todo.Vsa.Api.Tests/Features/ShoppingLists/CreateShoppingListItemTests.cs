using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Todo.Vsa.Api.Features.ShoppingLists;
using Todo.Vsa.DataAccess.Context;
using Todo.Vsa.Model.Constants;
using Todo.Vsa.Model.Domain.ShoppingLists;

namespace Todo.Vsa.Api.Tests.Features.ShoppingLists;

/// <summary>
/// Tests for the <see cref="CreateShoppingListItem"/> vertical slice.
/// </summary>
public class CreateShoppingListItemTests
{
    private static TodoDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TodoDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new TodoDbContext(options);
    }

    private static CreateShoppingListItem.Handler CreateHandler(TodoDbContext context) =>
        new(context, NullLogger<CreateShoppingListItem.Handler>.Instance);

    [Test]
    public async Task Handle_WithValidCommand_PersistsItemAndReturnsSuccess()
    {
        await using var context = CreateContext();
        var list = new ShoppingList { Id = Guid.NewGuid(), Title = "Groceries", Status = BusinessObjectStatus.Active };
        context.ShoppingLists.Add(list);
        await context.SaveChangesAsync();

        var command = new CreateShoppingListItem.Command(list.Id, "Milk");
        var result = await CreateHandler(context).Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        var stored = await context.ShoppingListItems.SingleAsync();
        await Assert.That(stored.Title).IsEqualTo("Milk");
        await Assert.That(stored.IsComplete).IsFalse();
        await Assert.That(stored.ShoppingListId).IsEqualTo(list.Id);
    }

    [Test]
    public async Task Handle_WhenParentListMissing_ReturnsFailure()
    {
        await using var context = CreateContext();
        var command = new CreateShoppingListItem.Command(Guid.NewGuid(), "Milk");
        var result = await CreateHandler(context).Handle(command, CancellationToken.None);
        await Assert.That(result.IsSuccess).IsFalse();
    }

    [Test]
    public async Task Handle_WhenParentListDeleted_ReturnsFailure()
    {
        await using var context = CreateContext();
        var list = new ShoppingList { Id = Guid.NewGuid(), Title = "Deleted", Status = BusinessObjectStatus.Deleted };
        context.ShoppingLists.Add(list);
        await context.SaveChangesAsync();

        var result = await CreateHandler(context).Handle(new CreateShoppingListItem.Command(list.Id, "Milk"), CancellationToken.None);
        await Assert.That(result.IsSuccess).IsFalse();
    }
}

/// <summary>
/// Tests for the <see cref="CreateShoppingListItem.Validator"/> FluentValidation rules.
/// </summary>
public class CreateShoppingListItemValidatorTests
{
    [Test]
    public async Task Validator_WhenTitleIsEmpty_FailsValidation()
    {
        var validator = new CreateShoppingListItem.Validator();
        var result = await validator.ValidateAsync(new CreateShoppingListItem.Command(Guid.NewGuid(), string.Empty));
        await Assert.That(result.IsValid).IsFalse();
    }

    [Test]
    public async Task Validator_WhenTitleExceeds200Chars_FailsValidation()
    {
        var validator = new CreateShoppingListItem.Validator();
        var result = await validator.ValidateAsync(new CreateShoppingListItem.Command(Guid.NewGuid(), new string('x', 201)));
        await Assert.That(result.IsValid).IsFalse();
    }

    [Test]
    public async Task Validator_WhenListIdIsEmpty_FailsValidation()
    {
        var validator = new CreateShoppingListItem.Validator();
        var result = await validator.ValidateAsync(new CreateShoppingListItem.Command(Guid.Empty, "valid"));
        await Assert.That(result.IsValid).IsFalse();
    }

    [Test]
    public async Task Validator_WhenValid_PassesValidation()
    {
        var validator = new CreateShoppingListItem.Validator();
        var result = await validator.ValidateAsync(new CreateShoppingListItem.Command(Guid.NewGuid(), "Milk"));
        await Assert.That(result.IsValid).IsTrue();
    }
}
