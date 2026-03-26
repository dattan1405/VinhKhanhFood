using VinhKhanhFood.App.ViewModels;
using VinhKhanhFood.App.Models; // Nhớ thêm dòng này để nó hiểu FoodLocation

namespace VinhKhanhFood.App;

public partial class ExplorePage : ContentPage
{
    private readonly ExploreViewModel _viewModel;

    public ExplorePage()
    {
        InitializeComponent();
        _viewModel = new ExploreViewModel();
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadLocationsAsync();
    }

    // Xử lý khi người dùng bấm vào một quán ốc trong danh sách
    private async void OnLocationSelected(object sender, SelectionChangedEventArgs e)
    {
        // Lấy dữ liệu của quán vừa được bấm
        if (e.CurrentSelection.FirstOrDefault() is FoodLocation selectedLocation)
        {
            // Bỏ highlight (bỏ bôi đen) cái thẻ vừa chọn cho đẹp
            ((CollectionView)sender).SelectedItem = null;

            // Tạm thời hiện thông báo. Sau này sẽ đổi thành lệnh chuyển sang Trang Chi Tiết
            await DisplayAlert("Khám Phá", $"Đang mở chi tiết quán: {selectedLocation.Name}", "OK");
        }
    }
}