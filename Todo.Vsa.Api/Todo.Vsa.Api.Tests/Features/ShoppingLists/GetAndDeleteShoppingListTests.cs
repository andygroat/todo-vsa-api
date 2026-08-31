using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Todo.Vsa.Api.Features.ShoppingLists;
using Todo.Vsa.DataAccess.Context;
using Todo.Vsa.Model.Constants;
using Todo.Vsa.Model.Domain.ShoppingLists;

namespace Todo.Vsa.Api.Tests.Features.ShoppingLists;

/// <summary>
/// Tests for the <see cref="GetShoppingListById"/> vertical slice.
/// </summary>
public class GetShoppingListByIdTests
{
    private static TodoDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TodoDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new TodoDbContext(options);
    }

    private static GetShoppingListById.Handler CreateHandler(TodoDbContext context) =>
        new(context, NullLogger<GetShoppingListById.Handler>.Instance);

    [Test]
    public async Task Handle_WhenListExists_ReturnsSuccess()
    {
        await using var context = CreateContext();
        var list = new ShoppingList { Id = Guid.NewGuid(), Title = "Groceries", Status = BusinessObjectStatus.Active };
        context.ShoppingLists.Add(list);
        await context.SaveChangesAsync();

        var result = await CreateHandler(context).Handle(new GetShoppingListById.Query(list.Id), CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value.Id).IsEqualTo(list.Id);
        await Assert.That(result.Value.Title).IsEqualTo("Groceries");
    }

    [Test]
    public async Task Handle_WhenListMissing_ReturnsFailure()
    {
        await using var context = CreateContext();
        var result = await CreateHandler(context).Handle(new GetShoppingListById.Query(Guid.NewGuid()), CancellationToken.None);
        await Assert.That(result.IsSuccess).IsFalse();
    }

    [Test]
    public async Task Handle_WhenListDeleted_ReturnsFailure()
    {
        await using var context = CreateContext();
        var list = new ShoppingList { Id = Guid.NewGuid(), Title = "Deleted", Status = BusinessObjectStatus.Deleted };
        context.ShoppingLists.Add(list);
        await context.SaveChangesAsync();

        var result = await CreateHandler(context).Handle(new GetShoppingListById.Query(list.Id), CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
    }
}

/// <summary>
/// Tests for the <see cref="DeleteShoppingList"/> vertical slice.
/// </summary>
public class DeleteShoppingListTests
{
    private static TodoDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TodoDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new TodoDbContext(options);
    }

    private static DeleteShoppingList.Handler CreateHandler(TodoDbContext context) =>
        new(context, NullLogger<DeleteShoppingList.Handler>.Instance);

    [Test]
    public async Task Handle_SoftDeletesListAndAllItems()
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
                new ShoppingListItem { Id = Guid.NewGuid(), ShoppingListId = listId, Title = "Bread", Status = BusinessObjectStatus.Active }
            ]
        };
        context.ShoppingLists.Add(list);
        await context.SaveChangesAsync();

        var result = await CreateHandler(context).Handle(new DeleteShoppingList.Command(listId), CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        var storedList = await context.ShoppingLists.SingleAsync();
        await Assert.That(storedList.Status).IsEqualTo(BusinessObjectStatus.Deleted);
        var items = await context.ShoppingListItems.ToListAsync();
        await Assert.That(items.All(i => i.Status == BusinessObjectStatus.Deleted)).IsTrue();
    }

    [Test]
    public async Task Handle_WhenListNotFound_ReturnsFailure()
    {
        await using var context = CreateContext();
        var result = await CreateHandler(context).Handle(new DeleteShoppingList.Command(Guid.NewGuid()), CancellationToken.None);
        await Assert.That(result.IsSuccess).IsFalse();
    }
}
