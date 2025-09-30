using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Xunit;

namespace DotNetApp.Api.UnitTests;

public class StateControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public StateControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_Returns_Healthy()
    {
        var res = await _client.GetAsync("/api/state/health");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var obj = await res.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        obj.GetProperty("status").GetString().Should().Be("healthy");
    }
}

