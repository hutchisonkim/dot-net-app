using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using DotNetApp.Client;
using DotNetApp.Client.Contracts;
using Microsoft.Extensions.DependencyInjection;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Register generated PlatformApi client (also maps IHealthStatusProvider via adapter)
builder.Services.AddPlatformApi(builder.HostEnvironment.BaseAddress);

await builder.Build().RunAsync();

