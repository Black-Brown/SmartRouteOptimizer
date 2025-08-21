using Microsoft.AspNetCore.Mvc;
using SmartRouteOptimizer.Models;
using SmartRouteOptimizer.Services;
using SmartRouteOptimizer.ViewModels;

namespace SmartRouteOptimizer.Controllers
{
    public class HomeController : Controller
    {
        private readonly IOptimizationService _optimizationService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IOptimizationService optimizationService, ILogger<HomeController> logger)
        {
            _optimizationService = optimizationService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            var viewModel = new OptimizationViewModel
            {
                DeliveryPoints = GenerateSampleDeliveries(),
                Vehicles = GenerateSampleVehicles(),
                SelectedStrategy = ParallelizationStrategy.ByHeuristic,
                MaxThreads = Environment.ProcessorCount
            };

            return View(viewModel);
        }

        public IActionResult Dashboard()
        {
            return View(new DashboardViewModel());
        }

        private List<DeliveryPoint> GenerateSampleDeliveries()
        {
            var random = new Random(42); // Seed fijo para reproducibilidad
            var deliveries = new List<DeliveryPoint>();

            // Generar 50 entregas de ejemplo en Santo Domingo
            var baseLatitude = 18.4861;  // Santo Domingo
            var baseLongitude = -69.9312;

            for (int i = 1; i <= 50; i++)
            {
                deliveries.Add(new DeliveryPoint
                {
                    Id = i,
                    Address = $"Dirección {i}, Santo Domingo",
                    Latitude = baseLatitude + (random.NextDouble() - 0.5) * 0.1, // Variación de ~5km
                    Longitude = baseLongitude + (random.NextDouble() - 0.5) * 0.1,
                    DeliveryWindow = TimeSpan.FromHours(9 + random.Next(8)), // 9AM - 5PM
                    Priority = random.Next(1, 4), // 1=Alta, 2=Media, 3=Baja
                    PackageWeight = random.NextDouble() * 10 + 1, // 1-11 kg
                    IsDelivered = false
                });
            }

            return deliveries;
        }

        private List<Vehicle> GenerateSampleVehicles()
        {
            var random = new Random(42);
            var vehicles = new List<Vehicle>();
            var driverNames = new[] { "Carlos Méndez", "María García", "Juan Rodríguez", "Ana Martínez", "Luis Fernández" };

            for (int i = 1; i <= 5; i++)
            {
                vehicles.Add(new Vehicle
                {
                    Id = i,
                    DriverName = driverNames[i - 1],
                    Latitude = 18.4861 + (random.NextDouble() - 0.5) * 0.05,
                    Longitude = -69.9312 + (random.NextDouble() - 0.5) * 0.05,
                    MaxCapacity = 15 + random.Next(10), // 15-25 entregas
                    CurrentLoad = 0,
                    IsAvailable = true
                });
            }

            return vehicles;
        }
    }
}