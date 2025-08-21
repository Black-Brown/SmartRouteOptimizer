namespace SmartRouteOptimizer.Api.Models;
public class AlgorithmResultDto
{
    public string Algorithm { get; set; } = string.Empty;
    public double Cost { get; set; }
    public double DistanceKm { get; set; }
    public double TimeSeconds { get; set; }
}