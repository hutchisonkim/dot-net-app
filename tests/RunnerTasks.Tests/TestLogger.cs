using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace RunnerTasks.Tests
{
    public interface ITestLogger
    {
        IReadOnlyList<object> Logs { get; }
        bool Contains(Microsoft.Extensions.Logging.LogLevel level, string substring);
        IEnumerable<string> GetLastMessages(int count);
        IEnumerable<Dictionary<string, object?>> GetStructuredEntries();
    }

    public class TestLogger<T> : ILogger<T>, ITestLogger
    {
        private readonly List<LogEntry> _logs = new List<LogEntry>();
    private readonly Queue<string> _rolling = new Queue<string>();
        private readonly int _rollingCapacity;

        public TestLogger(int rollingCapacity = 200)
        {
            _rollingCapacity = rollingCapacity;
        }

    IReadOnlyList<object> ITestLogger.Logs => _logs;
    public IReadOnlyList<LogEntry> Logs => _logs;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = formatter != null ? formatter(state, exception) : Convert.ToString(state) ?? string.Empty;
            var timestamp = DateTimeOffset.UtcNow;
            _logs.Add(new LogEntry(logLevel, eventId, message, exception, state, timestamp));

            // maintain rolling buffer with ISO timestamp prefix
            var line = $"{timestamp:O} {message}";
            lock (_rolling)
            {
                _rolling.Enqueue(line);
                while (_rolling.Count > _rollingCapacity) _rolling.Dequeue();
            }
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

        public IEnumerable<string> GetLastMessages(int count)
        {
            lock (_rolling)
            {
                return _rolling.Reverse().Take(count).ToArray();
            }
        }

        public IEnumerable<Dictionary<string, object?>> GetStructuredEntries()
        {
            var results = new List<Dictionary<string, object?>>();
            foreach (var log in _logs)
            {
                var msg = log.Message?.Trim();
                if (string.IsNullOrEmpty(msg)) continue;

                if ((msg.StartsWith("{") && msg.EndsWith("}")) || (msg.StartsWith("[") && msg.EndsWith("]")))
                {
                    try
                    {
                        var parsed = System.Text.Json.JsonSerializer.Deserialize<object>(msg);
                        if (parsed is System.Text.Json.JsonElement je)
                        {
                            if (je.ValueKind == System.Text.Json.JsonValueKind.Object)
                            {
                                var dict = new Dictionary<string, object?>();
                                foreach (var prop in je.EnumerateObject())
                                {
                                    dict[prop.Name] = JsonElementToObject(prop.Value);
                                }
                                results.Add(dict);
                            }
                            else
                            {
                                results.Add(new Dictionary<string, object?> { ["value"] = JsonElementToObject(je) });
                            }
                        }
                    }
                    catch { /* ignore parse errors */ }
                }
            }
            return results;
        }

        private static object? JsonElementToObject(System.Text.Json.JsonElement el)
        {
            switch (el.ValueKind)
            {
                case System.Text.Json.JsonValueKind.String: return el.GetString();
                case System.Text.Json.JsonValueKind.Number:
                    if (el.TryGetInt64(out var l)) return l;
                    if (el.TryGetDouble(out var d)) return d;
                    return el.GetDecimal();
                case System.Text.Json.JsonValueKind.True: return true;
                case System.Text.Json.JsonValueKind.False: return false;
                case System.Text.Json.JsonValueKind.Null: return null;
                case System.Text.Json.JsonValueKind.Object:
                    var dict = new Dictionary<string, object?>();
                    foreach (var prop in el.EnumerateObject()) dict[prop.Name] = JsonElementToObject(prop.Value);
                    return dict;
                case System.Text.Json.JsonValueKind.Array:
                    var list = new List<object?>();
                    foreach (var v in el.EnumerateArray()) list.Add(JsonElementToObject(v));
                    return list;
                default: return el.GetRawText();
            }
        }

        public class LogEntry
        {
            public LogLevel Level { get; }
            public EventId EventId { get; }
            public string? Message { get; }
            public Exception? Exception { get; }
            public object? State { get; }
            public DateTimeOffset Timestamp { get; }

            public LogEntry(LogLevel level, EventId eventId, string? message, Exception? exception, object? state, DateTimeOffset timestamp)
            {
                Level = level;
                EventId = eventId;
                Message = message;
                Exception = exception;
                State = state;
                Timestamp = timestamp;
            }
        }
    }
}
