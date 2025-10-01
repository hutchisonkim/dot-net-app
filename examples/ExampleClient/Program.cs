using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ExampleClient;
using DotNetApp.Client.Shared;
using DotNetApp.Client.Shared.Contracts;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddPlatformApi(builder.HostEnvironment.BaseAddress);

await builder.Build().RunAsync();
