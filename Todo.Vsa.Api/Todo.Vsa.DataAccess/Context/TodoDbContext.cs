using Microsoft.EntityFrameworkCore;
using Todo.Vsa.Model.Constants;
using Todo.Vsa.Model.Domain.ShoppingLists;
using Todo.Vsa.Model.Domain.Todos;

namespace Todo.Vsa.DataAccess.Context;

public sealed class TodoDbContext (DbContextOptions<TodoDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Gets or sets the DbSet of TodoItem entities.
    /// </summary>
    public DbSet<TodoItem> TodoItems { get; set; }

    /// <summary>
    /// Gets or sets the DbSet of ShoppingList entities.
    /// </summary>
    public DbSet<ShoppingList> ShoppingLists { get; set; }

    /// <summary>
    /// Gets or sets the DbSet of ShoppingListItem entities.
    /// </summary>
    public DbSet<ShoppingListItem> ShoppingListItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schemas.Default);

        // Configure one-to-many relationship between ShoppingList and ShoppingListItem
        modelBuilder.Entity<ShoppingList>()
            .HasMany(sl => sl.Items)
            .WithOne()
            .HasForeignKey(sli => sli.ShoppingListId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
