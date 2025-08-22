using Xunit;
using SmartRouteOptimizer.Api.Models;
using SmartRouteOptimizer.Api.Services;
using System.Collections.Generic;
using System.Threading;
using System;

namespace SmartRouteOptimizer.Tests
{
    public class OptimizationEngineTests
    {
        [Fact]
        public void StartOptimization_WithValidRequest_ReturnsSolutionEventually()
        {
            // Arrange
            var engine = new OptimizationEngine();
            var request = new OptimizationRequest
            {
                TimeLimitSeconds = 5,
                Vehicles = new List<VehicleDto>
                {
                    new VehicleDto { Id = 1, Color = "#FF0000", Capacidad = 10 }
                },
                Clients = new List<ClientDto>
                {
                    new ClientDto { Id = 101, Lat = 18.48, Lng = -69.93, Nombre = "Cliente A", Prioridad = 1, VentanaInicio = 8.0, VentanaFin = 12.0 },
                    new ClientDto { Id = 102, Lat = 18.49, Lng = -69.92, Nombre = "Cliente B", Prioridad = 2, VentanaInicio = 9.0, VentanaFin = 13.0 }
                }
            };

            // Act
            var sessionId = engine.StartOptimization(request);

            // Esperar hasta que la solución esté disponible (máx 10 segundos)
            SolutionDto? solution = null;
            var timeout = DateTime.UtcNow.AddSeconds(10);
            while (DateTime.UtcNow < timeout)
            {
                solution = engine.GetSolution(sessionId);
                if (solution != null) break;
                Thread.Sleep(200);
            }

            // Assert
            Assert.NotNull(solution);
            Assert.NotEmpty(solution.Routes);
            Assert.NotNull(solution.Best);
            Assert.True(solution.CostPerDelivery >= 0);
        }
    }
}

