namespace Todo.Vsa.Api.Infrastructure.ResultHelper;

/// <summary>
/// Represents an application error with a code and description.
/// </summary>
/// <param name="Code">The error code.</param>
/// <param name="Description">The error description.</param>
public sealed record Error(string Code, string Description)
{
    /// <summary>
    /// Represents no error, with an empty code and description.
    /// </summary>
    public static readonly Error None = new(string.Empty, string.Empty);
}
