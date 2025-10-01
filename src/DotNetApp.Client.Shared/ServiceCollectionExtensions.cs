using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using DotNetApp.Core.Abstractions;
using DotNetApp.Client.Services;
using DotNetApp.Client.Shared.Contracts;

namespace DotNetApp.Client.Shared;

public static class ServiceCollectionExtensions
{
    [Obsolete("Use AddPlatformApi(baseAddress) instead; will be removed in a future release.")]
    public static IServiceCollection AddDotNetAppClient(this IServiceCollection services, string? explicitBaseAddress = null, string? fallbackBaseAddress = null)
    {
        // Legacy signature preserved; forward to AddPlatformApi.
        var baseAddress = explicitBaseAddress ?? fallbackBaseAddress;
        return services.AddPlatformApi(baseAddress ?? "http://localhost/");
    }

    public static IServiceCollection AddPlatformApi(this IServiceCollection services, string? baseAddress = null)
    {
        services.AddHttpClient<PlatformApiClient>(client =>
        {
            if (!string.IsNullOrWhiteSpace(baseAddress))
                client.BaseAddress = new Uri(baseAddress);
        });
        services.AddScoped<IPlatformApi>(sp => sp.GetRequiredService<PlatformApiClient>());
        // Map legacy abstraction to new contract implementation
        services.AddScoped<IHealthStatusProvider, PlatformApiHealthStatusProviderAdapter>();
        return services;
    }

    // Test helper overload allowing injection of a custom HttpMessageHandler
    public static IServiceCollection AddPlatformApi(this IServiceCollection services, HttpMessageHandler handler, string? baseAddress = null)
    {
        services.AddHttpClient<PlatformApiClient>(client =>
        {
            if (!string.IsNullOrWhiteSpace(baseAddress))
                client.BaseAddress = new Uri(baseAddress);
        }).ConfigurePrimaryHttpMessageHandler(() => handler);
        services.AddScoped<IPlatformApi>(sp => sp.GetRequiredService<PlatformApiClient>());
        services.AddScoped<IHealthStatusProvider, PlatformApiHealthStatusProviderAdapter>();
        return services;
    }
}
