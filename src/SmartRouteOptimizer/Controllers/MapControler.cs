using Microsoft.AspNetCore.Mvc;

public class MapController : Controller
{
    public IActionResult Index()
    {
        return View(); 
    }
}