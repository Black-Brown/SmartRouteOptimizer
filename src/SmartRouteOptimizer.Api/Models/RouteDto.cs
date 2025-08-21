namespace LastMileOptimizer.Api.Models;
public class RouteDto
{
    public int VehicleId { get; set; }
    public string VehicleColor { get; set; } = "#000000";
    public List<double[]> Coordinates { get; set; } = new(); // [[lat,lng], ...]
    public int Deliveries { get; set; }
    public double EstimatedDistanceKm { get; set; }
}