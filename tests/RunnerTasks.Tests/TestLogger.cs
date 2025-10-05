using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace RunnerTasks.Tests
{
    public class TestLogger<T> : ILogger<T>
    {
        private readonly List<LogEntry> _logs = new List<LogEntry>();

        public IReadOnlyList<LogEntry> Logs => _logs;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = formatter != null ? formatter(state, exception) : Convert.ToString(state) ?? string.Empty;
            _logs.Add(new LogEntry(logLevel, eventId, message, exception, state));
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        IDisposable? ILogger.BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool Contains(LogLevel level, string substring)
        {
            return _logs.Any(l => l.Level == level && (l.Message?.Contains(substring) ?? false));
        }

        public class LogEntry
        {
            public LogLevel Level { get; }
            public EventId EventId { get; }
            public string? Message { get; }
            public Exception? Exception { get; }
            public object? State { get; }

            public LogEntry(LogLevel level, EventId eventId, string? message, Exception? exception, object? state)
            {
                Level = level;
                EventId = eventId;
                Message = message;
                Exception = exception;
                State = state;
            }
        }
    }
}
