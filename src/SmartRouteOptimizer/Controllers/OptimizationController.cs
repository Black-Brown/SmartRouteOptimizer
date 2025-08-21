using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using SmartRouteOptimizer.Models;
using SmartRouteOptimizer.Services;
using SmartRouteOptimizer.ViewModels;

namespace SmartRouteOptimizer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OptimizationController : ControllerBase
    {
        private readonly IOptimizationService _optimizationService;
        private readonly ILogger<OptimizationController> _logger;
        private static readonly Dictionary<string, OptimizationViewModel> _activeSessions = new();

        public OptimizationController(IOptimizationService optimizationService, ILogger<OptimizationController> logger)
        {
            _optimizationService = optimizationService;
            _logger = logger;
        }

        [HttpPost("start")]
        public async Task<IActionResult> StartOptimization([FromBody] OptimizationRequest request)
        {
            try
            {
                var sessionId = Guid.NewGuid().ToString();

                // Crear sesión de optimización
                var session = new OptimizationViewModel
                {
                    DeliveryPoints = request.DeliveryPoints,
                    Vehicles = request.Vehicles,
                    SelectedStrategy = request.Strategy,
                    MaxThreads = request.MaxThreads,
                    IsRunning = true,
                    Status = "Iniciando optimización..."
                };

                _activeSessions[sessionId] = session;

                // Ejecutar optimización en background
                _ = Task.Run(async () =>
                {
                    try
                    {
                        session.Status = $"Ejecutando estrategia: {request.Strategy}";

                        var result = await _optimizationService.OptimizeRoutesAsync(
                            request.DeliveryPoints,
                            request.Vehicles,
                            request.Strategy,
                            request.MaxThreads);

                        session.Results.Add(result);
                        session.IsRunning = false;
                        session.Status = $"Completado - {result.TotalDeliveries} entregas optimizadas";

                        _logger.LogInformation($"Optimización completada: {request.Strategy}, Tiempo: {result.ExecutionTime}");
                    }
                    catch (Exception ex)
                    {
                        session.IsRunning = false;
                        session.Status = $"Error: {ex.Message}";
                        _logger.LogError(ex, "Error en optimización");
                    }
                });

                return Ok(new { SessionId = sessionId, Message = "Optimización iniciada" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al iniciar optimización");
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("status/{sessionId}")]
        public IActionResult GetOptimizationStatus(string sessionId)
        {
            if (!_activeSessions.TryGetValue(sessionId, out var session))
            {
                return NotFound(new { Error = "Sesión no encontrada" });
            }

            return Ok(new
            {
                IsRunning = session.IsRunning,
                Status = session.Status,
                ResultsCount = session.Results.Count,
                Results = session.Results.Select(r => new
                {
                    r.Strategy,
                    r.ExecutionTime,
                    r.ThreadsUsed,
                    r.TotalDistance,
                    r.TotalDeliveries,
                    r.SuccessRate,
                    r.TotalFuelCost
                })
            });
        }

        [HttpPost("compare")]
        public async Task<IActionResult> CompareStrategies([FromBody] ComparisonRequest request)
        {
            try
            {
                var results = new List<OptimizationResult>();
                var strategies = request.Strategies ?? Enum.GetValues<ParallelizationStrategy>();

                foreach (var strategy in strategies)
                {
                    var result = await _optimizationService.OptimizeRoutesAsync(
                        request.DeliveryPoints,
                        request.Vehicles,
                        strategy,
                        request.MaxThreads);

                    results.Add(result);
                }

                var comparison = results.Select(r => new
                {
                    Strategy = r.Strategy,
                    ExecutionTime = r.ExecutionTime.TotalMilliseconds,
                    TotalDistance = r.TotalDistance,
                    SuccessRate = r.SuccessRate,
                    FuelCost = r.TotalFuelCost,
                    ThreadsUsed = r.ThreadsUsed,
                    Efficiency = r.ThreadsUsed > 1 ?
                        (results.First(baseline => baseline.Strategy == "Sequential")?.ExecutionTime.TotalMilliseconds ?? r.ExecutionTime.TotalMilliseconds) / r.ExecutionTime.TotalMilliseconds / r.ThreadsUsed
                        : 1.0
                }).ToList();

                return Ok(comparison);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en comparación de estrategias");
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("benchmark")]
        public async Task<IActionResult> RunBenchmark([FromBody] BenchmarkRequest request)
        {
            try
            {
                var metrics = await _optimizationService.RunPerformanceBenchmarkAsync(
                    request.DeliveryPoints,
                    request.Vehicles);

                var benchmarkResult = new
                {
                    Timestamp = DateTime.Now,
                    SystemInfo = new
                    {
                        ProcessorCount = Environment.ProcessorCount,
                        MachineName = Environment.MachineName,
                        WorkingSet = Environment.WorkingSet
                    },
                    Metrics = metrics.Select(m => new
                    {
                        Strategy = m.Strategy.ToString(),
                        ThreadCount = m.ThreadCount,
                        ExecutionTimeMs = m.ExecutionTime.TotalMilliseconds,
                        Speedup = m.Speedup,
                        Efficiency = m.Efficiency,
                        ScenariosPerSecond = m.ScenariosPerSecond,
                        MemoryUsageMB = m.MemoryUsageMB,
                        CpuUsagePercent = m.CpuUsagePercent
                    })
                };

                return Ok(benchmarkResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en benchmark");
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("live-metrics")]
        public IActionResult GetLiveMetrics()
        {
            var liveData = new
            {
                ActiveSessions = _activeSessions.Count(s => s.Value.IsRunning),
                TotalSessions = _activeSessions.Count,
                AverageExecutionTime = _activeSessions
                    .SelectMany(s => s.Value.Results)
                    .Where(r => r.ExecutionTime.TotalSeconds > 0)
                    .Average(r => r.ExecutionTime.TotalMilliseconds),
                BestPerformingStrategy = _activeSessions
                    .SelectMany(s => s.Value.Results)
                    .GroupBy(r => r.Strategy)
                    .OrderBy(g => g.Average(r => r.ExecutionTime.TotalMilliseconds))
                    .FirstOrDefault()?.Key ?? "N/A",
                SystemLoad = new
                {
                    CpuUsage = GetCpuUsage(),
                    MemoryUsage = GC.GetTotalMemory(false) / 1024.0 / 1024.0,
                    ThreadCount = System.Threading.ThreadPool.ThreadCount
                }
            };

            return Ok(liveData);
        }

        private double GetCpuUsage()
        {
            // Simulación de uso de CPU - en producción usar PerformanceCounter
            return Math.Min(100, _activeSessions.Count(s => s.Value.IsRunning) * 25.0);
        }
    }

    // DTOs para las requests
    public class OptimizationRequest
    {
        public List<DeliveryPoint> DeliveryPoints { get; set; } = new();
        public List<Vehicle> Vehicles { get; set; } = new();
        public ParallelizationStrategy Strategy { get; set; }
        public int MaxThreads { get; set; }
    }

    public class ComparisonRequest
    {
        public List<DeliveryPoint> DeliveryPoints { get; set; } = new();
        public List<Vehicle> Vehicles { get; set; } = new();
        public ParallelizationStrategy[]? Strategies { get; set; }
        public int MaxThreads { get; set; } = Environment.ProcessorCount;
    }

    public class BenchmarkRequest
    {
        public List<DeliveryPoint> DeliveryPoints { get; set; } = new();
        public List<Vehicle> Vehicles { get; set; } = new();
    }
}
