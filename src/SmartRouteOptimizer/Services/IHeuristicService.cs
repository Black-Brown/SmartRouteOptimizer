using SmartRouteOptimizer.Models;

namespace SmartRouteOptimizer.Services
{
    public interface IHeuristicService
    {
        Task<Route> NearestNeighborAsync(Vehicle vehicle, List<DeliveryPoint> deliveries, SharedData sharedData);
        Task<Route> GreedyTimeBasedAsync(Vehicle vehicle, List<DeliveryPoint> deliveries, SharedData sharedData);
        Task<Route> PriorityBasedAsync(Vehicle vehicle, List<DeliveryPoint> deliveries, SharedData sharedData);
        Task<Route> GeneticAlgorithmAsync(Vehicle vehicle, List<DeliveryPoint> deliveries, SharedData sharedData);
        Task<Route> RandomizedGreedyAsync(Vehicle vehicle, List<DeliveryPoint> deliveries, SharedData sharedData, Random random);
    }
}