namespace SmartRouteOptimizer.Models
{
    public enum ParallelizationStrategy
    {
        Sequential,           // Sin paralelismo (base de comparación)
        ByHeuristic,         // Cada heurística en un hilo diferente
        ByGeographicZone,    // Un hilo por zona geográfica
        ByCandidateRoutes,   // Exploración aleatoria en paralelo
        HybridApproach       // Combinación de estrategias
    }
}