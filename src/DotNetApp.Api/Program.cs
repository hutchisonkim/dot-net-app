using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Minimal API configuration
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

if (!string.IsNullOrEmpty(clientWwwroot))
{
    // Serve static files from the client wwwroot at application root
    var provider = new PhysicalFileProvider(clientWwwroot);
    var staticOptions = new StaticFileOptions { FileProvider = provider, ServeUnknownFileTypes = true };
    app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = provider });
    app.UseStaticFiles(staticOptions);
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.MapControllers();

app.Run();

// Expose Program type for WebApplicationFactory-based tests
public partial class Program { }
