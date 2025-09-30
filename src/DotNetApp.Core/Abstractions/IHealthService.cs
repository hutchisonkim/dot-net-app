namespace DotNetApp.Core.Abstractions;

public interface IHealthService
{
    Task<string> GetStatusAsync(CancellationToken cancellationToken = default);
}

// Client consumption abstraction for retrieving health status via an API boundary.
public interface IHealthStatusProvider
{
    Task<string?> FetchStatusAsync(CancellationToken cancellationToken = default);
}

// Abstraction for configuring how client (frontend) assets are served by the backend host.
// Enables swapping Blazor static hosting for another frontend (e.g., React, Angular) without changing Program.cs.
public interface IClientAssetConfigurator
{
    void Configure(object appBuilder);
}
