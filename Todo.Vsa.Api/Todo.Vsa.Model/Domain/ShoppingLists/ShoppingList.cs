using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Todo.Vsa.Model.Constants;

namespace Todo.Vsa.Model.Domain.ShoppingLists;

/// <summary>
/// Represents a shopping list in the application.
/// </summary>
[Table("ShoppingLists", Schema = Schemas.Default)]
public sealed class ShoppingList : BusinessObject
{
    /// <summary>
    /// Gets or sets the title of the shopping list.
    /// </summary>
    [Required, MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the collection of items in the shopping list.
    /// </summary>
    public ICollection<ShoppingListItem> Items { get; set; } = [];
}
