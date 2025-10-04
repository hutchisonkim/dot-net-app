using Xunit;

namespace DotNetApp.Tests.Shared;

[CollectionDefinition("docker-compose")]
public class DockerComposeCollection : ICollectionFixture<LocalStaticFrontendFixture>
{
    // Collection definition to share the LocalStaticFrontendFixture across tests that
    // use the "docker-compose" collection attribute.
}
