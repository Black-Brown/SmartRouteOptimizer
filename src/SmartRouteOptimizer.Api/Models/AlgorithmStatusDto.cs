namespace SmartRouteOptimizer.Api.Models;

public record AlgorithmStatusDto(
    string Name,
    string State,
    double? Cost,
    DateTimeOffset LastUpdate
);