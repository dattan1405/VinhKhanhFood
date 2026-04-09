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

        // Khi SettingsPage "phát loa", hàm bên dưới sẽ tự động chạy
        MessagingCenter.Subscribe<object>(this, "LanguageChanged", async (sender) =>
        {
            // Bắt ViewModel nạp lại dữ liệu từ API
            // Lúc này Model sẽ tự nhận diện App.CurrentLanguage mới để đổi chữ
            await _viewModel.LoadLocationsAsync();

            System.Diagnostics.Debug.WriteLine(" ExplorePage: Đã nạp lại dữ liệu theo ngôn ngữ mới!");
        });
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadLocationsAsync();
    }

    private async void OnLocationSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is FoodLocation selectedLocation)
        {
            await Shell.Current.Navigation.PushAsync(new DetailPage(selectedLocation));
            ((CollectionView)sender).SelectedItem = null;
        }
    }
}