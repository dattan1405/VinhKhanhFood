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
            _httpClient.Timeout = TimeSpan.FromSeconds(10); // ✅ Thêm timeout
        }

        public async Task<List<FoodLocation>> GetFoodLocationsAsync()
        {
            try
            {
                // 1. Gọi API lấy danh sách thô về
                System.Diagnostics.Debug.WriteLine($" Gọi API: {BaseUrl}");
                var response = await _httpClient.GetFromJsonAsync<List<FoodLocation>>(BaseUrl);

                if (response != null)
                {
                    System.Diagnostics.Debug.WriteLine($" API trả về {response.Count} quán");
                    
                    // 2. Vòng lặp để nối thêm địa chỉ máy ảo Android (10.0.2.2) cho từng quán
                    foreach (var loc in response)
                    {
                        System.Diagnostics.Debug.WriteLine($"📍 {loc.Id} - {loc.Name} - ImageUrl gốc: {loc.ImageUrl}");
                        
                        if (!string.IsNullOrEmpty(loc.ImageUrl))
                        {
                            // Kiểm tra nếu đã có "http" rồi thì không thêm nữa
                            if (!loc.ImageUrl.StartsWith("http"))
                            {
                                loc.ImageUrl = $"http://10.0.2.2:5020/images/{loc.ImageUrl}";
                                System.Diagnostics.Debug.WriteLine($"    ImageUrl sau xử lý: {loc.ImageUrl}");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"    ImageUrl đã có full URL rồi: {loc.ImageUrl}");
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"    ImageUrl trống");
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($" Xong! Trả về {response.Count} quán");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"❌ API trả về null");
                }

                return response ?? new List<FoodLocation>();
            }
            catch (HttpRequestException httpEx)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Lỗi HTTP: {httpEx.Message}");
                System.Diagnostics.Debug.WriteLine($"   Inner: {httpEx.InnerException?.Message}");
                return new List<FoodLocation>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Lỗi API: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"   Stack: {ex.StackTrace}");
                return new List<FoodLocation>();
            }
        }
    }
}
