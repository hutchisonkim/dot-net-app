using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace DotNetApp.Tests.Shared;

public class LocalStaticFrontendFixture : IAsyncLifetime
{
    private IHost? _host;
    public string FrontendUrl { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var env = Environment.GetEnvironmentVariable("FRONTEND_URL");
        if (!string.IsNullOrWhiteSpace(env))
        {
            FrontendUrl = env!;
            return; // nothing to start
        }

        // Start a minimal Kestrel server serving src/DotNetApp.Client/wwwroot on port 8080
        var wwwroot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "src", "DotNetApp.Client", "wwwroot"));
        if (!Directory.Exists(wwwroot))
        {
            // try upward search (when running from test project directory)
            var dir = Directory.GetCurrentDirectory();
            for (int up = 0; up < 6; up++)
            {
                var candidate = Path.GetFullPath(Path.Combine(dir, "src", "DotNetApp.Client", "wwwroot"));
                if (Directory.Exists(candidate))
                {
                    wwwroot = candidate;
                    break;
                }
                dir = Path.Combine(dir, "..");
            }
        }

        if (!Directory.Exists(wwwroot))
        {
            // nothing to serve; rely on tests to set FRONTEND_URL explicitly
            FrontendUrl = string.Empty;
            return;
        }

        // pick an available ephemeral port on loopback to avoid conflicts and firewall prompts
        int port;
        var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }

        _host = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                // Listen on the loopback interface only to avoid Windows firewall prompts
                webBuilder.UseKestrel(options => options.ListenLocalhost(port));
                webBuilder.Configure(app =>
                {
                    app.UseStaticFiles(new StaticFileOptions
                    {
                        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(wwwroot),
                        RequestPath = string.Empty
                    });
                    // fallback to index.html for SPA routing
                    app.Run(async ctx =>
                    {
                        var index = Path.Combine(wwwroot, "index.html");
                        if (File.Exists(index))
                        {
                            ctx.Response.ContentType = "text/html";
                            using var fs = File.OpenRead(index);
                            ctx.Response.ContentLength = fs.Length;
                            await fs.CopyToAsync(ctx.Response.Body);
                        }
                        else
                        {
                            ctx.Response.StatusCode = 404;
                        }
                    });
                });
            })
            .Build();

        await _host.StartAsync();
        FrontendUrl = $"http://localhost:{port}/";
    }

    public async Task DisposeAsync()
    {
        if (_host != null)
        {
            try
            {
                await _host.StopAsync(TimeSpan.FromSeconds(2));
            }
            catch { }
            _host.Dispose();
            _host = null;
        }
    }
}
