namespace SmartRouteOptimizer.Api.Models
{
    public class OptimizationRequest
    {
        public required List<ClientDto> Clients { get; set; }
        public required List<VehicleDto> Vehicles { get; set; }
        public int TimeLimitSeconds { get; set; } = 30;
    }
}
