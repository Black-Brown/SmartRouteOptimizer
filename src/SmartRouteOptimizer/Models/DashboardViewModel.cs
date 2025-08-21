using SmartRouteOptimizer.Models;
using Route = SmartRouteOptimizer.Models.Route;

namespace SmartRouteOptimizer.ViewModels
{
    public class DashboardViewModel
    {
        public List<PerformanceMetrics> RealtimeMetrics { get; set; } = new();
        public OptimizationResult BestResult { get; set; }
        public List<OptimizationResult> ComparisonResults { get; set; } = new();
        public Dictionary<ParallelizationStrategy, double> StrategyEfficiency { get; set; } = new();
        public List<SmartRouteOptimizer.Models.Route> ActiveRoutes { get; set; } = new();
        public SharedData CurrentSharedData { get; set; }
    }
}