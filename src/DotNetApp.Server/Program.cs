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
    .AddScoped<DotNetApp.Core.Abstractions.IHealthService, DotNetApp.Server.Services.DefaultHealthService>()
    .AddSingleton<DotNetApp.Core.Abstractions.IClientAssetConfigurator, DotNetApp.Server.Services.BlazorClientAssetConfigurator>();

// Allow CORS for local testing (replace with tighter policy in prod)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

// Allow pluggable client asset configuration (BlazorClientAssetConfigurator will discover and wire static files)
var assetConfigurator = app.Services.GetRequiredService<DotNetApp.Core.Abstractions.IClientAssetConfigurator>();
assetConfigurator.Configure(app);

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.MapControllers();

app.Run();

// Expose Program type for WebApplicationFactory-based tests
public partial class Program { }
