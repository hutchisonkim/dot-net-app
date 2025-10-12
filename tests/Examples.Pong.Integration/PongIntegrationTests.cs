using Xunit;

namespace Examples.Pong.Integration.Tests
{
    [Trait("Category","Integration")]
    public class PongIntegrationTests
    {
        [Fact]
        public void Integration_Smoke()
        {
            Assert.True(true);
        }
    }
}
