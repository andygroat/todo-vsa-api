using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Todo.Vsa.Api.Features.ShoppingLists;
using Todo.Vsa.DataAccess.Context;
using Todo.Vsa.Model.Constants;
using Todo.Vsa.Model.Domain.ShoppingLists;

namespace Todo.Vsa.Api.Tests.Features.ShoppingLists;

/// <summary>
/// Tests for the <see cref="UpdateShoppingListItem"/> and <see cref="DeleteShoppingListItem"/> vertical slices.
/// </summary>
public class UpdateAndDeleteShoppingListItemTests
{
    private static TodoDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TodoDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new TodoDbContext(options);
    }

    private static async Task<(Guid listId, Guid itemId)> SeedListAndItemAsync(TodoDbContext context)
    {
        var listId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var list = new ShoppingList
        {
            Id = listId,
            Title = "Groceries",
            Status = BusinessObjectStatus.Active,
            Items =
            [
                new ShoppingListItem { Id = itemId, ShoppingListId = listId, Title = "Milk", IsComplete = false, Status = BusinessObjectStatus.Active }
            ]
        };
        context.ShoppingLists.Add(list);
        await context.SaveChangesAsync();
        return (listId, itemId);
    }

    [Test]
    public async Task Update_WithValidCommand_UpdatesTitleAndIsComplete()
    {
        await using var context = CreateContext();
        var (listId, itemId) = await SeedListAndItemAsync(context);

        var handler = new UpdateShoppingListItem.Handler(context, NullLogger<UpdateShoppingListItem.Handler>.Instance);
        var result = await handler.Handle(new UpdateShoppingListItem.Command(listId, itemId, "Milk 2L", true), CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        var updated = await context.ShoppingListItems.SingleAsync();
        await Assert.That(updated.Title).IsEqualTo("Milk 2L");
        await Assert.That(updated.IsComplete).IsTrue();
    }

    [Test]
    public async Task Update_WhenItemMissing_ReturnsFailure()
    {
        await using var context = CreateContext();
        var (listId, _) = await SeedListAndItemAsync(context);
        var handler = new UpdateShoppingListItem.Handler(context, NullLogger<UpdateShoppingListItem.Handler>.Instance);

        var result = await handler.Handle(new UpdateShoppingListItem.Command(listId, Guid.NewGuid(), "x", true), CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
    }

    [Test]
    public async Task Delete_SoftDeletesItem()
    {
        await using var context = CreateContext();
        var (listId, itemId) = await SeedListAndItemAsync(context);

        var handler = new DeleteShoppingListItem.Handler(context, NullLogger<DeleteShoppingListItem.Handler>.Instance);
        var result = await handler.Handle(new DeleteShoppingListItem.Command(listId, itemId), CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        var stored = await context.ShoppingListItems.SingleAsync();
        await Assert.That(stored.Status).IsEqualTo(BusinessObjectStatus.Deleted);
    }

    [Test]
    public async Task Delete_WhenItemMissing_ReturnsFailure()
    {
        await using var context = CreateContext();
        var (listId, _) = await SeedListAndItemAsync(context);

        var handler = new DeleteShoppingListItem.Handler(context, NullLogger<DeleteShoppingListItem.Handler>.Instance);
        var result = await handler.Handle(new DeleteShoppingListItem.Command(listId, Guid.NewGuid()), CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
    }
}

/// <summary>
/// Tests for the <see cref="GetShoppingListItems"/> and <see cref="GetShoppingListItemById"/> vertical slices.
/// </summary>
public class GetShoppingListItemsTests
{
    private static TodoDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TodoDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new TodoDbContext(options);
    }

    [Test]
    public async Task GetAll_ReturnsOnlyActiveItemsForList()
    {
        await using var context = CreateContext();
        var listId = Guid.NewGuid();
        var list = new ShoppingList
        {
            Id = listId,
            Title = "Groceries",
            Status = BusinessObjectStatus.Active,
            Items =
            [
                new ShoppingListItem { Id = Guid.NewGuid(), ShoppingListId = listId, Title = "Milk", Status = BusinessObjectStatus.Active },
                new ShoppingListItem { Id = Guid.NewGuid(), ShoppingListId = listId, Title = "Old", Status = BusinessObjectStatus.Deleted }
            ]
        };
        context.ShoppingLists.Add(list);
        await context.SaveChangesAsync();

        var handler = new GetShoppingListItems.Handler(context, NullLogger<GetShoppingListItems.Handler>.Instance);
        var result = await handler.Handle(new GetShoppingListItems.Query(listId), CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        var items = result.Value.ToList();
        await Assert.That(items).HasCount().EqualTo(1);
        await Assert.That(items[0].Title).IsEqualTo("Milk");
    }

    [Test]
    public async Task GetAll_WhenListMissing_ReturnsFailure()
    {
        await using var context = CreateContext();
        var handler = new GetShoppingListItems.Handler(context, NullLogger<GetShoppingListItems.Handler>.Instance);
        var result = await handler.Handle(new GetShoppingListItems.Query(Guid.NewGuid()), CancellationToken.None);
        await Assert.That(result.IsSuccess).IsFalse();
    }

    [Test]
    public async Task GetById_WhenItemExists_ReturnsItem()
    {
        await using var context = CreateContext();
        var listId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var list = new ShoppingList
        {
            Id = listId,
            Title = "Groceries",
            Status = BusinessObjectStatus.Active,
            Items = [new ShoppingListItem { Id = itemId, ShoppingListId = listId, Title = "Milk", IsComplete = true, Status = BusinessObjectStatus.Active }]
        };
        context.ShoppingLists.Add(list);
        await context.SaveChangesAsync();

        var handler = new GetShoppingListItemById.Handler(context, NullLogger<GetShoppingListItemById.Handler>.Instance);
        var result = await handler.Handle(new GetShoppingListItemById.Query(listId, itemId), CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value.Title).IsEqualTo("Milk");
        await Assert.That(result.Value.IsComplete).IsTrue();
    }
}
