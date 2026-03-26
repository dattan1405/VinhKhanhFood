using System.Collections.ObjectModel;
using VinhKhanhFood.App.Models;
using VinhKhanhFood.App.Services;

namespace VinhKhanhFood.App.ViewModels;

public class ExploreViewModel : BindableObject
{
    private readonly ApiService _apiService = new ApiService();

    // Danh sách này sẽ tự động đổ vào CollectionView ở XAML
    public ObservableCollection<FoodLocation> Locations { get; set; } = new();

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set { _isBusy = value; OnPropertyChanged(); }
    }

    public async Task LoadLocationsAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            var data = await _apiService.GetFoodLocationsAsync();
            Locations.Clear();
            foreach (var item in data)
            {
                Locations.Add(item);
            }
        }
        catch (Exception ex)
        {
            // Có thể dùng DisplayAlert để báo lỗi
        }
        finally
        {
            IsBusy = false;
        }
    }
}