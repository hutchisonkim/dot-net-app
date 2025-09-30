using DotNetApp.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetApp.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDotNetAppCore(this IServiceCollection services)
    {
        // Placeholder for registering core cross-cutting services in the future.
        return services;
    }
}
