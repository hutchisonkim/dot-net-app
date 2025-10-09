// ...existing code...
using System;
using System.Linq;
using System.Collections.Generic;
using Xunit;

namespace GitHub.Runner.Docker.Tests
{
    public static class LoggingAssertions
    {
        public static void Contains(ITestLogger logger, string substring)
        {
            if (!logger.Contains(Microsoft.Extensions.Logging.LogLevel.Information, substring) && !logger.Contains(Microsoft.Extensions.Logging.LogLevel.Warning, substring) && !logger.Contains(Microsoft.Extensions.Logging.LogLevel.Error, substring))
            {
                throw new Xunit.Sdk.XunitException($"Expected log containing '{substring}'");
            }
        }

        public static void True(bool condition, string message, Xunit.Abstractions.ITestOutputHelper output, ITestLogger logger)
        {
            output.WriteLine(message);
            if (!condition)
            {
                // Dump a few log lines to help debugging
                foreach (var line in logger.GetLastMessages(20)) output.WriteLine(line);
                throw new Xunit.Sdk.XunitException(message);
            }
        }

        public static void Equal<T>(T expected, T actual, string message, Xunit.Abstractions.ITestOutputHelper output, ITestLogger logger)
        {
            output.WriteLine(message + $" (expected={expected}, actual={actual})");
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                foreach (var line in logger.GetLastMessages(20)) output.WriteLine(line);
                throw new Xunit.Sdk.XunitException(message + $" - expected: {expected}, actual: {actual}");
            }
        }
    }
}