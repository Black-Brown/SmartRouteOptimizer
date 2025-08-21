using SmartRouteOptimizer.Models;
using SmartRouteOptimizer.Models;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Diagnostics;


namespace SmartRouteOptimizer.Services
{
    public interface IOptimizationService
    {
        Task<OptimizationResult> OptimizeRoutesAsync(List<DeliveryPoint> deliveries,
            List<Vehicle> vehicles, ParallelizationStrategy strategy, int maxThreads);
        Task<List<PerformanceMetrics>> RunPerformanceBenchmarkAsync(
            List<DeliveryPoint> deliveries, List<Vehicle> vehicles);
    }
}



namespace SmartRouteOptimizer.Services
{
    public class OptimizationService : IOptimizationService
    {
        private readonly IDistanceService _distanceService;
        private readonly IHeuristicService _heuristicService;
        private SharedData _sharedData;

        public OptimizationService(IDistanceService distanceService, IHeuristicService heuristicService)
        {
            _distanceService = distanceService;
            _heuristicService = heuristicService;
            _sharedData = new SharedData();
        }

        public async Task<OptimizationResult> OptimizeRoutesAsync(
            List<DeliveryPoint> deliveries,
            List<Vehicle> vehicles,
            ParallelizationStrategy strategy,
            int maxThreads)
        {
            var stopwatch = Stopwatch.StartNew();
            await InitializeSharedDataAsync(deliveries, vehicles);

            OptimizationResult result = strategy switch
            {
                ParallelizationStrategy.Sequential => await OptimizeSequentialAsync(deliveries, vehicles),
                ParallelizationStrategy.ByHeuristic => await OptimizeByHeuristicAsync(deliveries, vehicles, maxThreads),
                ParallelizationStrategy.ByGeographicZone => await OptimizeByZoneAsync(deliveries, vehicles, maxThreads),
                ParallelizationStrategy.ByCandidateRoutes => await OptimizeByCandidateRoutesAsync(deliveries, vehicles, maxThreads),
                ParallelizationStrategy.HybridApproach => await OptimizeHybridAsync(deliveries, vehicles, maxThreads),
                _ => throw new ArgumentException("Estrategia no válida")
            };

            stopwatch.Stop();
            result.Strategy = strategy.ToString();
            result.ExecutionTime = stopwatch.Elapsed;
            result.ThreadsUsed = strategy == ParallelizationStrategy.Sequential ? 1 : maxThreads;
            result.CreatedAt = DateTime.Now;

            return result;
        }

        private async Task InitializeSharedDataAsync(List<DeliveryPoint> deliveries, List<Vehicle> vehicles)
        {
            // Inicializar matriz de distancias compartida
            await Task.Run(() =>
            {
                Parallel.ForEach(deliveries, delivery1 =>
                {
                    foreach (var delivery2 in deliveries.Where(d => d.Id != delivery1.Id))
                    {
                        var key = $"{delivery1.Id}-{delivery2.Id}";
                        var distance = _distanceService.CalculateDistance(
                            delivery1.Latitude, delivery1.Longitude,
                            delivery2.Latitude, delivery2.Longitude);
                        _sharedData.DistanceMatrix.TryAdd(key, distance);
                    }
                });
            });

            // Inicializar datos compartidos
            foreach (var delivery in deliveries)
                _sharedData.GlobalDeliveries.TryAdd(delivery.Id, delivery);

            foreach (var vehicle in vehicles)
                _sharedData.AvailableVehicles.TryAdd(vehicle.Id, vehicle);
        }

        // 1. Optimización Secuencial (Base de comparación)
        private async Task<OptimizationResult> OptimizeSequentialAsync(List<DeliveryPoint> deliveries, List<Vehicle> vehicles)
        {
            var result = new OptimizationResult();

            foreach (var vehicle in vehicles)
            {
                var route = await _heuristicService.NearestNeighborAsync(vehicle, deliveries, _sharedData);
                result.OptimizedRoutes.Add(route);

                // Actualizar métricas
                result.TotalDistance += route.TotalDistance;
                result.TotalFuelCost += route.FuelCost;
                result.TotalDeliveries += route.DeliverySequence.Count;
                result.OnTimeDeliveries += route.DeliveriesOnTime;
            }

            return result;
        }

        // 2. Paralelización por Heurística
        private async Task<OptimizationResult> OptimizeByHeuristicAsync(List<DeliveryPoint> deliveries, List<Vehicle> vehicles, int maxThreads)
        {
            var tasks = new List<Task<List<Route>>>();
            var semaphore = new SemaphoreSlim(maxThreads);

            // Ejecutar diferentes heurísticas en paralelo
            tasks.Add(RunHeuristicAsync("NearestNeighbor", deliveries, vehicles, semaphore));
            tasks.Add(RunHeuristicAsync("GreedyTime", deliveries, vehicles, semaphore));
            tasks.Add(RunHeuristicAsync("PriorityBased", deliveries, vehicles, semaphore));
            tasks.Add(RunHeuristicAsync("GeneticAlgorithm", deliveries, vehicles, semaphore));

            var results = await Task.WhenAll(tasks);

            // Seleccionar la mejor solución
            var bestResult = SelectBestSolution(results.SelectMany(r => r).ToList());
            return bestResult;
        }

        private async Task<List<Route>> RunHeuristicAsync(string heuristicName, List<DeliveryPoint> deliveries,
            List<Vehicle> vehicles, SemaphoreSlim semaphore)
        {
            await semaphore.WaitAsync();
            try
            {
                var routes = new List<Route>();

                await Task.Run(async () =>
                {
                    foreach (var vehicle in vehicles)
                    {
                        Route route = heuristicName switch
                        {
                            "NearestNeighbor" => await _heuristicService.NearestNeighborAsync(vehicle, deliveries, _sharedData),
                            "GreedyTime" => await _heuristicService.GreedyTimeBasedAsync(vehicle, deliveries, _sharedData),
                            "PriorityBased" => await _heuristicService.PriorityBasedAsync(vehicle, deliveries, _sharedData),
                            "GeneticAlgorithm" => await _heuristicService.GeneticAlgorithmAsync(vehicle, deliveries, _sharedData),
                            _ => await _heuristicService.NearestNeighborAsync(vehicle, deliveries, _sharedData)
                        };

                        route.OptimizationStrategy = heuristicName;
                        routes.Add(route);
                    }
                });

                return routes;
            }
            finally
            {
                semaphore.Release();
            }
        }

        // 3. Paralelización por Zona Geográfica
        private async Task<OptimizationResult> OptimizeByZoneAsync(List<DeliveryPoint> deliveries, List<Vehicle> vehicles, int maxThreads)
        {
            var zones = DivideIntoGeographicZones(deliveries, maxThreads);
            var tasks = new List<Task<OptimizationResult>>();

            foreach (var zone in zones)
            {
                var zoneVehicles = AssignVehiclesToZone(zone, vehicles);
                tasks.Add(OptimizeZoneAsync(zone, zoneVehicles));
            }

            var zoneResults = await Task.WhenAll(tasks);
            return MergeZoneResults(zoneResults);
        }

        private async Task<OptimizationResult> OptimizeZoneAsync(List<DeliveryPoint> zoneDeliveries, List<Vehicle> zoneVehicles)
        {
            return await Task.Run(async () =>
            {
                var result = new OptimizationResult();

                foreach (var vehicle in zoneVehicles)
                {
                    var route = await _heuristicService.NearestNeighborAsync(vehicle, zoneDeliveries, _sharedData);
                    result.OptimizedRoutes.Add(route);

                    lock (_sharedData.SyncLock)
                    {
                        result.TotalDistance += route.TotalDistance;
                        result.TotalFuelCost += route.FuelCost;
                        result.TotalDeliveries += route.DeliverySequence.Count;
                        result.OnTimeDeliveries += route.DeliveriesOnTime;
                    }
                }

                return result;
            });
        }

        // 4. Paralelización por Rutas Candidatas
        private async Task<OptimizationResult> OptimizeByCandidateRoutesAsync(List<DeliveryPoint> deliveries, List<Vehicle> vehicles, int maxThreads)
        {
            var candidateTasks = new List<Task<Route>>();
            var semaphore = new SemaphoreSlim(maxThreads);

            // Generar múltiples rutas candidatas para cada vehículo
            foreach (var vehicle in vehicles)
            {
                for (int i = 0; i < maxThreads; i++)
                {
                    candidateTasks.Add(GenerateCandidateRouteAsync(vehicle, deliveries, semaphore, i));
                }
            }

            var candidateRoutes = await Task.WhenAll(candidateTasks);

            // Seleccionar las mejores rutas sin solapamiento
            return SelectOptimalCombination(candidateRoutes.ToList(), vehicles.Count);
        }

        private async Task<Route> GenerateCandidateRouteAsync(Vehicle vehicle, List<DeliveryPoint> deliveries,
            SemaphoreSlim semaphore, int seed)
        {
            await semaphore.WaitAsync();
            try
            {
                return await Task.Run(async () =>
                {
                    // Usar diferentes semillas aleatorias para diversidad
                    var random = new Random(seed + vehicle.Id);
                    var shuffledDeliveries = deliveries.OrderBy(x => random.Next()).ToList();

                    return await _heuristicService.RandomizedGreedyAsync(vehicle, shuffledDeliveries, _sharedData, random);
                });
            }
            finally
            {
                semaphore.Release();
            }
        }

        // 5. Enfoque Híbrido
        private async Task<OptimizationResult> OptimizeHybridAsync(List<DeliveryPoint> deliveries, List<Vehicle> vehicles, int maxThreads)
        {
            // Combinar múltiples estrategias
            var tasks = new List<Task<OptimizationResult>>
            {
                OptimizeByHeuristicAsync(deliveries, vehicles, maxThreads / 2),
                OptimizeByZoneAsync(deliveries, vehicles, maxThreads / 2)
            };

            var results = await Task.WhenAll(tasks);

            // Seleccionar el mejor resultado híbrido
            return results.OrderBy(r => r.TotalDistance).First();
        }

        // Métodos auxiliares
        private List<List<DeliveryPoint>> DivideIntoGeographicZones(List<DeliveryPoint> deliveries, int zoneCount)
        {
            var zones = new List<List<DeliveryPoint>>();
            var sortedByLatitude = deliveries.OrderBy(d => d.Latitude).ToList();

            int itemsPerZone = deliveries.Count / zoneCount;

            for (int i = 0; i < zoneCount; i++)
            {
                var zoneDeliveries = sortedByLatitude
                    .Skip(i * itemsPerZone)
                    .Take(i == zoneCount - 1 ? int.MaxValue : itemsPerZone)
                    .ToList();
                zones.Add(zoneDeliveries);
            }

            return zones;
        }

        private List<Vehicle> AssignVehiclesToZone(List<DeliveryPoint> zoneDeliveries, List<Vehicle> allVehicles)
        {
            if (!zoneDeliveries.Any()) return new List<Vehicle>();

            var zoneCenterLat = zoneDeliveries.Average(d => d.Latitude);
            var zoneCenterLon = zoneDeliveries.Average(d => d.Longitude);

            return allVehicles
                .OrderBy(v => _distanceService.CalculateDistance(v.Latitude, v.Longitude, zoneCenterLat, zoneCenterLon))
                .Take(Math.Max(1, allVehicles.Count / 4))
                .ToList();
        }

        private OptimizationResult SelectBestSolution(List<Route> allRoutes)
        {
            var result = new OptimizationResult
            {
                OptimizedRoutes = allRoutes.GroupBy(r => r.VehicleId)
                    .Select(g => g.OrderBy(r => r.TotalDistance).First())
                    .ToList()
            };

            result.TotalDistance = result.OptimizedRoutes.Sum(r => r.TotalDistance);
            result.TotalFuelCost = result.OptimizedRoutes.Sum(r => r.FuelCost);
            result.TotalDeliveries = result.OptimizedRoutes.Sum(r => r.DeliverySequence.Count);
            result.OnTimeDeliveries = result.OptimizedRoutes.Sum(r => r.DeliveriesOnTime);

            return result;
        }

        private OptimizationResult MergeZoneResults(OptimizationResult[] zoneResults)
        {
            var mergedResult = new OptimizationResult();

            foreach (var zoneResult in zoneResults)
            {
                mergedResult.OptimizedRoutes.AddRange(zoneResult.OptimizedRoutes);
                mergedResult.TotalDistance += zoneResult.TotalDistance;
                mergedResult.TotalFuelCost += zoneResult.TotalFuelCost;
                mergedResult.TotalDeliveries += zoneResult.TotalDeliveries;
                mergedResult.OnTimeDeliveries += zoneResult.OnTimeDeliveries;
            }

            return mergedResult;
        }

        private OptimizationResult SelectOptimalCombination(List<Route> candidateRoutes, int vehicleCount)
        {
            // Algoritmo greedy para seleccionar la mejor combinación sin solapamientos
            var selectedRoutes = new List<Route>();
            var usedDeliveries = new HashSet<int>();

            var sortedCandidates = candidateRoutes.OrderBy(r => r.TotalDistance / Math.Max(1, r.DeliverySequence.Count)).ToList();

            foreach (var candidate in sortedCandidates)
            {
                if (selectedRoutes.Count >= vehicleCount) break;

                if (!candidate.DeliverySequence.Any(d => usedDeliveries.Contains(d.Id)))
                {
                    selectedRoutes.Add(candidate);
                    foreach (var delivery in candidate.DeliverySequence)
                        usedDeliveries.Add(delivery.Id);
                }
            }

            var result = new OptimizationResult { OptimizedRoutes = selectedRoutes };
            result.TotalDistance = selectedRoutes.Sum(r => r.TotalDistance);
            result.TotalFuelCost = selectedRoutes.Sum(r => r.FuelCost);
            result.TotalDeliveries = selectedRoutes.Sum(r => r.DeliverySequence.Count);
            result.OnTimeDeliveries = selectedRoutes.Sum(r => r.DeliveriesOnTime);

            return result;
        }

        public async Task<List<PerformanceMetrics>> RunPerformanceBenchmarkAsync(
            List<DeliveryPoint> deliveries, List<Vehicle> vehicles)
        {
            var metrics = new List<PerformanceMetrics>();
            var strategies = Enum.GetValues<ParallelizationStrategy>();

            foreach (var strategy in strategies)
            {
                for (int threads = 1; threads <= Environment.ProcessorCount; threads *= 2)
                {
                    var stopwatch = Stopwatch.StartNew();
                    var memoryBefore = GC.GetTotalMemory(false);

                    await OptimizeRoutesAsync(deliveries, vehicles, strategy, threads);

                    stopwatch.Stop();
                    var memoryAfter = GC.GetTotalMemory(false);

                    var baseTime = strategy == ParallelizationStrategy.Sequential ? stopwatch.Elapsed : TimeSpan.FromMilliseconds(1000); // Simulado
                    var speedup = strategy == ParallelizationStrategy.Sequential ? 1.0 : baseTime.TotalMilliseconds / stopwatch.Elapsed.TotalMilliseconds;

                    metrics.Add(new PerformanceMetrics
                    {
                        Strategy = strategy,
                        ThreadCount = threads,
                        ExecutionTime = stopwatch.Elapsed,
                        Speedup = speedup,
                        Efficiency = speedup / threads,
                        ScenariosProcessed = deliveries.Count * vehicles.Count,
                        ScenariosPerSecond = (deliveries.Count * vehicles.Count) / stopwatch.Elapsed.TotalSeconds,
                        MemoryUsageMB = (memoryAfter - memoryBefore) / 1024.0 / 1024.0,
                        CpuUsagePercent = Math.Min(100.0, threads * 25.0), // Simulado
                        Timestamp = DateTime.Now
                    });
                }
            }

            return metrics;
        }
    }
}