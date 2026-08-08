using System.Diagnostics.CodeAnalysis;

namespace Todo.VSA.Api.Infrastructure.ResultHelper;

/// <summary>
/// Represents the result of an operation, indicating success or failure and providing an associated error if applicable.
/// </summary>
public class Result
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Result"/> class with the specified success status and error.
    /// </summary>
    /// <param name="isSuccess">Indicates whether the operation was successful.</param>
    /// <param name="error">The associated error if the operation failed.</param>
    /// <exception cref="ArgumentException">Thrown when the combination of isSuccess and error is invalid.</exception>
    protected Result(bool isSuccess, Error error)
    {
        // Validate the combination of isSuccess and error
        if (isSuccess && error != Error.None ||
            !isSuccess && error == Error.None)
        {
            throw new ArgumentException("Invalid error", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the associated error if the operation failed. If the operation was successful, this property will be <see cref="Error.None"/>.
    /// </summary>
    public Error Error { get; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A <see cref="Result"/> representing a successful operation.</returns>
    public static Result Success() => new(true, Error.None);

    /// <summary>
    /// Creates a successful result with the specified value.
    /// </summary>
    /// <typeparam name="T">The type of the value returned by the operation.</typeparam>
    /// <param name="value">The value returned by the operation.</param>
    /// <returns>A <see cref="Result{T}"/> representing a successful operation with the specified value.</returns>
    public static Result<T> Success<T>(T value) => new(value, true, Error.None);

    /// <summary>
    /// Creates a failed result with the specified error.
    /// </summary>
    /// <param name="error">The error associated with the failure.</param>
    /// <returns>A <see cref="Result"/> representing a failed operation.</returns>
    public static Result Failure(Error error) => new(false, error);

    /// <summary>
    /// Creates a failed result with the specified error and no value.
    /// </summary>
    /// <typeparam name="T">The type of the value returned by the operation.</typeparam>
    /// <param name="error">The error associated with the failure.</param>
    /// <returns>A <see cref="Result{T}"/> representing a failed operation with the specified error and no value.</returns>
    public static Result<T> Failure<T>(Error error) => new(default, false, error);
}

/// <summary>
/// Represents the result of an operation that returns a value, indicating success or failure and providing an associated error if applicable.
/// </summary>
/// <typeparam name="T">The type of the value returned by the operation.</typeparam>
/// <param name="value">The value returned by the operation.</param>
/// <param name="isSuccess">Indicates whether the operation was successful.</param>
/// <param name="error">The error associated with the failure.</param>
public class Result<T>(T? value, bool isSuccess, Error error) : Result(isSuccess, error)
{
    /// <summary>
    /// Gets the value returned by the operation.
    /// </summary>
    private readonly T? _value = value;

    /// <summary>
    /// Gets the value returned by the operation if it was successful. If the operation failed, accessing this property will throw an <see cref="InvalidOperationException"/>.
    /// </summary>
    [NotNull]
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("The value of a failure result can't be accessed.");
}