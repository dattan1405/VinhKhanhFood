using System.Net.Http.Json;

namespace VinhKhanhFood.App.Services
{
    public class QrAccessService
    {
        //máy thật
        private const string ApiBaseUrl = "http://192.168.130.213:5020";
        //máy ảo
        //private const string ApiBaseUrl = "http://10.0.2.2:5020";

        private sealed class DeviceHeartbeatRequest
        {
            public string DeviceId { get; set; } = string.Empty;
        }

        public string GetDeviceId()
        {
            return $"{Microsoft.Maui.Devices.DeviceInfo.Current.Platform}_{Microsoft.Maui.Devices.DeviceInfo.Current.Model}_{Microsoft.Maui.Devices.DeviceInfo.Current.Name}";
        }

        public async Task<bool> VerifyAppAccess(string token)
        {
            try
            {
                string deviceId = GetDeviceId();

                using HttpClient client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(30);

                string url = $"{ApiBaseUrl}/api/QrManagement/verify";
                var payload = new { Token = token, DeviceId = deviceId };

                HttpResponseMessage response = await client.PostAsJsonAsync(url, payload);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> SendHeartbeatAsync()
        {
            try
            {
                string deviceId = GetDeviceId();

                using HttpClient client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(10);

                string url = $"{ApiBaseUrl}/api/QrManagement/heartbeat";
                DeviceHeartbeatRequest payload = new DeviceHeartbeatRequest { DeviceId = deviceId };

                HttpResponseMessage response = await client.PostAsJsonAsync(url, payload);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
