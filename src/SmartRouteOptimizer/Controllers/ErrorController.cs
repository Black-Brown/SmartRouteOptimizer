using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using SmartRouteOptimizer.Models;

namespace SmartRouteOptimizer.Controllers
{
    public class ErrorController : Controller
    {
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Index()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
