namespace SmartRouteOptimizer.Services
{
    public interface IDistanceService
    {
        double CalculateDistance(double lat1, double lon1, double lat2, double lon2);
        TimeSpan EstimateTime(double distance, double averageSpeed = 50.0);
        double EstimateFuelCost(double distance, double fuelEfficiency = 10.0, double fuelPrice = 1.50);
    }
}