using Mediator;

using Microsoft.Extensions.Logging;

using PharmaFlow.Application.Common.Behaviors;
using PharmaFlow.Domain.Common;

namespace PharmaFlow.Tests.Unit.Behaviors;

public class LoggingBehaviorTests
{
    [Fact]
    public async Task Successful_request_logs_information_with_elapsed_ms()
    {
        var logger = new TestLogger<LoggingBehavior<TestRequest, Result>>();
        var behavior = new LoggingBehavior<TestRequest, Result>(logger);
        MessageHandlerDelegate<TestRequest, Result> next =
            (_, _) => ValueTask.FromResult(Result.Success());

        var result = await behavior.Handle(new TestRequest(), next, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Contains("TestRequest", entry.Message, StringComparison.Ordinal);
        Assert.Contains("succeeded", entry.Message, StringComparison.Ordinal);
        Assert.Null(entry.Exception);
    }

    [Fact]
    public async Task Failed_result_logs_warning_with_error_code()
    {
        var logger = new TestLogger<LoggingBehavior<TestRequest, Result>>();
        var behavior = new LoggingBehavior<TestRequest, Result>(logger);
        var failure = Result.Failure(Error.Validation("test.fail", "boom"));
        MessageHandlerDelegate<TestRequest, Result> next =
            (_, _) => ValueTask.FromResult(failure);

        var result = await behavior.Handle(new TestRequest(), next, CancellationToken.None);

        Assert.True(result.IsFailure);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Contains("TestRequest", entry.Message, StringComparison.Ordinal);
        Assert.Contains("test.fail", entry.Message, StringComparison.Ordinal);
        Assert.Null(entry.Exception);
    }

    [Fact]
    public async Task Thrown_exception_logs_error_and_rethrows()
    {
        var logger = new TestLogger<LoggingBehavior<TestRequest, Result>>();
        var behavior = new LoggingBehavior<TestRequest, Result>(logger);
        var boom = new InvalidOperationException("boom");
        MessageHandlerDelegate<TestRequest, Result> next =
            (_, _) => throw boom;

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => behavior.Handle(new TestRequest(), next, CancellationToken.None).AsTask());

        Assert.Same(boom, thrown);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Same(boom, entry.Exception);
        Assert.Contains("TestRequest", entry.Message, StringComparison.Ordinal);
    }

    public sealed record TestRequest : IRequest<Result>;

    private sealed record LogEntry(LogLevel Level, EventId EventId, string Message, Exception? Exception);

    private sealed class TestLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new LogEntry(logLevel, eventId, formatter(state, exception), exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}
