using Microsoft.Maui.Devices.Sensors;
using VinhKhanhFood.App.Models;
using VinhKhanhFood.App.Services;

namespace VinhKhanhFood.App.ViewModels;

public class MapViewModel
{
    private readonly ApiService _apiService = new ApiService();

    // Nơi chứa dữ liệu sau khi tải về, Giao diện sẽ lấy từ đây
    public List<FoodLocation> Locations { get; private set; } = new();

    private bool _isTrackingLocation = false;
    private HashSet<int> _readLocationIds = new();
    private CancellationTokenSource? _cts;
    private CancellationTokenSource? _speechCts;

    // Sự kiện "bắn tin" cho giao diện biết khi đã tải API xong
    public event Action<List<FoodLocation>>? OnLocationsLoaded;

    /// <summary>
    /// Hàm khởi chạy (Gom chung API và Tracking)
    /// </summary>
    public async Task InitializeAsync()
    {
        await LoadDataAsync();
        await StartTrackingLocationAsync();
    }

    /// <summary>
    /// Tắt định vị và âm thanh khi thoát trang
    /// </summary>
    public void StopTracking()
    {
        _isTrackingLocation = false;
        _cts?.Cancel();
        _speechCts?.Cancel();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            Locations = await _apiService.GetFoodLocationsAsync();
            if (Locations != null && Locations.Count > 0)
            {
                // Gọi sự kiện để báo cho MainPage vẽ bản đồ
                OnLocationsLoaded?.Invoke(Locations);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Lỗi API: {ex.Message}");
        }
    }

    private async Task StartTrackingLocationAsync()
    {
        try
        {
            PermissionStatus status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted) return;

            _isTrackingLocation = true;
            _cts = new CancellationTokenSource();

            Device.StartTimer(TimeSpan.FromSeconds(3), () =>
            {
                if (!_isTrackingLocation || _cts?.IsCancellationRequested == true)
                    return false;

                _ = PollLocationAsync();
                return _isTrackingLocation;
            });
        }
        catch { /* Bỏ qua log lỗi để app không văng */ }
    }

    private async Task PollLocationAsync()
    {
        try
        {
            var location = await Geolocation.Default.GetLocationAsync();
            if (location != null) CheckGeofence(location);
        }
        catch { }
    }

    private void CheckGeofence(Location userLocation)
    {
        if (Locations == null) return;

        var nearby = Locations
            .Where(loc => userLocation.CalculateDistance(new Location(loc.Latitude, loc.Longitude), DistanceUnits.Kilometers) <= 0.03)
            .Where(loc => !_readLocationIds.Contains(loc.Id))
            .OrderBy(loc => userLocation.CalculateDistance(new Location(loc.Latitude, loc.Longitude), DistanceUnits.Kilometers))
            .ToList();

        var closest = nearby.FirstOrDefault();
        if (closest != null)
        {
            _readLocationIds.Add(closest.Id);
            string message = !string.IsNullOrEmpty(closest.Description)
                ? $"Bạn đang ở gần {closest.Name}. {closest.Description}"
                : $"Bạn đang ở gần {closest.Name}";

            TextToSpeech.Default.SpeakAsync(message);
        }
    }

    // --- CÁC HÀM XỬ LÝ ÂM THANH CHO GIAO DIỆN GỌI ---

    public async Task PlayPinAudioAsync(FoodLocation locData)
    {
        string speechText = !string.IsNullOrEmpty(locData.Description)
                            ? locData.Description
                            : $"Đây là quán {locData.Name}";

        _speechCts?.Cancel();
        _speechCts = new CancellationTokenSource();

        try
        {
            await TextToSpeech.Default.SpeakAsync(speechText, cancelToken: _speechCts.Token);
        }
        catch (TaskCanceledException) { }
    }

    public async Task PlayGeneralIntroAsync()
    {
        _readLocationIds.Clear();
        await TextToSpeech.Default.SpeakAsync("Chế độ hướng dẫn viên tự động đã bật. Hãy bắt đầu đi dạo phố Vĩnh Khánh nào!");
    }

    public void CancelSpeech()
    {
        _speechCts?.Cancel();
    }
}