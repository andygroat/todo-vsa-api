using Microsoft.EntityFrameworkCore;
using Todo.VSA.Model.Constants;
using Todo.VSA.Model.Domain.Todos;

namespace Todo.VSA.DataAccess.Context;

public sealed class TodoDbContext (DbContextOptions<TodoDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Gets or sets the DbSet of TodoItem entities.
    /// </summary>
    public DbSet<TodoItem> TodoItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schemas.Default);
    }
}
