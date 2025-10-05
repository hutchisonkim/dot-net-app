using System;
using System.Linq;
using Xunit.Abstractions;

namespace RunnerTasks.Tests
{
    public static class TestAssert
    {
        public static void FailWithLogs(string message, ITestOutputHelper output, ITestLogger logger, int lastN = 50)
        {
            output.WriteLine(message);
            output.WriteLine("--- Last log lines ---");
            foreach (var line in logger.GetLastMessages(lastN))
            {
                output.WriteLine(line);
            }
            output.WriteLine("--- End logs ---");
            throw new Xunit.Sdk.XunitException(message);
        }
    }
}
