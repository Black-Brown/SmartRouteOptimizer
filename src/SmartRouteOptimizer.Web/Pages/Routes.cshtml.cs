using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SmartRouteOptimizer.Web.Pages
{
    public class RoutesModel : PageModel
    {
        private readonly HttpClient _httpClient;

        public List<RouteDto> Routes { get; set; } = new();

        public RoutesModel(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("ApiClient");
        }

        public async Task OnGetAsync()
        {
            Routes = await _httpClient.GetFromJsonAsync<List<RouteDto>>("api/routes");
        }
    }

    public class RouteDto
    {
        public int Id { get; set; }
        public string Start { get; set; }
        public string End { get; set; }
        public double Distance { get; set; }
    }
}

