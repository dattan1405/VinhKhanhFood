using VinhKhanhFood.App.ViewModels;
using VinhKhanhFood.App.Models;

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
            // Điều hướng đến trang chi tiết, truyền dữ liệu của quán vừa chọn
            await Shell.Current.Navigation.PushAsync(new DetailPage(selectedLocation));
            // Bỏ highlight (bỏ bôi đen) cái thẻ vừa chọn
            ((CollectionView)sender).SelectedItem = null;
        }
    }

    
}