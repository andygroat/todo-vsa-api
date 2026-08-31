using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Todo.Vsa.Api.Features.ShoppingLists;
using Todo.Vsa.DataAccess.Context;
using Todo.Vsa.Model.Constants;
using Todo.Vsa.Model.Domain.ShoppingLists;

namespace Todo.Vsa.Api.Tests.Features.ShoppingLists;

/// <summary>
/// Tests for the <see cref="GetShoppingLists"/> vertical slice.
/// </summary>
public class GetShoppingListsTests
{
    private static TodoDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TodoDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new TodoDbContext(options);
    }

    private static GetShoppingLists.Handler CreateHandler(TodoDbContext context) =>
        new(context, NullLogger<GetShoppingLists.Handler>.Instance);

    [Test]
    public async Task Handle_ReturnsAllActiveShoppingLists()
    {
        // Arrange
        await using var context = CreateContext();
        var list1 = new ShoppingList { Id = Guid.NewGuid(), Title = "Groceries", Status = BusinessObjectStatus.Active };
        var list2 = new ShoppingList { Id = Guid.NewGuid(), Title = "Hardware", Status = BusinessObjectStatus.Active };
        context.ShoppingLists.AddRange(list1, list2);
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);
        var query = new GetShoppingLists.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        var lists = result.Value.ToList();
        await Assert.That(lists).HasCount().EqualTo(2);
        await Assert.That(lists.Any(l => l.Title == "Groceries")).IsTrue();
        await Assert.That(lists.Any(l => l.Title == "Hardware")).IsTrue();
    }

    [Test]
    public async Task Handle_ExcludesDeletedShoppingLists()
    {
        // Arrange
        await using var context = CreateContext();
        var activeList = new ShoppingList { Id = Guid.NewGuid(), Title = "Active", Status = BusinessObjectStatus.Active };
        var deletedList = new ShoppingList { Id = Guid.NewGuid(), Title = "Deleted", Status = BusinessObjectStatus.Deleted };
        context.ShoppingLists.AddRange(activeList, deletedList);
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);
        var query = new GetShoppingLists.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        var lists = result.Value.ToList();
        await Assert.That(lists).HasCount().EqualTo(1);
        await Assert.That(lists[0].Title).IsEqualTo("Active");
    }

    [Test]
    public async Task Handle_WhenNoLists_ReturnsEmptyCollection()
    {
        // Arrange
        await using var context = CreateContext();
        var handler = CreateHandler(context);
        var query = new GetShoppingLists.Query();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsEmpty();
    }
}
