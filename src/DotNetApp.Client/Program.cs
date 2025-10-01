using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using DotNetApp.Client;
using DotNetApp.Client.Shared;
using Microsoft.Extensions.DependencyInjection;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Register platform-agnostic ApiClient & provider mapping (base address resolution handled inside extension)
builder.Services.AddDotNetAppClient(fallbackBaseAddress: builder.HostEnvironment.BaseAddress);

await builder.Build().RunAsync();

