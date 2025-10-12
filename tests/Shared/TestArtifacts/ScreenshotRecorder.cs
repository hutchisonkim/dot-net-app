using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace TestArtifacts
{
    public class StepInfo
    {
        public string Name { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
    }

    public class TestSummary
    {
        public string Library { get; set; } = string.Empty;
        public string TestName { get; set; } = string.Empty;
        public List<StepInfo> Steps { get; set; } = new List<StepInfo>();
        public string TestHtmlFile => TestName.Replace(' ', '_') + ".html";
    }

    public static class ScreenshotRecorder
    {
        private static readonly string Root = Environment.GetEnvironmentVariable("TEST_ARTIFACTS_ROOT") ?? Path.Combine(Directory.GetCurrentDirectory(), "TestResults", "TestArtifacts");

        public static TestSummary StartTest(string library, string testName)
        {
            var dir = Path.Combine(Root, library, Sanitize(testName));
            Directory.CreateDirectory(dir);
            var summary = new TestSummary { Library = library, TestName = testName };
            return summary;
        }

        public static StepInfo RecordSvg(TestSummary summary, string stepName, string svgContent)
        {
            var dir = Path.Combine(Root, summary.Library, Sanitize(summary.TestName));
            Directory.CreateDirectory(dir);
            var fileName = Sanitize(stepName) + ".svg";
            var path = Path.Combine(dir, fileName);
            File.WriteAllText(path, svgContent);
            var info = new StepInfo { Name = stepName, FileName = fileName };
            summary.Steps.Add(info);
            WriteSummary(summary);
            return info;
        }

        private static void WriteSummary(TestSummary summary)
        {
            var dir = Path.Combine(Root, summary.Library, Sanitize(summary.TestName));
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "summary.json");
            File.WriteAllText(path, JsonConvert.SerializeObject(summary, Formatting.Indented));
        }

        private static string Sanitize(string s)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                s = s.Replace(c, '_');
            }
            return s.Replace(' ', '_');
        }
    }
}
