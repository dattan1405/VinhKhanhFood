using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Diagnostics;
using VinhKhanhFood.Admin.Models;

namespace VinhKhanhFood.Admin.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly HttpClient _client = new HttpClient();

        private sealed class ActiveDevicesResponse
        {
            [JsonProperty("count")]
            public int Count { get; set; }
        }

        private const string ApiBaseUrl = "http://localhost:5020";

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                HttpResponseMessage response = await _client.GetAsync($"{ApiBaseUrl}/api/Food/all");
                List<FoodLocation> locations = new List<FoodLocation>();

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    locations = JsonConvert.DeserializeObject<List<FoodLocation>>(json) ?? new List<FoodLocation>();
                }

                int activeDeviceCount = await GetActiveDeviceCountInternalAsync();
                ViewBag.ActiveDeviceCount = activeDeviceCount;

                List<FoodLocation> dashboardData = locations.OrderByDescending(x => x.Id).ToList();
                return View(dashboardData);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi kết nối API Dashboard: {ex.Message}");
                ViewBag.ActiveDeviceCount = 0;
                return View(new List<FoodLocation>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> ActiveDevicesCount()
        {
            try
            {
                int count = await GetActiveDeviceCountInternalAsync();
                return Json(new { count });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi lấy active devices count: {ex.Message}");
                return Json(new { count = 0 });
            }
        }

        private async Task<int> GetActiveDeviceCountInternalAsync()
        {
            HttpResponseMessage activeResponse = await _client.GetAsync($"{ApiBaseUrl}/api/QrManagement/active-devices-count?activeWithinSeconds=15");
            if (!activeResponse.IsSuccessStatusCode)
            {
                return 0;
            }

            string activeJson = await activeResponse.Content.ReadAsStringAsync();
            ActiveDevicesResponse? activeData = JsonConvert.DeserializeObject<ActiveDevicesResponse>(activeJson);
            return (activeData?.Count ?? 0) *1 ;
            // *2 thiết bị online
            //return (activeData?.Count ?? 0) * 2;
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

        [HttpGet]
        public async Task<IActionResult> ActiveVisitors()
        {
            try
            {
                HttpResponseMessage response = await _client.GetAsync($"{ApiBaseUrl}/api/Visitor/active?withinMinutes=5");

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<dynamic>(json);
                    return Json(data);
                }

                return Json(new { count = 0, visitors = new List<object>() });
            }
            catch
            {
                return Json(new { count = 0, visitors = new List<object>() });
            }
        }

        [HttpGet("monitor")]
        public IActionResult Monitor()
        {
            return View();
        }

        [HttpGet("GetVisitorMonitorData")]
        public async Task<IActionResult> GetVisitorMonitorData()
        {
            try
            {
                HttpResponseMessage response = await _client.GetAsync($"{ApiBaseUrl}/api/Visitor/realtime-monitoring");

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<dynamic>(json);

                    return Json(new
                    {
                        count = result["count"],
                        data = result["data"]
                    });
                }

                return Json(new { count = 0, data = new List<object>() });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi GetVisitorMonitorData: {ex.Message}");
                return Json(new { count = 0, data = new List<object>(), error = ex.Message });
            }
        }
    }
}