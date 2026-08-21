using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Todo.Vsa.Api.Infrastructure.Exceptions;

namespace Todo.Vsa.Api.Tests.Infrastructure.Exceptions;

/// <summary>
/// Tests for <see cref="ValidationExceptionHandler"/>.
/// </summary>
public class ValidationExceptionHandlerTests
{
    /// <summary>
    /// Fake <see cref="IProblemDetailsService"/> that records the last context and returns a
    /// configurable value from <see cref="TryWriteAsync"/>.
    /// </summary>
    private sealed class FakeProblemDetailsService(bool returnValue = true) : IProblemDetailsService
    {
        public ProblemDetailsContext? LastContext { get; private set; }
        public int CallCount { get; private set; }
        public int WriteAsyncCallCount { get; private set; }

        public ValueTask<bool> TryWriteAsync(ProblemDetailsContext context)
        {
            CallCount++;
            LastContext = context;
            return ValueTask.FromResult(returnValue);
        }

        public ValueTask WriteAsync(ProblemDetailsContext context)
        {
            WriteAsyncCallCount++;
            LastContext = context;
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// Minimal recording <see cref="ILogger{T}"/> capturing the level, formatted message,
    /// and exception passed to each log call.
    /// </summary>
    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = new();

        IDisposable? ILogger.BeginScope<TState>(TState state) => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception), exception));
        }
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();
        return ctx;
    }

    private static ValidationException CreateValidationException(params ValidationFailure[] failures) =>
        new(failures);

    [Test]
    public async Task TryHandleAsync_WhenExceptionIsNotValidationException_ReturnsFalseAndDoesNothing()
    {
        // Arrange
        var problemDetails = new FakeProblemDetailsService();
        var logger = new RecordingLogger<ValidationExceptionHandler>();
        var handler = new ValidationExceptionHandler(problemDetails, logger);
        var httpContext = CreateHttpContext();

        // Act
        var handled = await handler.TryHandleAsync(httpContext, new InvalidOperationException("nope"), CancellationToken.None);

        // Assert
        await Assert.That(handled).IsFalse();
        await Assert.That(problemDetails.CallCount).IsEqualTo(0);
        await Assert.That(logger.Entries).IsEmpty();
        await Assert.That(httpContext.Response.StatusCode).IsEqualTo(200); // Untouched
    }

    [Test]
    public async Task TryHandleAsync_WhenValidationException()
    {
        // Arrange
        var logger = new RecordingLogger<ValidationExceptionHandler>();
        var problemDetails = new FakeProblemDetailsService();
        var handler = new ValidationExceptionHandler(problemDetails, logger);
        var httpContext = CreateHttpContext();
        var exception = CreateValidationException(new ValidationFailure("Name", "must not be empty"));

        // Act
        await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        // Sets 400 Status Code
        await Assert.That(httpContext.Response.StatusCode).IsEqualTo(StatusCodes.Status400BadRequest);
        // Logs ValidationException at Error Level
        await Assert.That(logger.Entries.Count).IsEqualTo(1);
        await Assert.That(logger.Entries[0].Level).IsEqualTo(LogLevel.Error);
        await Assert.That(logger.Entries[0].Exception).IsEqualTo(exception);
        // Writes Problem Details With Expected Values
        await Assert.That(problemDetails.CallCount).IsEqualTo(1);
        await Assert.That(problemDetails.LastContext).IsNotNull();
        await Assert.That(problemDetails.LastContext!.HttpContext).IsEqualTo(httpContext);
        await Assert.That(problemDetails.LastContext.Exception).IsEqualTo(exception);
        await Assert.That(problemDetails.LastContext.ProblemDetails.Status).IsEqualTo(StatusCodes.Status400BadRequest);
        await Assert.That(problemDetails.LastContext.ProblemDetails.Detail).IsEqualTo("One or more validation errors occurred");
    }

    [Test]
    public async Task TryHandleAsync_AddsErrorsExtensionGroupedByLowercasePropertyName()
    {
        // Arrange
        var problemDetails = new FakeProblemDetailsService();
        var handler = new ValidationExceptionHandler(problemDetails, new RecordingLogger<ValidationExceptionHandler>());
        var exception = CreateValidationException(
            new ValidationFailure("Name", "must not be empty"),
            new ValidationFailure("Name", "must be at least 3 characters"),
            new ValidationFailure("Age", "must be greater than 0"));

        // Act
        await handler.TryHandleAsync(CreateHttpContext(), exception, CancellationToken.None);

        // Assert
        var extensions = problemDetails.LastContext!.ProblemDetails.Extensions;
        await Assert.That(extensions.ContainsKey("errors")).IsTrue();

        var errors = (IDictionary<string, string[]>)extensions["errors"]!;
        await Assert.That(errors.ContainsKey("name")).IsTrue();
        await Assert.That(errors.ContainsKey("age")).IsTrue();
        await Assert.That(errors["name"].Length).IsEqualTo(2);
        await Assert.That(errors["name"]).Contains("must not be empty");
        await Assert.That(errors["name"]).Contains("must be at least 3 characters");
        await Assert.That(errors["age"]).Contains("must be greater than 0");
    }

    [Test]
    public async Task TryHandleAsync_WhenProblemDetailsServiceWritesResponse_ReturnsTrue()
    {
        // Arrange
        var handler = new ValidationExceptionHandler(
            new FakeProblemDetailsService(returnValue: true),
            new RecordingLogger<ValidationExceptionHandler>());
        var exception = CreateValidationException(new ValidationFailure("X", "y"));

        // Act
        var handled = await handler.TryHandleAsync(CreateHttpContext(), exception, CancellationToken.None);

        // Assert
        await Assert.That(handled).IsTrue();
    }

    [Test]
    public async Task TryHandleAsync_WhenProblemDetailsServiceCannotWriteResponse_ReturnsFalse()
    {
        // Arrange
        var handler = new ValidationExceptionHandler(
            new FakeProblemDetailsService(returnValue: false),
            new RecordingLogger<ValidationExceptionHandler>());
        var exception = CreateValidationException(new ValidationFailure("X", "y"));

        // Act
        var handled = await handler.TryHandleAsync(CreateHttpContext(), exception, CancellationToken.None);

        // Assert
        await Assert.That(handled).IsFalse();
    }
}
