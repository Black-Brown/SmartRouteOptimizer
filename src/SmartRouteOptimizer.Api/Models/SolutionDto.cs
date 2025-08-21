namespace LastMileOptimizer.Api.Models;
public class SolutionDto
{
    public Guid SessionId { get; set; }
    public List<AlgorithmResultDto> Results { get; set; } = new();
    public AlgorithmResultDto? Best { get; set; }


    // Métricas agregadas
    public double BestEfficiencyPercent { get; set; }
    public double CostPerDelivery { get; set; }


    // Rutas visuales
    public List<RouteDto> Routes { get; set; } = new();
}