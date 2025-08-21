using SmartRouteOptimizer.Api.Models;
using System.Diagnostics;

namespace SmartRouteOptimizer.Api.Services
{
    public class OptimizationSession
    {
        public Guid Id { get; } = Guid.NewGuid();
        public OptimizationRequest Request { get; }
        public DateTimeOffset StartTime { get; } = DateTimeOffset.UtcNow;

        public int Evaluaciones { get; set; }
        public double ProgressPercent { get; set; }
        public string Message { get; set; } = "Iniciando...";

        public List<AlgorithmStatusDto> Algorithms { get; } = new()
    {
        new("Greedy-1", "Waiting", null, DateTimeOffset.UtcNow),
        new("Greedy-2", "Waiting", null, DateTimeOffset.UtcNow),
        new("Genético-1", "Waiting", null, DateTimeOffset.UtcNow),
        new("Genético-2", "Waiting", null, DateTimeOffset.UtcNow)
    };

        public List<AlgorithmResultDto>? Results { get; set; }
        public SolutionDto? Solution { get; set; }

        private readonly Stopwatch _sw = Stopwatch.StartNew();
        public double ElapsedSeconds => _sw.Elapsed.TotalSeconds;

        public OptimizationSession(OptimizationRequest request)
        {
            Request = request;
        }
    }
}
