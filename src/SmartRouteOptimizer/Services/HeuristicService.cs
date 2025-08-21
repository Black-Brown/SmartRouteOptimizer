using SmartRouteOptimizer.Models;
using SmartRouteOptimizer.Services;

namespace SmartRouteOptimizer.Services
{
    public class HeuristicService : IHeuristicService
    {
        private readonly IDistanceService _distanceService;

        public HeuristicService(IDistanceService distanceService)
        {
            _distanceService = distanceService;
        }

        // Heurística del Vecino Más Cercano
        public async Task<SmartRouteOptimizer.Models.Route> NearestNeighborAsync(Vehicle vehicle, List<DeliveryPoint> deliveries, SharedData sharedData)
        {
            return await Task.Run(() =>
            {
                var route = new Route { VehicleId = vehicle.Id, OptimizationStrategy = "NearestNeighbor" };
                var unvisited = new List<DeliveryPoint>(deliveries);
                var currentLat = vehicle.Latitude;
                var currentLon = vehicle.Longitude;
                var totalDistance = 0.0;

                while (unvisited.Any() && route.DeliverySequence.Count < vehicle.MaxCapacity)
                {
                    // Sincronizar acceso a datos compartidos
                    lock (sharedData.SyncLock)
                    {
                        var nearest = unvisited
                            .Where(d => sharedData.GlobalDeliveries.ContainsKey(d.Id) && !sharedData.GlobalDeliveries[d.Id].IsDelivered)
                            .OrderBy(d => GetCachedDistance(currentLat, currentLon, d.Latitude, d.Longitude, sharedData))
                            .FirstOrDefault();

                        if (nearest != null)
                        {
                            var distance = GetCachedDistance(currentLat, currentLon, nearest.Latitude, nearest.Longitude, sharedData);

                            route.DeliverySequence.Add(nearest);
                            totalDistance += distance;

                            // Marcar como entregado en datos compartidos
                            if (sharedData.GlobalDeliveries.TryGetValue(nearest.Id, out var delivery))
                            {
                                delivery.IsDelivered = true;
                                delivery.DeliveredAt = DateTime.Now.Add(route.EstimatedTime);
                            }

                            unvisited.Remove(nearest);
                            currentLat = nearest.Latitude;
                            currentLon = nearest.Longitude;
                        }
                    }
                }

                route.TotalDistance = totalDistance;
                route.EstimatedTime = _distanceService.EstimateTime(totalDistance);
                route.FuelCost = _distanceService.EstimateFuelCost(totalDistance);
                route.DeliveriesOnTime = CalculateOnTimeDeliveries(route);

                return route;
            });
        }

        // Heurística Basada en Tiempo (Greedy)
        public async Task<SmartRouteOptimizer.Models.Route> GreedyTimeBasedAsync(Vehicle vehicle, List<DeliveryPoint> deliveries, SharedData sharedData)
        {
            return await Task.Run(() =>
            {
                var route = new Route { VehicleId = vehicle.Id, OptimizationStrategy = "GreedyTime" };
                var availableDeliveries = deliveries.Where(d => !d.IsDelivered).OrderBy(d => d.DeliveryWindow).ToList();
                var currentTime = DateTime.Now;
                var currentLat = vehicle.Latitude;
                var currentLon = vehicle.Longitude;
                var totalDistance = 0.0;

                foreach (var delivery in availableDeliveries)
                {
                    if (route.DeliverySequence.Count >= vehicle.MaxCapacity) break;

                    lock (sharedData.SyncLock)
                    {
                        if (!sharedData.GlobalDeliveries[delivery.Id].IsDelivered)
                        {
                            var distance = GetCachedDistance(currentLat, currentLon, delivery.Latitude, delivery.Longitude, sharedData);
                            var travelTime = _distanceService.EstimateTime(distance);
                            var arrivalTime = currentTime.Add(travelTime);

                            // Verificar si puede llegar a tiempo
                            if (arrivalTime.TimeOfDay <= delivery.DeliveryWindow.Add(TimeSpan.FromHours(1))) // 1 hora de tolerancia
                            {
                                route.DeliverySequence.Add(delivery);
                                totalDistance += distance;
                                currentTime = arrivalTime.Add(TimeSpan.FromMinutes(5)); // 5 min por entrega
                                currentLat = delivery.Latitude;
                                currentLon = delivery.Longitude;

                                sharedData.GlobalDeliveries[delivery.Id].IsDelivered = true;
                                sharedData.GlobalDeliveries[delivery.Id].DeliveredAt = arrivalTime;
                            }
                        }
                    }
                }

                route.TotalDistance = totalDistance;
                route.EstimatedTime = TimeSpan.FromTicks(currentTime.Subtract(DateTime.Now).Ticks);
                route.FuelCost = _distanceService.EstimateFuelCost(totalDistance);
                route.DeliveriesOnTime = CalculateOnTimeDeliveries(route);

                return route;
            });
        }

        // Heurística Basada en Prioridad
        public async Task<Route> PriorityBasedAsync(Vehicle vehicle, List<DeliveryPoint> deliveries, SharedData sharedData)
        {
            return await Task.Run(() =>
            {
                var route = new Route { VehicleId = vehicle.Id, OptimizationStrategy = "PriorityBased" };
                var prioritizedDeliveries = deliveries
                    .Where(d => !d.IsDelivered)
                    .OrderBy(d => d.Priority)
                    .ThenBy(d => d.DeliveryWindow)
                    .ToList();

                var currentLat = vehicle.Latitude;
                var currentLon = vehicle.Longitude;
                var totalDistance = 0.0;

                foreach (var delivery in prioritizedDeliveries)
                {
                    if (route.DeliverySequence.Count >= vehicle.MaxCapacity) break;

                    lock (sharedData.SyncLock)
                    {
                        if (!sharedData.GlobalDeliveries[delivery.Id].IsDelivered)
                        {
                            var distance = GetCachedDistance(currentLat, currentLon, delivery.Latitude, delivery.Longitude, sharedData);

                            route.DeliverySequence.Add(delivery);
                            totalDistance += distance;
                            currentLat = delivery.Latitude;
                            currentLon = delivery.Longitude;

                            sharedData.GlobalDeliveries[delivery.Id].IsDelivered = true;
                        }
                    }
                }

                route.TotalDistance = totalDistance;
                route.EstimatedTime = _distanceService.EstimateTime(totalDistance);
                route.FuelCost = _distanceService.EstimateFuelCost(totalDistance);
                route.DeliveriesOnTime = CalculateOnTimeDeliveries(route);

                return route;
            });
        }

        // Algoritmo Genético Simplificado
        public async Task<Route> GeneticAlgorithmAsync(Vehicle vehicle, List<DeliveryPoint> deliveries, SharedData sharedData)
        {
            return await Task.Run(() =>
            {
                const int populationSize = 50;
                const int generations = 100;
                var random = new Random();

                var availableDeliveries = deliveries.Where(d => !d.IsDelivered).ToList();
                if (!availableDeliveries.Any()) return new Route { VehicleId = vehicle.Id };

                // Generar población inicial
                var population = new List<List<DeliveryPoint>>();
                for (int i = 0; i < populationSize; i++)
                {
                    var individual = availableDeliveries.OrderBy(x => random.Next()).Take((int)vehicle.MaxCapacity).ToList();
                    population.Add(individual);
                }

                // Evolución
                for (int gen = 0; gen < generations; gen++)
                {
                    // Evaluar fitness (menor distancia = mejor)
                    var fitness = population.Select(ind => new { Individual = ind, Fitness = CalculateRouteFitness(ind, vehicle) }).ToList();

                    // Seleccionar los mejores
                    var selected = fitness.OrderBy(f => f.Fitness).Take(populationSize / 2).Select(f => f.Individual).ToList();

                    // Generar nueva población con crossover y mutación
                    population.Clear();
                    population.AddRange(selected);

                    while (population.Count < populationSize)
                    {
                        var parent1 = selected[random.Next(selected.Count)];
                        var parent2 = selected[random.Next(selected.Count)];
                        var offspring = Crossover(parent1, parent2, random);
                        if (random.NextDouble() < 0.1) // 10% probabilidad de mutación
                            offspring = Mutate(offspring, availableDeliveries, random);
                        population.Add(offspring);
                    }
                }

                // Seleccionar la mejor solución
                var bestSolution = population.OrderBy(ind => CalculateRouteFitness(ind, vehicle)).First();

                return CreateRouteFromSequence(vehicle, bestSolution, sharedData);
            });
        }

        // Greedy Aleatorizado
        public async Task<Route> RandomizedGreedyAsync(Vehicle vehicle, List<DeliveryPoint> deliveries, SharedData sharedData, Random random)
        {
            return await Task.Run(() =>
            {
                var route = new Route { VehicleId = vehicle.Id, OptimizationStrategy = "RandomizedGreedy" };
                var unvisited = deliveries.Where(d => !d.IsDelivered).ToList();
                var currentLat = vehicle.Latitude;
                var currentLon = vehicle.Longitude;
                var totalDistance = 0.0;

                while (unvisited.Any() && route.DeliverySequence.Count < vehicle.MaxCapacity)
                {
                    // Seleccionar entre los 3 mejores candidatos aleatoriamente
                    var candidates = unvisited
                        .OrderBy(d => GetCachedDistance(currentLat, currentLon, d.Latitude, d.Longitude, sharedData))
                        .Take(Math.Min(3, unvisited.Count))
                        .ToList();

                    var selected = candidates[random.Next(candidates.Count)];

                    lock (sharedData.SyncLock)
                    {
                        if (!sharedData.GlobalDeliveries[selected.Id].IsDelivered)
                        {
                            var distance = GetCachedDistance(currentLat, currentLon, selected.Latitude, selected.Longitude, sharedData);

                            route.DeliverySequence.Add(selected);
                            totalDistance += distance;
                            currentLat = selected.Latitude;
                            currentLon = selected.Longitude;

                            sharedData.GlobalDeliveries[selected.Id].IsDelivered = true;
                        }
                    }

                    unvisited.Remove(selected);
                }

                route.TotalDistance = totalDistance;
                route.EstimatedTime = _distanceService.EstimateTime(totalDistance);
                route.FuelCost = _distanceService.EstimateFuelCost(totalDistance);
                route.DeliveriesOnTime = CalculateOnTimeDeliveries(route);

                return route;
            });
        }

        // Métodos auxiliares
        private double GetCachedDistance(double lat1, double lon1, double lat2, double lon2, SharedData sharedData)
        {
            var key = $"{lat1:F6}-{lon1:F6}-{lat2:F6}-{lon2:F6}";
            return sharedData.DistanceMatrix.GetOrAdd(key, _ => _distanceService.CalculateDistance(lat1, lon1, lat2, lon2));
        }

        private int CalculateOnTimeDeliveries(Route route)
        {
            var currentTime = DateTime.Now;
            var onTimeCount = 0;

            foreach (var delivery in route.DeliverySequence)
            {
                var travelTime = _distanceService.EstimateTime(_distanceService.CalculateDistance(
                    route.DeliverySequence.IndexOf(delivery) == 0 ? 0 : route.DeliverySequence[route.DeliverySequence.IndexOf(delivery) - 1].Latitude,
                    route.DeliverySequence.IndexOf(delivery) == 0 ? 0 : route.DeliverySequence[route.DeliverySequence.IndexOf(delivery) - 1].Longitude,
                    delivery.Latitude, delivery.Longitude));

                currentTime = currentTime.Add(travelTime).Add(TimeSpan.FromMinutes(5));

                if (currentTime.TimeOfDay <= delivery.DeliveryWindow.Add(TimeSpan.FromHours(1)))
                    onTimeCount++;
            }

            return onTimeCount;
        }

        private double CalculateRouteFitness(List<DeliveryPoint> sequence, Vehicle vehicle)
        {
            if (!sequence.Any()) return double.MaxValue;

            var totalDistance = 0.0;
            var currentLat = vehicle.Latitude;
            var currentLon = vehicle.Longitude;

            foreach (var delivery in sequence)
            {
                totalDistance += _distanceService.CalculateDistance(currentLat, currentLon, delivery.Latitude, delivery.Longitude);
                currentLat = delivery.Latitude;
                currentLon = delivery.Longitude;
            }

            return totalDistance;
        }

        private List<DeliveryPoint> Crossover(List<DeliveryPoint> parent1, List<DeliveryPoint> parent2, Random random)
        {
            var crossoverPoint = random.Next(1, Math.Min(parent1.Count, parent2.Count));
            var offspring = new List<DeliveryPoint>();

            offspring.AddRange(parent1.Take(crossoverPoint));
            offspring.AddRange(parent2.Where(d => !offspring.Contains(d)));

            return offspring;
        }

        private List<DeliveryPoint> Mutate(List<DeliveryPoint> individual, List<DeliveryPoint> allDeliveries, Random random)
        {
            var mutated = new List<DeliveryPoint>(individual);

            if (mutated.Count > 1 && random.NextDouble() < 0.5)
            {
                // Intercambiar dos posiciones
                var pos1 = random.Next(mutated.Count);
                var pos2 = random.Next(mutated.Count);
                (mutated[pos1], mutated[pos2]) = (mutated[pos2], mutated[pos1]);
            }
            else if (allDeliveries.Count > mutated.Count)
            {
                // Agregar una nueva entrega
                var available = allDeliveries.Where(d => !mutated.Contains(d)).ToList();
                if (available.Any())
                    mutated.Add(available[random.Next(available.Count)]);
            }

            return mutated;
        }

        private Route CreateRouteFromSequence(Vehicle vehicle, List<DeliveryPoint> sequence, SharedData sharedData)
        {
            var route = new Route { VehicleId = vehicle.Id, OptimizationStrategy = "GeneticAlgorithm" };
            var currentLat = vehicle.Latitude;
            var currentLon = vehicle.Longitude;
            var totalDistance = 0.0;

            foreach (var delivery in sequence)
            {
                var distance = GetCachedDistance(currentLat, currentLon, delivery.Latitude, delivery.Longitude, sharedData);
                totalDistance += distance;
                currentLat = delivery.Latitude;
                currentLon = delivery.Longitude;

                route.DeliverySequence.Add(delivery);

                lock (sharedData.SyncLock)
                {
                    if (sharedData.GlobalDeliveries.TryGetValue(delivery.Id, out var globalDelivery))
                    {
                        globalDelivery.IsDelivered = true;
                    }
                }
            }

            route.TotalDistance = totalDistance;
            route.EstimatedTime = _distanceService.EstimateTime(totalDistance);
            route.FuelCost = _distanceService.EstimateFuelCost(totalDistance);
            route.DeliveriesOnTime = CalculateOnTimeDeliveries(route);

            return route;
        }
    }
}