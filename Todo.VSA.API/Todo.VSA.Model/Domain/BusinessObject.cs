using System.ComponentModel.DataAnnotations;
using Todo.Vsa.Model.Constants;

namespace Todo.Vsa.Model.Domain;

/// <summary>
/// Base business object class to provide the base properties that all
/// business objects should have.
/// </summary>
public abstract class BusinessObject
{
    /// <summary>
    /// The business object's unique id.
    /// </summary>
    [Required, Key]
    public virtual Guid Id { get; set; }

    /// <summary>
    /// The business object's status.
    /// </summary>
    [Required]
    public virtual BusinessObjectStatus Status { get; set; }
}
