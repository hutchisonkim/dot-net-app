using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ExampleClient;
using ExampleClient.Services;
using DotNetApp.Core.Abstractions;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Register example typed client and expose it via the repo-level abstraction
builder.Services.AddScoped<ExampleApiClient>();
builder.Services.AddScoped<IHealthStatusProvider>(sp => sp.GetRequiredService<ExampleApiClient>());

await builder.Build().RunAsync();
