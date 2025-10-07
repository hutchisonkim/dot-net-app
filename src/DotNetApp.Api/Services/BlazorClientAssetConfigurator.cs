using DotNetApp.Core.Abstractions;
using Microsoft.Extensions.FileProviders;

namespace DotNetApp.Api.Services;

public class BlazorClientAssetConfigurator : IClientAssetConfigurator
{
    public void Configure(object appBuilder)
    {
        if (appBuilder is not IApplicationBuilder app) return;
        var env = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();
        var candidateClientWwwroots = new[] {
            Path.GetFullPath(Path.Combine(env.ContentRootPath, "..", "..", "src", "DotNetApp.Client", "bin", "Debug", "net8.0", "wwwroot")),
            Path.GetFullPath(Path.Combine(env.ContentRootPath, "..", "..", "src", "DotNetApp.Client", "wwwroot"))
        };

        string? clientWwwroot = candidateClientWwwroots.FirstOrDefault(c => File.Exists(Path.Combine(c, "index.html")));
        if (clientWwwroot is null) return;

        var provider = new PhysicalFileProvider(clientWwwroot);
        app.Use(async (ctx, next) =>
        {
            await next();
        });
        app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = provider });
        app.UseStaticFiles(new StaticFileOptions { FileProvider = provider, ServeUnknownFileTypes = true });
    }
}
