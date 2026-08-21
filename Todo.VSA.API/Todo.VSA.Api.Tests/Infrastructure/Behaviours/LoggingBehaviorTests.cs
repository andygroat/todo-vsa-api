using MediatR;
using Microsoft.Extensions.Logging;
using Todo.Vsa.Api.Infrastructure.Behaviours;

namespace Todo.Vsa.Api.Tests.Infrastructure.Behaviours;

/// <summary>
/// Tests for <see cref="LoggingBehavior{TRequest, TResponse}"/>.
/// </summary>
public class LoggingBehaviorTests
{
    private sealed record SampleRequest(string Value) : IRequest<string>;

    /// <summary>
    /// Minimal in-memory <see cref="ILogger{TCategoryName}"/> that captures log entries
    /// so tests can assert on level, message template, and ordering.
    /// </summary>
    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = new();

        IDisposable? ILogger.BeginScope<TState>(TState state) => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        /// <summary>
        /// Logs a message with the specified log level, event ID, state, exception, and formatter.
        /// </summary>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception)));
        }
    }

    [Test]
    public async Task Handle_ReturnsResponseFromNext()
    {
        // Arrange
        var logger = new RecordingLogger<LoggingBehavior<SampleRequest, string>>();
        var behavior = new LoggingBehavior<SampleRequest, string>(logger);
        RequestHandlerDelegate<string> next = _ => Task.FromResult("expected");

        // Act
        var result = await behavior.Handle(new SampleRequest("hi"), next, CancellationToken.None);

        // Assert
        await Assert.That(result).IsEqualTo("expected");
    }

    [Test]
    public async Task Handle_LogsStartAndCompletionAtInformationLevel()
    {
        // Arrange
        var logger = new RecordingLogger<LoggingBehavior<SampleRequest, string>>();
        var behavior = new LoggingBehavior<SampleRequest, string>(logger);
        RequestHandlerDelegate<string> next = _ => Task.FromResult("ok");

        // Act
        await behavior.Handle(new SampleRequest("hi"), next, CancellationToken.None);

        // Assert
        await Assert.That(logger.Entries.Count).IsEqualTo(2);
        await Assert.That(logger.Entries[0].Level).IsEqualTo(LogLevel.Information);
        await Assert.That(logger.Entries[0].Message).Contains("Starting");
        await Assert.That(logger.Entries[0].Message).Contains(nameof(SampleRequest));
        await Assert.That(logger.Entries[1].Level).IsEqualTo(LogLevel.Information);
        await Assert.That(logger.Entries[1].Message).Contains("Completed");
        await Assert.That(logger.Entries[1].Message).Contains(nameof(SampleRequest));
    }

    [Test]
    public async Task Handle_LogsStartBeforeInvokingNext()
    {
        // Arrange
        var logger = new RecordingLogger<LoggingBehavior<SampleRequest, string>>();
        var behavior = new LoggingBehavior<SampleRequest, string>(logger);

        var logCountWhenNextCalled = -1;
        RequestHandlerDelegate<string> next = _ =>
        {
            logCountWhenNextCalled = logger.Entries.Count;
            return Task.FromResult("ok");
        };

        // Act
        await behavior.Handle(new SampleRequest("hi"), next, CancellationToken.None);

        // Assert
        await Assert.That(logCountWhenNextCalled).IsEqualTo(1);
        await Assert.That(logger.Entries[0].Message).Contains("Starting");
    }

    [Test]
    public async Task Handle_WhenNextThrows_DoesNotLogCompletionAndPropagatesException()
    {
        // Arrange
        var logger = new RecordingLogger<LoggingBehavior<SampleRequest, string>>();
        var behavior = new LoggingBehavior<SampleRequest, string>(logger);
        RequestHandlerDelegate<string> next = _ => throw new InvalidOperationException("boom");

        // Act & Assert
        await Assert.That(async () =>
            await behavior.Handle(new SampleRequest("hi"), next, CancellationToken.None))
            .Throws<InvalidOperationException>();

        await Assert.That(logger.Entries.Count).IsEqualTo(1);
        await Assert.That(logger.Entries[0].Message).Contains("Starting");
    }

    [Test]
    public async Task Handle_PassesCancellationTokenToNext()
    {
        // Arrange
        var logger = new RecordingLogger<LoggingBehavior<SampleRequest, string>>();
        var behavior = new LoggingBehavior<SampleRequest, string>(logger);
        using var cts = new CancellationTokenSource();

        CancellationToken received = default;
        RequestHandlerDelegate<string> next = ct =>
        {
            received = ct;
            return Task.FromResult("ok");
        };

        // Act
        await behavior.Handle(new SampleRequest("hi"), next, cts.Token);

        // Assert
        await Assert.That(received).IsEqualTo(cts.Token);
    }
}
