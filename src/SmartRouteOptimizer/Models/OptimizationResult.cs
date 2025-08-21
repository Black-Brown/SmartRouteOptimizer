namespace SmartRouteOptimizer.Models
{
    public class OptimizationResult
    {
        public int Id { get; set; }
        public List<Route> OptimizedRoutes { get; set; } = new List<Route>();
        public double TotalDistance { get; set; }
        public double TotalFuelCost { get; set; }
        public TimeSpan TotalTime { get; set; }
        public int TotalDeliveries { get; set; }
        public int OnTimeDeliveries { get; set; }
        public double SuccessRate => TotalDeliveries > 0 ? (double)OnTimeDeliveries / TotalDeliveries * 100 : 0;
        public string Strategy { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public int ThreadsUsed { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}