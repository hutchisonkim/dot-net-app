using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DotNetApp.Core.Abstractions;
using DotNetApp.Client.Services;

namespace DotNetApp.Client.Shared;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDotNetAppClient(this IServiceCollection services, string? explicitBaseAddress = null, string? fallbackBaseAddress = null)
    {
        services.AddScoped<ApiClient>();
        services.AddScoped<IHealthStatusProvider>(sp => sp.GetRequiredService<ApiClient>());
        services.AddScoped(sp =>
        {
            var cfg = sp.GetRequiredService<IConfiguration>();
            var envApiBase = Environment.GetEnvironmentVariable("FRONTEND_API_BASE");
            var configApiBase = cfg["ApiBaseAddress"];
            var resolved = explicitBaseAddress ?? envApiBase ?? configApiBase ?? fallbackBaseAddress ?? "http://localhost/";
            return new HttpClient { BaseAddress = new Uri(resolved) };
        });
        return services;
    }
}
