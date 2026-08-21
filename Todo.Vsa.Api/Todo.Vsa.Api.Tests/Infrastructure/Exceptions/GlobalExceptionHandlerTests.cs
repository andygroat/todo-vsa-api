using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Todo.Vsa.Api.Infrastructure.Exceptions;

namespace Todo.Vsa.Api.Tests.Infrastructure.Exceptions;

/// <summary>
/// Tests for <see cref="GlobalExceptionHandler"/>.
/// </summary>
public class GlobalExceptionHandlerTests
{
    /// <summary>
    /// Fake <see cref="IProblemDetailsService"/> that records the context it received and
    /// returns a configurable result from <see cref="TryWriteAsync"/>.
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

    [Test]
    public async Task TryHandleAsync()
    {
        // Arrange
        var problemDetails = new FakeProblemDetailsService();
        var logger = new RecordingLogger<GlobalExceptionHandler>();
        var handler = new GlobalExceptionHandler(problemDetails, logger);
        var exception = new InvalidOperationException("boom");
        var httpContext = CreateHttpContext();

        // Act
        await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        // Sets 500 Status Code
        await Assert.That(httpContext.Response.StatusCode).IsEqualTo(StatusCodes.Status500InternalServerError);
        // Logs Exception At Error Level
        await Assert.That(logger.Entries.Count).IsEqualTo(1);
        await Assert.That(logger.Entries[0].Level).IsEqualTo(LogLevel.Error);
        await Assert.That(logger.Entries[0].Exception).IsEqualTo(exception);
        // Writes Problem Details With Expected Values
        await Assert.That(problemDetails.CallCount).IsEqualTo(1);
        await Assert.That(problemDetails.LastContext).IsNotNull();
        await Assert.That(problemDetails.LastContext!.HttpContext).IsEqualTo(httpContext);
        await Assert.That(problemDetails.LastContext.Exception).IsEqualTo(exception);
        await Assert.That(problemDetails.LastContext.ProblemDetails.Status).IsEqualTo(StatusCodes.Status500InternalServerError);
        await Assert.That(problemDetails.LastContext.ProblemDetails.Detail).IsEqualTo("An unexpected error occurred");
    }

    [Test]
    public async Task TryHandleAsync_WhenProblemDetailsServiceWritesResponse_ReturnsTrue()
    {
        // Arrange
        var problemDetails = new FakeProblemDetailsService(returnValue: true);
        var handler = new GlobalExceptionHandler(problemDetails, new RecordingLogger<GlobalExceptionHandler>());

        // Act
        var handled = await handler.TryHandleAsync(CreateHttpContext(), new Exception(), CancellationToken.None);

        // Assert
        await Assert.That(handled).IsTrue();
    }

    [Test]
    public async Task TryHandleAsync_WhenProblemDetailsServiceCannotWriteResponse_ReturnsFalse()
    {
        // Arrange
        var problemDetails = new FakeProblemDetailsService(returnValue: false);
        var handler = new GlobalExceptionHandler(problemDetails, new RecordingLogger<GlobalExceptionHandler>());

        // Act
        var handled = await handler.TryHandleAsync(CreateHttpContext(), new Exception(), CancellationToken.None);

        // Assert
        await Assert.That(handled).IsFalse();
    }
}
