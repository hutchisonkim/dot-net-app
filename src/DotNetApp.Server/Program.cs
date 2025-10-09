using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Minimal API configuration
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Core + application services
// Register core-like services directly (DotNetApp.Core project removed in this workspace).
builder.Services
    .AddScoped<DotNetApp.Core.Abstractions.IHealthService, DotNetApp.Server.Services.DefaultHealthService>();

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

app.Run();

// Expose Program type for WebApplicationFactory-based tests
namespace DotNetApp.Server
{
    public partial class Program { }
}
