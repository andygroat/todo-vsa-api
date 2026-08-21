using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Todo.Vsa.Api.Features.Todos;
using Todo.Vsa.DataAccess.Context;
using Todo.Vsa.Model.Domain.Todos;

namespace Todo.Vsa.Api.Tests.Features.Todos;

/// <summary>
/// Tests for the <see cref="GetTodos"/> vertical slice.
/// </summary>
public class GetTodosTests
{
    private static TodoDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TodoDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new TodoDbContext(options);
    }

    private static GetTodos.Handler CreateHandler(TodoDbContext context) =>
        new(context, NullLogger<GetTodos.Handler>.Instance);

    [Test]
    public async Task Handle_WhenNoTodos_ReturnsSuccessWithEmptyList()
    {
        // Arrange
        await using var context = CreateContext();
        var handler = CreateHandler(context);

        // Act
        var result = await handler.Handle(new GetTodos.Query(null), CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsEmpty();
    }

    [Test]
    public async Task Handle_WhenTodosExist_ReturnsAllOrderedByDueDate()
    {
        // Arrange
        await using var context = CreateContext();

        var later = new TodoItem { Id = Guid.NewGuid(), Description = "Later", DueDate = DateTime.Today.AddDays(5), IsCompleted = false };
        var sooner = new TodoItem { Id = Guid.NewGuid(), Description = "Sooner", DueDate = DateTime.Today.AddDays(1), IsCompleted = false };
        var noDate = new TodoItem { Id = Guid.NewGuid(), Description = "No due", DueDate = null, IsCompleted = true };
        context.TodoItems.AddRange(later, sooner, noDate);
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);

        // Act
        var result = await handler.Handle(new GetTodos.Query(null), CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value.Count).IsEqualTo(3);

        // Nulls sort first when ordering ascending by DueDate.
        await Assert.That(result.Value[0].Id).IsEqualTo(noDate.Id);
        await Assert.That(result.Value[1].Id).IsEqualTo(sooner.Id);
        await Assert.That(result.Value[2].Id).IsEqualTo(later.Id);
    }

    [Test]
    public async Task Handle_MapsTodoItemFieldsIntoResponseDto()
    {
        // Arrange
        await using var context = CreateContext();
        var todo = new TodoItem
        {
            Id = Guid.NewGuid(),
            Description = "Mapping check",
            DueDate = DateTime.Today.AddDays(2),
            IsCompleted = true
        };
        context.TodoItems.Add(todo);
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);

        // Act
        var result = await handler.Handle(new GetTodos.Query(null), CancellationToken.None);

        // Assert
        var response = result.Value.Single();
        await Assert.That(response.Id).IsEqualTo(todo.Id);
        await Assert.That(response.Description).IsEqualTo(todo.Description);
        await Assert.That(response.DueDate).IsEqualTo(todo.DueDate);
        await Assert.That(response.IsCompleted).IsEqualTo(todo.IsCompleted);
    }

    [Test]
    public async Task Handle_WhenSearchProvided_FiltersByDescriptionContains()
    {
        // Arrange
        await using var context = CreateContext();
        context.TodoItems.AddRange(
            new TodoItem { Id = Guid.NewGuid(), Description = "Buy milk", IsCompleted = false },
            new TodoItem { Id = Guid.NewGuid(), Description = "Buy eggs", IsCompleted = false },
            new TodoItem { Id = Guid.NewGuid(), Description = "Read book", IsCompleted = false });
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);

        // Act
        var result = await handler.Handle(new GetTodos.Query("Buy"), CancellationToken.None);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value.Count).IsEqualTo(2);
        await Assert.That(result.Value.All(r => r.Description.Contains("Buy"))).IsTrue();
    }

    [Test]
    public async Task Handle_WhenSearchIsWhitespace_ReturnsAllTodos()
    {
        // Arrange
        await using var context = CreateContext();
        context.TodoItems.AddRange(
            new TodoItem { Id = Guid.NewGuid(), Description = "A", IsCompleted = false },
            new TodoItem { Id = Guid.NewGuid(), Description = "B", IsCompleted = false });
        await context.SaveChangesAsync();

        var handler = CreateHandler(context);

        // Act
        var result = await handler.Handle(new GetTodos.Query("   "), CancellationToken.None);

        // Assert
        await Assert.That(result.Value.Count).IsEqualTo(2);
    }
}
