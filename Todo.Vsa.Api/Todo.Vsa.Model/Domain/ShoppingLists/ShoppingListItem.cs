using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Todo.Vsa.Model.Constants;

namespace Todo.Vsa.Model.Domain.ShoppingLists;

/// <summary>
/// Represents an item in a shopping list.
/// </summary>
[Table("ShoppingListItems", Schema = Schemas.Default)]
public sealed class ShoppingListItem : BusinessObject
{
    /// <summary>
    /// Gets or sets the title of the shopping list item.
    /// </summary>
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the shopping list item is complete.
    /// </summary>
    public bool IsComplete { get; set; }

    /// <summary>
    /// Gets or sets the ID of the shopping list this item belongs to.
    /// </summary>
    [Required]
    public Guid ShoppingListId { get; set; }
}
