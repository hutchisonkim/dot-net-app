using DotNetApp.Core.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetApp.Api.UnitTests.Fakes;

public class FakeClientAssetConfigurator : IClientAssetConfigurator
{
    public const string Html = "<html><head><title>Fake Index</title></head><body><h1>Fake Frontend</h1></body></html>";

    public void Configure(object appBuilder)
    {
        if (appBuilder is not IApplicationBuilder app) return;
        app.Use(async (ctx, next) =>
        {
            if (ctx.Request.Path == "/" || ctx.Request.Path == "/index.html")
            {
                ctx.Response.ContentType = "text/html";
                await ctx.Response.WriteAsync(Html);
                return;
            }
            await next();
        });
    }
}

public class FakeHealthService : DotNetApp.Core.Abstractions.IHealthService
{
    public const string CustomStatus = "integration-mocked";
    public Task<string> GetStatusAsync(CancellationToken cancellationToken = default) => Task.FromResult(CustomStatus);
}
