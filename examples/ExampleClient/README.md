ExampleClient - DI demonstration

This small Blazor WebAssembly app is included as an example that demonstrates how
dependency injection is used in this repository to provide client-facing
implementations that consume abstractions from `DotNetApp.Core`.

Key points:
- The example project references `src/DotNetApp.Core/DotNetApp.Core.csproj`.
- `ExampleApiClient` implements `DotNetApp.Core.Abstractions.IHealthStatusProvider`.
- In `Program.cs` the concrete client is registered, then the abstraction is
  mapped to the concrete service via `services.AddScoped<IHealthStatusProvider>(sp => sp.GetRequiredService<ExampleApiClient>());`.
- `Pages/Example.razor` injects `IHealthStatusProvider` and displays a status.

Use this project as a template if you need to add additional example clients that
demonstrate how to swap implementations via DI.
