using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using VinhKhanhFood.Admin.Models;
using Newtonsoft.Json;

namespace VinhKhanhFood.Admin.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        // Sử dụng IHttpClientFactory là cách chuyên nghiệp hơn, nhưng hiện tại dùng HttpClient cũng OK
        private readonly HttpClient _client = new HttpClient();

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var response = await _client.GetAsync("http://localhost:5020/api/Food/all"); // ✅ Sửa thành /all

                List<FoodLocation> locations = new List<FoodLocation>();

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    locations = JsonConvert.DeserializeObject<List<FoodLocation>>(json) ?? new List<FoodLocation>();
                }

                var dashboardData = locations.OrderByDescending(x => x.Id).ToList();
                return View(dashboardData);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi kết nối API Dashboard: {ex.Message}");
                return View(new List<FoodLocation>());
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}