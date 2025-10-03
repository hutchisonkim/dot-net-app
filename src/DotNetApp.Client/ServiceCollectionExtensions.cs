using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using DotNetApp.Client.Contracts;

namespace DotNetApp.Client;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPlatformApi(this IServiceCollection services, string? baseAddress = null)
    {
        services.AddHttpClient<PlatformApiClient>(client =>
        {
            if (!string.IsNullOrWhiteSpace(baseAddress)) client.BaseAddress = new Uri(baseAddress);
        });
        services.AddScoped<IPlatformApi>(sp => sp.GetRequiredService<PlatformApiClient>());
        services.AddScoped<IHealthStatusProvider, PlatformApiHealthStatusProviderAdapter>();
        return services;
    }

    public static IServiceCollection AddPlatformApi(this IServiceCollection services, HttpMessageHandler handler, string? baseAddress = null)
    {
        services.AddHttpClient<PlatformApiClient>(client =>
        {
            if (!string.IsNullOrWhiteSpace(baseAddress)) client.BaseAddress = new Uri(baseAddress);
        }).ConfigurePrimaryHttpMessageHandler(() => handler);
        services.AddScoped<IPlatformApi>(sp => sp.GetRequiredService<PlatformApiClient>());
        services.AddScoped<IHealthStatusProvider, PlatformApiHealthStatusProviderAdapter>();
        return services;
    }
}
