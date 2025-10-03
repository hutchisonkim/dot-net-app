using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using DotNetApp.Core;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Minimal API configuration
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Core + application services
// Register core-like services directly (DotNetApp.Core project removed in this workspace).
builder.Services
    .AddScoped<DotNetApp.Core.Abstractions.IHealthService, DotNetApp.Api.Services.DefaultHealthService>()
    .AddSingleton<DotNetApp.Core.Abstractions.IClientAssetConfigurator, DotNetApp.Api.Services.BlazorClientAssetConfigurator>();

// Allow CORS for local testing (replace with tighter policy in prod)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

// If a Blazor client build or source wwwroot exists in the repo, serve it as static files
// This allows running integration tests locally without Docker by having the API host the static client files.
var candidateClientWwwroots = new[] {
    // Common build output for the client project (bin/Debug/net8.0/wwwroot)
    Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "..", "src", "DotNetApp.Client", "bin", "Debug", "net8.0", "wwwroot")),
    // Client source wwwroot
    Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "..", "src", "DotNetApp.Client", "wwwroot"))
};

// Prefer a candidate wwwroot that actually contains an index.html so DefaultFiles will serve '/'
string? clientWwwroot = null;
foreach (var candidate in candidateClientWwwroots)
{
    var indexPath = Path.Combine(candidate, "index.html");
    if (File.Exists(indexPath))
    {
        clientWwwroot = candidate;
        break;
    }
}

// Allow pluggable client asset configuration
var assetConfigurator = app.Services.GetRequiredService<DotNetApp.Core.Abstractions.IClientAssetConfigurator>();
assetConfigurator.Configure(app);

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.MapControllers();

app.Run();

// Expose Program type for WebApplicationFactory-based tests
public partial class Program { }
