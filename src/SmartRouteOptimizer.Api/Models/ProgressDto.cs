namespace SmartRouteOptimizer.Api.Models;

public class ProgressDto
{
    public Guid SessionId { get; set; }
    public int Evaluaciones { get; set; }
    public double ElapsedSeconds { get; set; }
    public double ProgressPercent { get; set; }
    public List<AlgorithmStatusDto> Algorithms { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}