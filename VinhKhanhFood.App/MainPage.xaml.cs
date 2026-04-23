using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using VinhKhanhFood.App.Models;
using VinhKhanhFood.App.Services;
using VinhKhanhFood.App.ViewModels;

namespace VinhKhanhFood.App;

public partial class MainPage : ContentPage
{
    private readonly MapViewModel _viewModel;
    private readonly QrAccessService _qrService;
    private ApiService _apiService = new ApiService();
    private CancellationTokenSource? _locationCts;

    public MainPage()
    {
        InitializeComponent();

        _viewModel = new MapViewModel();
        _qrService = new QrAccessService();

        BindingContext = _viewModel;
        _viewModel.OnLocationsLoaded += DrawMapElements;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            PermissionStatus gpsStatus = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (gpsStatus != PermissionStatus.Granted)
            {
                gpsStatus = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            vinhKhanhMap.IsShowingUser = gpsStatus == PermissionStatus.Granted;

            using (CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(15)))
            {
                try
                {
                    await _viewModel.InitializeAsync();
                }
                catch
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Map data timeout");
                }
            }

            string token = Preferences.Default.Get("pending_token", string.Empty);
            if (!string.IsNullOrWhiteSpace(token))
            {
                Preferences.Default.Remove("pending_token");
                using (CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                {
                    try
                    {
                        bool ok = await _qrService.VerifyAppAccess(token);
                        if (ok)
                        {
                            await DisplayAlert("✅ Xác thực thành công", "Chào mừng bạn!", "OK");
                        }
                        else
                        {
                            await DisplayAlert("❌ Lỗi", "Token không hợp lệ", "OK");
                        }
                    }
                    catch
                    {
                        await DisplayAlert("⚠️ Timeout", "Kết nối quá lâu", "OK");
                    }
                }
            }

            StartLocationTracking();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ OnAppearing error: {ex.Message}");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.StopTracking();
        StopLocationTracking();
    }

    private void DrawMapElements(List<FoodLocation> locations)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            vinhKhanhMap.Pins.Clear();
            vinhKhanhMap.MapElements.Clear();

            foreach (FoodLocation loc in locations)
            {
                Location pinLocation = new Location(loc.Latitude, loc.Longitude);

                Pin pin = new Pin
                {
                    Label = loc.Name,
                    Address = "Nhấn để xem chi tiết",
                    Location = pinLocation
                };

                pin.MarkerClicked += OnMapInfoWindowClicked!;
                vinhKhanhMap.Pins.Add(pin);

                Circle circle = new Circle
                {
                    Center = pinLocation,
                    Radius = new Distance(30),
                    StrokeColor = Colors.Blue,
                    StrokeWidth = 2,
                    FillColor = Color.FromArgb("#330099FF")
                };
                vinhKhanhMap.MapElements.Add(circle);
            }

            if (locations.Any())
            {
                FoodLocation first = locations[0];
                vinhKhanhMap.MoveToRegion(MapSpan.FromCenterAndRadius(
                    new Location(first.Latitude, first.Longitude), Distance.FromKilometers(0.5)));
            }
        });
    }

    private async void OnMapInfoWindowClicked(object? sender, PinClickedEventArgs e)
    {
        e.HideInfoWindow = true;

        if (sender is Pin clickedPin)
        {
            FoodLocation? locData = _viewModel.Locations.FirstOrDefault(l => l.Name == clickedPin.Label);
            if (locData != null)
            {
                FoodBottomSheet.BindingContext = locData;
                FoodBottomSheet.IsVisible = true;
                FoodBottomSheet.TranslationY = 300;
                await FoodBottomSheet.TranslateTo(0, 0, 300, Easing.SinOut);
            }
        }
    }

    private async void OnPlayAudioClicked(object sender, EventArgs e)
    {
        await _viewModel.PlayGeneralIntroAsync();
    }

    private async void OnCloseBottomSheetClicked(object sender, EventArgs e)
    {
        _viewModel.CancelSpeech();
        await FoodBottomSheet.TranslateTo(0, 300, 250, Easing.SinIn);
        FoodBottomSheet.IsVisible = false;
    }

    private async void OnViewDetailsClicked(object sender, EventArgs e)
    {
        if (FoodBottomSheet.BindingContext is FoodLocation selectedLocation)
        {
            _viewModel.CancelSpeech();
            await Navigation.PushAsync(new DetailPage(selectedLocation));
        }
    }

    private void StartLocationTracking()
    {
        StopLocationTracking();

        _locationCts = new CancellationTokenSource();
        CancellationToken token = _locationCts.Token;

        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // Lấy vị trí hiện tại
                    Location? location = await GetCurrentLocationAsync();
                    if (location != null)
                    {
                        // Gửi lên server
                        await _apiService.UpdateVisitorLocationAsync(location.Latitude, location.Longitude);
                        System.Diagnostics.Debug.WriteLine($"📍 Gửi vị trí: {location.Latitude}, {location.Longitude}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Location tracking error: {ex.Message}");
                }

                // Gửi mỗi 30 giây
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }, token);
    }

    private void StopLocationTracking()
    {
        if (_locationCts != null)
        {
            _locationCts.Cancel();
            _locationCts.Dispose();
            _locationCts = null;
        }
    }

    private async Task<Location?> GetCurrentLocationAsync()
    {
        try
        {
            Location location = await Geolocation.Default.GetLocationAsync(
                new GeolocationRequest
                {
                    DesiredAccuracy = GeolocationAccuracy.Best,
                    Timeout = TimeSpan.FromSeconds(10)
                });

            return location;
        }
        catch
        {
            return null;
        }
    }
}