using Microsoft.Maui.Storage;

namespace VinhKhanhFood.App.Services
{
    public static class DeviceIdProvider
    {
        private const string DeviceIdKey = "vkh_device_id";

        public static string GetDeviceId()
        {
            string deviceId = Preferences.Default.Get(DeviceIdKey, string.Empty);

            if (!string.IsNullOrWhiteSpace(deviceId))
            {
                return deviceId;
            }

            deviceId = $"{Microsoft.Maui.Devices.DeviceInfo.Current.Platform}_{Guid.NewGuid():N}";
            Preferences.Default.Set(DeviceIdKey, deviceId);

            return deviceId;
        }
    }
}