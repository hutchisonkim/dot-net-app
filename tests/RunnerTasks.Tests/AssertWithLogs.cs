using System;
using System.Diagnostics;
using Xunit.Abstractions;

namespace RunnerTasks.Tests
{
    public static class AssertWithLogs
    {
        // Asserts condition is true; on failure dumps lastN logs and fails with header including timestamp and test name.
        public static void True(bool condition, string message, ITestOutputHelper output, ITestLogger logger, int lastN = 50, string? extraInfo = null)
        {
            if (condition) return;

            var ts = DateTimeOffset.UtcNow;
            var testName = DetectTestName();
            var header = $"{ts:O} [{testName}] {message}" + (string.IsNullOrEmpty(extraInfo) ? string.Empty : $" - {extraInfo}");
            TestAssert.FailWithLogs(header, output, logger, lastN);
        }

        // Asserts equality; on failure dumps logs and fails with header
        public static void Equal<T>(T expected, T actual, string message, ITestOutputHelper output, ITestLogger logger, int lastN = 50, string? extraInfo = null)
        {
            if (object.Equals(expected, actual)) return;

            var ts = DateTimeOffset.UtcNow;
            var testName = DetectTestName();
            var header = $"{ts:O} [{testName}] {message} (expected: {expected}, actual: {actual})" + (string.IsNullOrEmpty(extraInfo) ? string.Empty : $" - {extraInfo}");
            TestAssert.FailWithLogs(header, output, logger, lastN);
        }

        private static string DetectTestName()
        {
            try
            {
                var st = new StackTrace();
                for (int i = 0; i < st.FrameCount; i++)
                {
                    var frame = st.GetFrame(i);
                    if (frame == null) continue;
                    var m = frame.GetMethod();
                    if (m == null) continue;
                    var name = m.Name;
                    if (name.Contains("Test") || name.Contains("Integration") || name.Contains("Should") || m.GetCustomAttributes(false).Length > 0)
                        return name;
                }
            }
            catch { }
            return "UnknownTest";
        }
    }
}
