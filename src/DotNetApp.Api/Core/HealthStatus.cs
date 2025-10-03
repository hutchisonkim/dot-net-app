namespace DotNetApp.Core.Models
{
    public static class HealthStatus
    {
        public static readonly HealthStatusValue Healthy = new("Healthy");

        public sealed record HealthStatusValue(string Status);
    }
}
