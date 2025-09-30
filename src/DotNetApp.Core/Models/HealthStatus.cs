namespace DotNetApp.Core.Models;

public record HealthStatus(string Status)
{
    public static readonly HealthStatus Healthy = new("healthy");
}
