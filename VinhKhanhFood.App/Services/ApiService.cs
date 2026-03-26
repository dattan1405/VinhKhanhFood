using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Json;
using VinhKhanhFood.App.Models;

namespace VinhKhanhFood.App.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;

        // 10.0.2.2 địa chỉ để máy ảo Android nhìn thấy máy
        private const string BaseUrl = "http://10.0.2.2:5020/api/Food";

        public ApiService()
        {
        // Bỏ qua kiểm tra SSL, debug trên máy ảo không bị chặn
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            _httpClient = new HttpClient(handler);
        }

        public async Task<List<FoodLocation>> GetFoodLocationsAsync()
        {
            try
            {
                // Gọi điện sang API lấy danh sách quán ốc
                var response = await _httpClient.GetFromJsonAsync<List<FoodLocation>>(BaseUrl);
                return response ?? new List<FoodLocation>();
            }
            catch (Exception ex)
            {
                // Nếu lỗi (ví dụ chưa bật API)
                Console.WriteLine($"Lỗi kết nối API: {ex.Message}");
                return new List<FoodLocation>();
            }
        }
    }
}
