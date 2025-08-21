using SmartRouteOptimizer.Models;

namespace SmartRouteOptimizer.ViewModels
{
    public class OptimizationViewModel
    {
        public List<DeliveryPoint> DeliveryPoints { get; set; } = new();
        public List<Vehicle> Vehicles { get; set; } = new();
        public ParallelizationStrategy SelectedStrategy { get; set; }
        public int MaxThreads { get; set; } = Environment.ProcessorCount;
        public List<OptimizationResult> Results { get; set; } = new();
        public List<PerformanceMetrics> PerformanceData { get; set; } = new();
        public bool IsRunning { get; set; }
        public string Status { get; set; } = "Listo para optimizar";
    }
}