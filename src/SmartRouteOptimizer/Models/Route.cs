using SmartRouteOptimizer.Models;

namespace SmartRouteOptimizer.Models
{
    public class Route
    {
        public int Id { get; set; }
        public int VehicleId { get; set; }
        public List<DeliveryPoint> DeliverySequence { get; set; } = new List<DeliveryPoint>();
        public double TotalDistance { get; set; }
        public TimeSpan EstimatedTime { get; set; }
        public double FuelCost { get; set; }
        public int DeliveriesOnTime { get; set; }
        public string OptimizationStrategy { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}