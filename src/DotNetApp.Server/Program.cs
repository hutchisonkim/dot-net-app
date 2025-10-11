using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Minimal API configuration
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// SignalR for real-time game communication
builder.Services.AddSignalR();

// Core + application services
// Register core-like services directly (DotNetApp.Core project removed in this workspace).
builder.Services
    .AddSingleton<DotNetApp.Core.Abstractions.IHealthService, DotNetApp.Server.Services.DefaultHealthService>();

// Allow CORS for local testing (replace with tighter policy in prod)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

// Client assets retired: server now serves API only. If you need static files, call UseStaticFiles() here.

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.MapControllers();
app.MapHub<DotNetApp.Server.Hubs.GameHub>("/gamehub");

app.Run();

// Expose Program type for WebApplicationFactory-based tests
// This pattern is required for .NET 6+ with top-level statements to make the implicit
// Program class accessible to integration tests using WebApplicationFactory.
// See: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests
// Verified by: ProgramEntryPointTests in DotNetApp.Server.Tests.Integration
namespace DotNetApp.Server
{
    public partial class Program { }
}
