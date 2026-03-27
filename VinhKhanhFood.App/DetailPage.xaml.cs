using Microsoft.Maui.ApplicationModel;
using VinhKhanhFood.App.Models;

namespace VinhKhanhFood.App;

public partial class DetailPage : ContentPage
{
    private readonly FoodLocation _currentLocation;

    // ĐÂY CHÍNH LÀ CÁNH CỬA ĐỂ NHẬN DỮ LIỆU TỪ MAINPAGE NÈ ĐẠT!
    public DetailPage(FoodLocation location)
    {
        InitializeComponent();

        _currentLocation = location;

        // Bí quyết MVVM: Gán dữ liệu vào BindingContext, giao diện sẽ tự lên hình và chữ!
        BindingContext = _currentLocation;
    }

    // Sự kiện khi bấm nút Quay Lại (Mũi tên ←)
    private async void OnBackButtonClicked(object sender, EventArgs e)
    {
        // Rút trang hiện tại ra khỏi ngăn xếp để lùi về trang bản đồ
        await Navigation.PopAsync();
    }

    // Sự kiện khi bấm nút Chỉ Đường (Get Directions)
    private async void OnGetDirectionsClicked(object sender, EventArgs e)
    {
        try
        {
            // Bật Google Maps thật trên điện thoại và chỉ đường tới quán
            var location = new Location(_currentLocation.Latitude, _currentLocation.Longitude);
            var options = new MapLaunchOptions { Name = _currentLocation.Name };

            await Map.Default.OpenAsync(location, options);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", "Không thể mở bản đồ chỉ đường.", "OK");
        }
    }
}