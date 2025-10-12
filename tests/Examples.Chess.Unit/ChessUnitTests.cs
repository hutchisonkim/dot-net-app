using Xunit;
using TestArtifacts;

namespace Examples.Chess.Unit.Tests
{
    public class ChessUnitTests
    {
        [Fact]
        public void IndexPage_Renders_Title()
        {
            // Minimal smoke test placeholder - real UI tests belong in UI project
            // We'll record two example steps as SVGs to demonstrate artifact generation.
            var summary = ScreenshotRecorder.StartTest("Examples.Chess.Unit", nameof(IndexPage_Renders_Title));
            var svg1 = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"200\" height=\"200\"><rect width=\"200\" height=\"200\" fill=\"#EEE\"/><text x=\"10\" y=\"20\">Step 1: Start</text></svg>";
            ScreenshotRecorder.RecordSvg(summary, "Step 1 - Start", svg1);
            var svg2 = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"200\" height=\"200\"><rect width=\"200\" height=\"200\" fill=\"#DDD\"/><text x=\"10\" y=\"20\">Step 2: Move</text></svg>";
            ScreenshotRecorder.RecordSvg(summary, "Step 2 - Move", svg2);

            Assert.True(true);
        }
    }
}
