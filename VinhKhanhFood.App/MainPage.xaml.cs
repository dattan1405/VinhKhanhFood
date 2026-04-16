using Microsoft.Maui.Maps;
using Microsoft.Maui.Controls.Maps;
using VinhKhanhFood.App.Models;
using VinhKhanhFood.App.ViewModels;
using VinhKhanhFood.App.Services;

namespace VinhKhanhFood.App;

public partial class MainPage : ContentPage
{
    private readonly MapViewModel _viewModel;
    private readonly QrAccessService _qrService; // ✅ FIX: Thêm dòng này

    public MainPage()
    {
        InitializeComponent();

        _viewModel = new MapViewModel();
        _qrService = new QrAccessService(); // ✅ FIX: Khởi tạo service

        BindingContext = _viewModel;

        _viewModel.OnLocationsLoaded += DrawMapElements;

        // ✅ FIX: Thay MessagingCenter (deprecated) bằng WeakReferenceMessenger
        // Nếu bạn chưa cài MVVM Toolkit, xóa đoạn này tạm thời
        // MessagingCenter.Subscribe<object>(this, "LanguageChanged", (sender) =>
        // {
        //     if (_viewModel.Locations != null && _viewModel.Locations.Any())
        //     {
        //         DrawMapElements(_viewModel.Locations);
        //     }
        // });
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        try
        {
            // 1. Lấy quyền GPS TRƯỚC HẾT để Google Map không crash
            var gpsStatus = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (gpsStatus != PermissionStatus.Granted)
            {
                gpsStatus = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            // 2. Hiển thị user location (Chấm xanh) nếu được cấp quyền
            if (gpsStatus == PermissionStatus.Granted)
            {
                vinhKhanhMap.IsShowingUser = true;
            }
            else
            {
                vinhKhanhMap.IsShowingUser = false; // Tắt châm xanh đi nếu bị deny
            }

            // 3. Load dữ liệu ghim quán (timeout handle)
            if (_viewModel != null)
            {
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15)))
                {
                    try { await _viewModel.InitializeAsync(); }
                    catch { System.Diagnostics.Debug.WriteLine("⚠️ Map data timeout"); }
                }
            }

            // 4. Check token QR (Giữ nguyên)
            string token = Preferences.Default.Get("pending_token", string.Empty);
            if (!string.IsNullOrWhiteSpace(token))
            {
                Preferences.Default.Remove("pending_token");
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                {
                    try
                    {
                        bool ok = await _qrService.VerifyAppAccess(token);
                        if (ok) await DisplayAlert("✅ Xác thực thành công", "Chào mừng bạn!", "OK");
                        else await DisplayAlert("❌ Lỗi", "Token không hợp lệ", "OK");
                    }
                    catch { await DisplayAlert("⚠️ Timeout", "Kết nối quá lâu", "OK"); }
                }
            }
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
    }

    private void DrawMapElements(List<FoodLocation> locations)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            vinhKhanhMap.Pins.Clear();
            vinhKhanhMap.MapElements.Clear();

            foreach (var loc in locations)
            {
                var pinLocation = new Location(loc.Latitude, loc.Longitude);

                var pin = new Pin
                {
                    Label = loc.Name,
                    Address = "Nhấn để xem chi tiết",
                    Location = pinLocation
                };

                // ✅ FIX: Sửa nullability - thêm dấu ? trước object
                pin.MarkerClicked += OnMapInfoWindowClicked!;
                vinhKhanhMap.Pins.Add(pin);

                var circle = new Circle
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
                var first = locations[0];
                vinhKhanhMap.MoveToRegion(MapSpan.FromCenterAndRadius(
                    new Location(first.Latitude, first.Longitude), Distance.FromKilometers(0.5)));
            }
        });
    }

    // ✅ FIX: Thêm ? cho parameter sender
    private async void OnMapInfoWindowClicked(object? sender, PinClickedEventArgs e)
    {
        e.HideInfoWindow = true;

        if (sender is Pin clickedPin)
        {
            var locData = _viewModel.Locations.FirstOrDefault(l => l.Name == clickedPin.Label);
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
}