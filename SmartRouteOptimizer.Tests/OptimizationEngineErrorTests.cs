using System;
using SmartRouteOptimizer.Api.Services;
using SmartRouteOptimizer.Api.Models;
using Xunit;

namespace SmartRouteOptimizer.Tests
{
    public class OptimizationEngineErrorTests
    {
        [Fact]
        public void StartOptimization_WithNullRequest_ThrowsArgumentNullException()
        {
            // Arrange
            var engine = new OptimizationEngine();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => engine.StartOptimization(null));
        }
    }
}
