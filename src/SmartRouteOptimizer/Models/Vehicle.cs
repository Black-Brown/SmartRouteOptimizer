using SmartRouteOptimizer.Models;

namespace SmartRouteOptimizer.Models
{
    public class Vehicle
    {
        public int Id { get; set; }
        public string DriverName { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double MaxCapacity { get; set; }
        public double CurrentLoad { get; set; }
        public bool IsAvailable { get; set; }
        public List<DeliveryPoint> AssignedDeliveries { get; set; } = new List<DeliveryPoint>();
    }
}