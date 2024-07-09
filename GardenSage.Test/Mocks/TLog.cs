
using System.Collections.Concurrent;

using Microsoft.Extensions.Logging;

using Xunit.Abstractions;

namespace GardenSage.Test.Mocks;
public sealed class XUnitTestOutputLoggerProvider : ILoggerProvider
{
    private readonly ITestOutputHelper _output;
    private ConcurrentDictionary<string, ILogger> LoggerPool { get; } = [];

    public XUnitTestOutputLoggerProvider(ITestOutputHelper output)
    {
        _output = output;
    }

    public ILogger CreateLogger(string categoryName)
    {
        _ = LoggerPool.TryAdd(categoryName, new XUnitTestOutputLogger(_output, categoryName.Split('.').Last()));
        return LoggerPool[categoryName];
    }

    public void Dispose()
    {
        // Clean up resources if needed
        LoggerPool.Clear();
    }
}


public class XUnitTestOutputLogger : ILogger
{
    private readonly ITestOutputHelper _output;

    public XUnitTestOutputLogger(ITestOutputHelper output, string? category = null)
    {
        _output = output;
        Category = category;
    }

    public string? Category { get; }
    private ConcurrentStack<object> Scopes { get; } = [];

    readonly struct Ephemeral<TState> : IDisposable where TState : notnull
    {
        private readonly XUnitTestOutputLogger _source;

        public Ephemeral(XUnitTestOutputLogger source, TState state)
        {
            _source = source;
            State = state;
            _source.Scopes.Push(state);
        }

        readonly TState State { get; }
        public readonly void Dispose()
        {
            lock (_source)
            {
                if (_source.Scopes.TryPeek(out var topstate) && topstate is TState tstateValue && tstateValue.Equals(State))
                    _ = _source.Scopes.TryPop(out var _);
            }
        }
    }

    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        return new Ephemeral<TState>(this, state);
    }

    IDisposable? ILogger.BeginScope<TState>(TState state) => null;

    bool ILogger.IsEnabled(LogLevel logLevel) => true;

    void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state,
        Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        if (false)
        {
            _output.WriteLine($"[{logLevel}]{Category} {message}");
        }
        else
        {
            _output.WriteLine($"[{logLevel}] {Category} {string.Join(',', Scopes)}");
            foreach (string line in message.Split(Environment.NewLine))
            {
                _output.WriteLine($"    {line}");
            }
        }
    }
}
