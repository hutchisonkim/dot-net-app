using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using DotNetApp.Client;
using DotNetApp.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Allow configuring the API base address via environment/configuration for tests/containers/CI.
// Priority: FRONTEND_API_BASE env var -> configuration key "ApiBaseAddress" -> builder.HostEnvironment.BaseAddress
var envApiBase = Environment.GetEnvironmentVariable("FRONTEND_API_BASE");
var configApiBase = builder.Configuration["ApiBaseAddress"];
var baseAddress = envApiBase ?? configApiBase ?? builder.HostEnvironment.BaseAddress;

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(baseAddress) });

// Register ApiClient that encapsulates API calls and uses the centralized HttpClient
builder.Services.AddScoped<ApiClient>();
builder.Services.AddScoped<DotNetApp.Core.Abstractions.IHealthStatusProvider>(sp => sp.GetRequiredService<ApiClient>());

await builder.Build().RunAsync();

