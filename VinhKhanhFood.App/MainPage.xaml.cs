using Microsoft.Maui.Maps;
using Microsoft.Maui.Controls.Maps;
using VinhKhanhFood.App.Models;
using VinhKhanhFood.App.ViewModels;

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

        // Cấp bộ não cho toàn bộ trang (Rất quan trọng để Load danh sách gợi ý nếu có)
        BindingContext = _viewModel;

        _viewModel.OnLocationsLoaded += DrawMapElements;

        MessagingCenter.Subscribe<object>(this, "LanguageChanged", (sender) =>
        {
            if (_viewModel.Locations != null && _viewModel.Locations.Any())
            {
                DrawMapElements(_viewModel.Locations);
            }
        });
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_viewModel != null)
        {
            await _viewModel.InitializeAsync();
        }
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
                    Address = "Nhấn để xem chi tiết",
                    Location = pinLocation
                };

                // Gắn sự kiện Click vào Pin
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

            // Di chuyển camera về quán đầu tiên
            if (locations.Any())
            {
                var first = locations[0];
                vinhKhanhMap.MoveToRegion(MapSpan.FromCenterAndRadius(
                    new Location(first.Latitude, first.Longitude), Distance.FromKilometers(0.5)));
            }
        });
    }

    // SỰ KIỆN: KHI BẤM VÀO PIN TRÊN BẢN ĐỒ
    private async void OnMapInfoWindowClicked(object sender, PinClickedEventArgs e)
    {
        // 1. QUAN TRỌNG: Ẩn bảng InfoWindow
        e.HideInfoWindow = true;

        if (sender is Pin clickedPin)
        {
            // Lấy data từ ViewModel dựa vào Label
            var locData = _viewModel.Locations.FirstOrDefault(l => l.Name == clickedPin.Label);
            if (locData != null)
            {
                // 2. Ép dữ liệu vào BottomSheet để XAML tự động load Hình ảnh, Tên, Mô tả
                FoodBottomSheet.BindingContext = locData;

                // 3. Hiển thị BottomSheet với hiệu ứng trượt lên cực mượt (Pro UI)
                FoodBottomSheet.IsVisible = true;
                FoodBottomSheet.TranslationY = 300; // Giấu xuống dưới trước
                await FoodBottomSheet.TranslateTo(0, 0, 300, Easing.SinOut); // Trượt lên

                // 4. Nhờ ViewModel đọc âm thanh
                //await _viewModel.PlayPinAudioAsync(locData);
            }
        }
    }

    private async void OnPlayAudioClicked(object sender, EventArgs e)
    {
        await _viewModel.PlayGeneralIntroAsync();
    }

    // SỰ KIỆN: KHI BẤM NÚT TẮT (X) TRÊN BOTTOM SHEET
    private async void OnCloseBottomSheetClicked(object sender, EventArgs e)
    {
        // Tắt âm thanh
        _viewModel.CancelSpeech();

        // Tạo hiệu ứng trượt xuống mượt mà trước khi ẩn
        await FoodBottomSheet.TranslateTo(0, 300, 250, Easing.SinIn);
        FoodBottomSheet.IsVisible = false;
    }

    // SỰ KIỆN: KHI BẤM NÚT "XEM CHI TIẾT" MÀU ĐỎ
    private async void OnViewDetailsClicked(object sender, EventArgs e)
    {
        // Lấy lại dữ liệu của quán đang được chọn
        if (FoodBottomSheet.BindingContext is FoodLocation selectedLocation)
        {

            // Tắt âm thanh nếu nó đang đọc ở trang ngoài
            _viewModel.CancelSpeech();

            // Chuyển sang Trang Chi Tiết và "cầm theo" dữ liệu của quán ốc đó
            await Navigation.PushAsync(new DetailPage(selectedLocation));
        }
    }
}