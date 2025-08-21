using System.ComponentModel.DataAnnotations;

namespace SmartRouteOptimizer.Models
{
    public class DeliveryPoint
    {
        public int Id { get; set; }

        [Required]
        public string Address { get; set; }

        [Range(-90, 90)]
        public double Latitude { get; set; }

        [Range(-180, 180)]
        public double Longitude { get; set; }

        public TimeSpan DeliveryWindow { get; set; }
        public int Priority { get; set; } // 1 = Alta, 2 = Media, 3 = Baja
        public double PackageWeight { get; set; }
        public bool IsDelivered { get; set; }
        public DateTime? DeliveredAt { get; set; }
    }
}
