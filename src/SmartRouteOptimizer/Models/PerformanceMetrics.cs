using SmartRouteOptimizer.Models;

namespace SmartRouteOptimizer.Models
{
    public class PerformanceMetrics
    {
        public int Id { get; set; }
        public ParallelizationStrategy Strategy { get; set; }
        public int ThreadCount { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public double Speedup { get; set; }
        public double Efficiency { get; set; }
        public int ScenariosProcessed { get; set; }
        public double ScenariosPerSecond { get; set; }
        public double MemoryUsageMB { get; set; }
        public double CpuUsagePercent { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
