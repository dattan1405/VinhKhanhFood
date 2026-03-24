using Microsoft.Maui.Maps;
using Microsoft.Maui.Controls.Maps;
using VinhKhanhFood.App.Models;
using VinhKhanhFood.App.ViewModels; // Nhớ thêm dòng này

namespace VinhKhanhFood.App;

public partial class MainPage : ContentPage
{
    // Khai báo bộ não trung tâm
    private readonly MapViewModel _viewModel;

    public MainPage()
    {
        InitializeComponent();

        // Khởi tạo và đăng ký nhận dữ liệu từ ViewModel
        _viewModel = new MapViewModel();
        _viewModel.OnLocationsLoaded += DrawMapElements;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Ra lệnh cho ViewModel bắt đầu làm việc
        await _viewModel.InitializeAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // Ra lệnh cho ViewModel ngừng theo dõi GPS
        _viewModel.StopTracking();
    }

    /// <summary>
    /// Hàm này chỉ chạy khi ViewModel báo là đã lấy xong dữ liệu từ API
    /// </summary>
    private void DrawMapElements(List<FoodLocation> locations)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            vinhKhanhMap.Pins.Clear();
            vinhKhanhMap.MapElements.Clear();

            foreach (var loc in locations)
            {
                var pinLocation = new Location(loc.Latitude, loc.Longitude);

                // Vẽ Pin
                var pin = new Pin
                {
                    Label = loc.Name,
                    Address = "Nhấn để nghe giới thiệu chi tiết",
                    Location = pinLocation
                };
                pin.MarkerClicked += OnMapInfoWindowClicked;
                vinhKhanhMap.Pins.Add(pin);

                // Vẽ Vòng tròn 30m
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

            // Di chuyển camera
            if (locations.Any())
            {
                var first = locations[0];
                vinhKhanhMap.MoveToRegion(MapSpan.FromCenterAndRadius(
                    new Location(first.Latitude, first.Longitude), Distance.FromKilometers(0.5)));
            }
        });
    }

    private async void OnMapInfoWindowClicked(object sender, PinClickedEventArgs e)
    {
        e.HideInfoWindow = false;

        if (sender is Pin clickedPin)
        {
            // Lấy data từ ViewModel
            var locData = _viewModel.Locations.FirstOrDefault(l => l.Name == clickedPin.Label);
            if (locData != null)
            {
                // Nếu có BottomSheet, mở khóa code dưới đây để gán tên:
                // LblFoodName.Text = locData.Name;
                // LblFoodDescription.Text = locData.Description;
                // FoodBottomSheet.IsVisible = true;

                // Nhờ ViewModel đọc âm thanh
                await _viewModel.PlayPinAudioAsync(locData);
            }
        }
    }

    private async void OnPlayAudioClicked(object sender, EventArgs e)
    {
        await _viewModel.PlayGeneralIntroAsync();
    }

    private void OnCloseBottomSheetClicked(object sender, EventArgs e)
    {
        _viewModel.CancelSpeech();
        // FoodBottomSheet.IsVisible = false; // Mở khóa dòng này nếu dùng BottomSheet
    }
}