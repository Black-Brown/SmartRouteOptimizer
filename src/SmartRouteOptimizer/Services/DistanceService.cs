using SmartRouteOptimizer.Services;

namespace SmartRouteOptimizer.Services
{
    public class DistanceService : IDistanceService
    {
        public double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            // Fórmula de Haversine para calcular distancia entre coordenadas
            const double R = 6371; // Radio de la Tierra en kilómetros

            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c;
        }

        public TimeSpan EstimateTime(double distance, double averageSpeed = 50.0)
        {
            var hours = distance / averageSpeed;
            return TimeSpan.FromHours(hours);
        }

        public double EstimateFuelCost(double distance, double fuelEfficiency = 10.0, double fuelPrice = 1.50)
        {
            var fuelNeeded = distance / fuelEfficiency;
            return fuelNeeded * fuelPrice;
        }

        private static double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }
    }
}