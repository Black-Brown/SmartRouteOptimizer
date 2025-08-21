using Microsoft.AspNetCore.Mvc;
using SmartRouteOptimizer.ViewModels;

namespace SmartRouteOptimizer.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            var viewModel = new DashboardViewModel();
            return View(viewModel);
        }

        public IActionResult RealTime()
        {
            return View();
        }

        public IActionResult Performance()
        {
            return View();
        }

        public IActionResult RouteVisualization()
        {
            return View();
        }
    }
}