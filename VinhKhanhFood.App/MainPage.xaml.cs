using Microsoft.Maui.Maps;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using VinhKhanhFood.App.Models;
using VinhKhanhFood.App.Services;

namespace VinhKhanhFood.App;

public partial class MainPage : ContentPage
{
    private readonly ApiService _apiService = new ApiService();
    private List<FoodLocation> _locations = new();
    private bool _isTrackingLocation = false;
    private HashSet<int> _readLocationIds = new();
    private CancellationTokenSource? _cts;
    private CancellationTokenSource? _speechCts;
    private Location? _lastUserLocation;

    public MainPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadPinsFromApi();
        await StartTrackingLocation();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopTrackingLocation();
    }

    /// <summary>
    /// Tải danh sách quán từ API, vẽ pins và vòng tròn bán kính 30m
    /// </summary>
    private async Task LoadPinsFromApi()
    {
        try
        {
            _locations = await _apiService.GetFoodLocationsAsync();

            if (_locations != null && _locations.Count > 0)
            {
                vinhKhanhMap.Pins.Clear();
                vinhKhanhMap.MapElements.Clear();

                foreach (var loc in _locations)
                {
                    var pinLocation = new Location(loc.Latitude, loc.Longitude);

                    // 1. TẠO PIN CHO QUÁN ỐC
                    var pin = new Pin
                    {
                        Label = loc.Name,
                        Address = "Nhấn để nghe giới thiệu chi tiết",
                        Location = pinLocation
                    };
                    pin.MarkerClicked += OnMapInfoWindowClicked;
                    vinhKhanhMap.Pins.Add(pin);

                    // 2. TẠO VÒNG TRÒN BÁN KÍNH 30M
                    var circle = new Circle
                    {
                        Center = pinLocation,
                        Radius = new Distance(30), // 30 mét
                        StrokeColor = Colors.Blue,
                        StrokeWidth = 2,
                        FillColor = Color.FromArgb("#330099FF") // Xanh nhạt, trong suốt
                    };
                    vinhKhanhMap.MapElements.Add(circle);
                }

                // Đưa camera về POI đầu tiên để dễ nhìn
                var first = _locations[0];
                vinhKhanhMap.MoveToRegion(MapSpan.FromCenterAndRadius(
                    new Location(first.Latitude, first.Longitude),
                    Distance.FromKilometers(0.5)));
            }
            else
            {
                await DisplayAlert("Thông báo", "Không có dữ liệu quán ốc từ API", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", $"Lỗi khi tải dữ liệu: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// Bắt đầu theo dõi vị trí người dùng bằng polling (mỗi 3 giây)
    /// </summary>
    private async Task StartTrackingLocation()
    {
        try
        {
            PermissionStatus status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                await DisplayAlert("Thông báo", "Cần cấp quyền GPS để sử dụng tính năng này", "OK");
                return;
            }

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
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi GPS", $"Không thể bật GPS: {ex.Message}", "OK");
        }
    }

    private async Task PollLocationAsync()
    {
        try
        {
            var location = await Geolocation.Default.GetLocationAsync();
            if (location != null)
            {
                OnLocationChanged(location);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Lỗi khi lấy vị trí: {ex.Message}");
        }
    }

    private void StopTrackingLocation()
    {
        _isTrackingLocation = false;
        _cts?.Cancel();
    }

    /// <summary>
    /// Xử lý khi có vị trí mới (Chỉ quét quán, không tự giật bản đồ)
    /// </summary>
    private void OnLocationChanged(Location userLocation)
    {
        try
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Cập nhật vị trí lưu vết (Đã khóa lệnh MoveToRegion để Đạt tự do vuốt map)
                _lastUserLocation = userLocation;

                // TÌM QUÁN GẦN NHẤT TRONG BÁN KÍNH 30M
                var nearby = _locations
                    .Where(loc => userLocation.CalculateDistance(
                        new Location(loc.Latitude, loc.Longitude),
                        DistanceUnits.Kilometers) <= 0.03) // 30m
                    .Where(loc => !_readLocationIds.Contains(loc.Id)) // Chưa phát tiếng
                    .OrderBy(loc => userLocation.CalculateDistance(
                        new Location(loc.Latitude, loc.Longitude),
                        DistanceUnits.Kilometers)) // Ưu tiên quán gần nhất
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
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Lỗi khi cập nhật vị trí: {ex.Message}");
        }
    }

    /// <summary>
    /// Phát âm thanh khi người dùng lấy tay bấm vào một cái Pin bất kỳ
    /// </summary>
    /// <summary>
    /// Phát âm thanh khi người dùng lấy tay bấm vào một cái Pin bất kỳ
    /// </summary>
    private async void OnMapInfoWindowClicked(object sender, PinClickedEventArgs e)
    {
        // Ẩn bảng tên mặc định
        e.HideInfoWindow = true;

        var clickedPin = sender as Pin;
        if (clickedPin == null) return;

        var locData = _locations.FirstOrDefault(l => l.Name == clickedPin.Label);

        if (locData != null)
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
            catch (TaskCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("Đã ngắt giọng đọc cũ.");
            }
        }
    }

    /// <summary>
    /// Phát âm thanh giới thiệu chung và reset danh sách đã đọc
    /// </summary>
    private async void OnPlayAudioClicked(object sender, EventArgs e)
    {
        try
        {
            _readLocationIds.Clear();
            string introText = "Chế độ hướng dẫn viên tự động đã bật. Hãy bắt đầu đi dạo phố Vĩnh Khánh nào!";
            await TextToSpeech.Default.SpeakAsync(introText);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", $"Lỗi phát âm: {ex.Message}", "OK");
        }
    }
}