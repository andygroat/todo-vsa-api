using Microsoft.EntityFrameworkCore;
using Todo.Vsa.Model.Constants;
using Todo.Vsa.Model.Domain.Todos;

namespace Todo.Vsa.DataAccess.Context;

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
