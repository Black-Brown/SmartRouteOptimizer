using Xunit;
using SmartRouteOptimizer.Api.Services;
using SmartRouteOptimizer.Api.Models;
using System.Collections.Generic;
using System.Threading;

namespace SmartRouteOptimizer.Tests
{
    public class OptimizationSessionTests
    {
        [Fact]
        public void Session_InitializesCorrectly()
        {
            // Arrange
            var request = new OptimizationRequest
            {
                TimeLimitSeconds = 10,
                Vehicles = new List<VehicleDto>(),
                Clients = new List<ClientDto>()
            };

            // Act
            var session = new OptimizationSession(request);

            // Assert
            Assert.NotNull(session.Id);
            Assert.Equal(request, session.Request);
            Assert.Equal("Iniciando...", session.Message);
            Assert.InRange(session.ElapsedSeconds, 0, 1); // recién creado
            Assert.Equal(0, session.Evaluaciones);
            Assert.Equal(0, session.ProgressPercent);
            Assert.NotNull(session.Algorithms);
            Assert.Equal(4, session.Algorithms.Count);
        }

        [Fact]
        public void Session_TracksProgressCorrectly()
        {
            // Arrange
            var session = new OptimizationSession(new OptimizationRequest
            {
                TimeLimitSeconds = 5,
                Vehicles = new List<VehicleDto>(),
                Clients = new List<ClientDto>()
            });

            // Act
            session.Evaluaciones = 120;
            session.ProgressPercent = 75.5;
            session.Message = "Optimización avanzada";

            // Assert
            Assert.Equal(120, session.Evaluaciones);
            Assert.Equal(75.5, session.ProgressPercent);
            Assert.Equal("Optimización avanzada", session.Message);
        }

        [Fact]
        public void Session_TracksElapsedTime()
        {
            // Arrange
            var session = new OptimizationSession(new OptimizationRequest
            {
                TimeLimitSeconds = 5,
                Vehicles = new List<VehicleDto>(),
                Clients = new List<ClientDto>()
            });

            // Act
            Thread.Sleep(500); // Simula tiempo de ejecución

            // Assert
            Assert.True(session.ElapsedSeconds >= 0.5);
        }
    }
}
