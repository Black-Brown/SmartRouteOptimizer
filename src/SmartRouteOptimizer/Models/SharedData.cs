using SmartRouteOptimizer.Models;
using System.Collections.Concurrent;

namespace SmartRouteOptimizer.Models
{
    public class SharedData
    {
        // Matriz de distancias compartida entre hilos
        public ConcurrentDictionary<string, double> DistanceMatrix { get; set; } = new();

        // Estado global de entregas
        public ConcurrentDictionary<int, DeliveryPoint> GlobalDeliveries { get; set; } = new();

        // Restricciones compartidas
        public ConcurrentDictionary<int, Vehicle> AvailableVehicles { get; set; } = new();

        // Mejores rutas encontradas
        public ConcurrentDictionary<string, Route> BestRoutes { get; set; } = new();

        // Lock para operaciones críticas
        public readonly object SyncLock = new object();

        // Semáforo para controlar acceso a recursos limitados
        public SemaphoreSlim ResourceSemaphore { get; set; } = new(Environment.ProcessorCount);

        // Estadísticas en tiempo real
        public ConcurrentDictionary<string, double> LiveMetrics { get; set; } = new();
    }
}