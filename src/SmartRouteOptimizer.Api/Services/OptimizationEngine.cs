using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SmartRouteOptimizer.Api.Models;
using SmartRouteOptimizer.Api.Models;
using SmartRouteOptimizer.Api.Services;

namespace SmartRouteOptimizer.Api.Services;

public class OptimizationEngine
{
    private readonly ConcurrentDictionary<Guid, OptimizationSession> _sessions = new();
    private static readonly double[] SANTO_DOMINGO_CENTER = new[] { 18.4861, -69.9312 };

    public Guid StartOptimization(OptimizationRequest request)
    {
        var session = new OptimizationSession(request);
        _sessions[session.Id] = session;

        // Lanzar en background
        _ = Task.Run(() => RunAsync(session));
        return session.Id;
    }

    public ProgressDto? GetProgress(Guid sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var s)) return null;
        return new ProgressDto
        {
            SessionId = s.Id,
            Evaluaciones = s.Evaluaciones,
            ElapsedSeconds = s.ElapsedSeconds,
            ProgressPercent = s.ProgressPercent,
            Algorithms = s.Algorithms.ToList(),
            Message = s.Message,
        };
    }

    public SolutionDto? GetSolution(Guid sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var s)) return null;
        return s.Solution;
    }

    private async Task RunAsync(OptimizationSession s)
    {
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(5, s.Request.TimeLimitSeconds)));
        var token = cts.Token;

        s.Message = "Ejecutando algoritmos paralelos...";
        var rng = new Random();

        // ✅ INICIALIZAR PROGRESO INMEDIATAMENTE
        s.Evaluaciones = 1;
        s.ProgressPercent = 0.1;

        // ✅ PROGRESO EN PARALELO - CORREGIDO
        var progressTask = Task.Run(async () =>
        {
            try
            {
                while (!token.IsCancellationRequested && s.ProgressPercent < 95)
                {
                    s.Evaluaciones += rng.Next(50, 200);
                    s.ProgressPercent = Math.Min(95.0, s.ProgressPercent + rng.NextDouble() * 2.0);
                    s.Message = $"Explorando {Math.Floor(s.ProgressPercent)}% del espacio de soluciones...";

                    // ✅ DELAY MÁS CORTO PARA MEJOR RESPONSIVIDAD
                    await Task.Delay(100, token);
                }
            }
            catch (OperationCanceledException)
            {
                // Normal cuando se cancela
            }
        }, token);

        // ✅ CREAR TASKS EN PARALELO PERO SIN MARCAR COMO RUNNING AQUÍ
        var tasks = new List<Task<AlgorithmResultDto>>
        {
            Task.Run(() => RunGreedy("Greedy-1", 2.0, s, rng, token), token),
            Task.Run(() => RunGreedy("Greedy-2", 2.4, s, rng, token), token),
            Task.Run(() => RunGenetic("Genético-1", 7.0, s, rng, token), token),
            Task.Run(() => RunGenetic("Genético-2", 6.2, s, rng, token), token)
        };

        AlgorithmResultDto[] results;
        try
        {
            // ✅ ESPERAR QUE TODOS TERMINEN
            results = await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
            // Recoger los que terminaron
            results = tasks
                .Where(t => t.IsCompletedSuccessfully)
                .Select(t => t.Result)
                .ToArray();
        }
        finally
        {
            cts.Cancel();
            try { await progressTask; } catch { /* ignore */ }
        }

        s.Results = results.ToList();

        // Construir solución final
        var best = s.Results.OrderBy(r => r.Cost).FirstOrDefault();
        var routes = BuildRoutes(s.Request.Clients, s.Request.Vehicles);

        s.Solution = new SolutionDto
        {
            SessionId = s.Id,
            Results = s.Results,
            Best = best,
            BestEfficiencyPercent = 75 + rng.NextDouble() * 20,
            CostPerDelivery = best is null || s.Request.Clients.Count == 0 ?
                0 : best.Cost / s.Request.Clients.Count,
            Routes = routes
        };

        // ✅ FINALIZAR CORRECTAMENTE
        s.ProgressPercent = 100;
        s.Message = "Optimización finalizada";
        s.Evaluaciones += rng.Next(100, 300); // Bump final
    }

    // ===== ALGORITMOS CORREGIDOS =====
    private AlgorithmResultDto RunGreedy(string name, double seconds,
        OptimizationSession s, Random rng, CancellationToken token)
    {
        // ✅ MARCAR COMO RUNNING AL INICIAR
        UpdateAlg(s, name, state: "Running");

        // ✅ TRABAJO SIMULADO CON MEJOR CONTROL
        SimulateWork(seconds, token, s, name);

        var result = new AlgorithmResultDto
        {
            Algorithm = name,
            Cost = 80 + rng.NextDouble() * 40, // 80–120
            DistanceKm = 130 + rng.NextDouble() * 30, // 130–160
            TimeSeconds = seconds
        };

        // ✅ ACTUALIZAR AL COMPLETAR
        UpdateAlg(s, name, state: "Completed", cost: result.Cost);

        return result;
    }

    private AlgorithmResultDto RunGenetic(string name, double seconds,
        OptimizationSession s, Random rng, CancellationToken token)
    {
        // ✅ MARCAR COMO RUNNING AL INICIAR  
        UpdateAlg(s, name, state: "Running");

        // ✅ TRABAJO SIMULADO
        SimulateWork(seconds, token, s, name);

        var result = new AlgorithmResultDto
        {
            Algorithm = name,
            Cost = 85 + rng.NextDouble() * 35, // 85–120
            DistanceKm = 135 + rng.NextDouble() * 25, // 135–160
            TimeSeconds = seconds
        };

        // ✅ ACTUALIZAR AL COMPLETAR
        UpdateAlg(s, name, state: "Completed", cost: result.Cost);

        return result;
    }

    // ✅ SIMULACIÓN MEJORADA CON MEJOR RESPONSIVIDAD
    private void SimulateWork(double seconds, CancellationToken token, OptimizationSession s, string algorithmName)
    {
        var stopAt = DateTime.UtcNow.AddSeconds(seconds);
        var rng = new Random();

        while (DateTime.UtcNow < stopAt)
        {
            token.ThrowIfCancellationRequested();

            // ✅ MENOS CPU-INTENSIVO PERO VISIBLE
            Thread.Sleep(50); // 50ms pause entre iteraciones

            // Contribuir a las evaluaciones globales ocasionalmente
            if (rng.NextDouble() < 0.1) // 10% chance cada iteración
            {
                s.Evaluaciones += rng.Next(10, 50);
            }
        }
    }

    // ✅ CORRECCIÓN DEL UpdateAlg - THREAD-SAFE
    private void UpdateAlg(OptimizationSession s, string name, string state, double? cost = null)
    {
        lock (s.Algorithms) // Thread-safe para acceso concurrente
        {
            var idx = s.Algorithms.FindIndex(a => a.Name == name);
            if (idx >= 0)
            {
                var prev = s.Algorithms[idx];
                s.Algorithms[idx] = prev with
                {
                    State = state,
                    Cost = cost,
                    LastUpdate = DateTimeOffset.UtcNow
                };
            }
        }
    }

    // --- Construcción de rutas para visualización en el mapa ---
    private List<RouteDto> BuildRoutes(List<ClientDto> clients, List<VehicleDto> vehicles)
    {
        var routes = new List<RouteDto>();
        if (vehicles.Count == 0) return routes;

        var clientsPerVehicle = (int)Math.Ceiling((double)clients.Count / vehicles.Count);

        for (int i = 0; i < vehicles.Count; i++)
        {
            var v = vehicles[i];
            var chunk = clients.Skip(i * clientsPerVehicle).Take(clientsPerVehicle).ToList();
            if (chunk.Count == 0) continue;

            var coords = new List<double[]> { new[] { SANTO_DOMINGO_CENTER[0], SANTO_DOMINGO_CENTER[1] } };
            coords.AddRange(chunk.Select(c => new[] { c.Lat, c.Lng }));
            coords.Add(new[] { SANTO_DOMINGO_CENTER[0], SANTO_DOMINGO_CENTER[1] });

            // Distancia estimada simple
            var dist = 20 + chunk.Count * 2 + (i * 1.5);

            routes.Add(new RouteDto
            {
                VehicleId = v.Id,
                VehicleColor = v.Color,
                Coordinates = coords,
                Deliveries = chunk.Count,
                EstimatedDistanceKm = Math.Round(dist, 1)
            });
        }

        return routes;
    }
}