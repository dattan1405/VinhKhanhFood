using System;
using System.Net.Http.Json;

namespace VinhKhanhFood.App.Services
{
    public class QrAccessService
    {
        private const string ApiBaseUrl = "http://10.17.186.213:5020";

        public async Task<bool> VerifyAppAccess(string token)
        {
            try
            {
                string deviceId = $"{Microsoft.Maui.Devices.DeviceInfo.Current.Model}_{Microsoft.Maui.Devices.DeviceInfo.Current.Name}";

                // ✅ FIX: Tăng timeout từ 10s → 30s
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(30);
                
                var url = $"{ApiBaseUrl}/api/QrManagement/verify";
                var payload = new { Token = token, DeviceId = deviceId };
                
                System.Diagnostics.Debug.WriteLine($"📱 Verify token: {token}");
                System.Diagnostics.Debug.WriteLine($"🌐 API URL: {url}");

                var response = await client.PostAsJsonAsync(url, payload);

                System.Diagnostics.Debug.WriteLine($"✅ Verify response: {response.StatusCode}");
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Network error: {ex.Message}");
                return false;
            }
            catch (TaskCanceledException ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Timeout error: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ General error: {ex.Message}");
                return false;
            }
        }
    }
}
