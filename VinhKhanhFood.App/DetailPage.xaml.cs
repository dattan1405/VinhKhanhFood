using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.Communication; // Thư viện để gọi điện thoại
using Microsoft.Maui.Media;
using VinhKhanhFood.App.Models;

namespace VinhKhanhFood.App;

public partial class DetailPage : ContentPage
{
    private readonly FoodLocation _currentLocation;
    private bool _isPlaying = false; // Biến cờ theo dõi trạng thái đang đọc
    private CancellationTokenSource _cts; // Bộ đếm để hủy đọc ngang chừng

    public DetailPage(FoodLocation location)
    {
        InitializeComponent();

        _currentLocation = location;

        // Gán dữ liệu để XAML tự động bốc hình ảnh và text lên giao diện
        BindingContext = _currentLocation;
    }

    // Cập nhật lại chữ khi quay lại trang
    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Buộc UI phải tính toán lại DisplayName và DisplayDescription
        // bằng cách reset lại BindingContext
        BindingContext = null;
        BindingContext = _currentLocation;
    }

    // ==========================================
    // CÁC NÚT ĐIỀU HƯỚNG VÀ HÀNH ĐỘNG
    // ==========================================

    private async void OnBackButtonClicked(object sender, EventArgs e)
    {
        // Phải tắt âm thanh trước khi thoát để không bị lỗi tiếng ma đè lên nhau
        StopAudio();
        await Navigation.PopAsync();
    }

    private async void OnGetDirectionsClicked(object sender, EventArgs e)
    {
        try
        {
            // Bật Google Maps thật trên điện thoại
            var location = new Location(_currentLocation.Latitude, _currentLocation.Longitude);
            var options = new MapLaunchOptions { Name = _currentLocation.Name };
            await Map.Default.OpenAsync(location, options);
        }
        catch (Exception)
        {
            await DisplayAlert("Lỗi", "Không thể mở bản đồ chỉ đường.", "OK");
        }
    }

    // Sự kiện cho nút Gọi Điện Store
    private void OnCallStoreClicked(object sender, EventArgs e)
    {
        if (PhoneDialer.Default.IsSupported)
        {
            // Mở app gọi điện của điện thoại và nhập sẵn số
            PhoneDialer.Default.Open("0904567788");
        }
        else
        {
            DisplayAlert("Thông báo", "Máy ảo này không hỗ trợ gọi điện.", "OK");
        }
    }

    // ==========================================
    // LOGIC TRÌNH PHÁT ÂM THANH (TEXT TO SPEECH)
    // ==========================================

    private async void OnToggleAudioClicked(object sender, EventArgs e)
    {
        if (_isPlaying)
        {
            StopAudio(); // Nếu đang đọc thì bấm vào sẽ Tắt
        }
        else
        {
            await PlayAudio(); // Nếu đang tắt thì bấm vào sẽ Đọc
        }
    }

    private async Task PlayAudio()
    {
        // ✅ LẤY ĐÚNG NỘI DUNG DỊCH: Thay vì lấy .Description, hãy lấy .DisplayDescription
        string textToRead = _currentLocation.DisplayDescription;

        if (string.IsNullOrWhiteSpace(textToRead))
        {
            await DisplayAlert("Thông báo", "Chưa có phần giới thiệu cho ngôn ngữ này.", "OK");
            return;
        }

        _isPlaying = true;
        BtnPlayAudio.Text = "■";
        LblAudioStatus.Text = "Reading Guide...";

        _cts = new CancellationTokenSource();

        try
        {
            var locales = await TextToSpeech.Default.GetLocalesAsync();

            // Dựa trên App.CurrentLanguage để chọn giọng đọc tương ứng
            var selectedLocale = locales.FirstOrDefault(l => l.Language.StartsWith(App.CurrentLanguage));

            // Nếu không tìm thấy giọng (ví dụ máy ko cài tiếng Hàn), thì mặc định lấy cái đầu tiên
            if (selectedLocale == null) selectedLocale = locales.FirstOrDefault(l => l.Language.Contains("vi"));

            var options = new SpeechOptions()
            {
                Volume = 1.0f,
                Locale = selectedLocale //  Áp dụng giọng đọc đúng quốc tịch
            };

            // Tính toán ProgressBar dựa trên độ dài văn bản thực tế
            int durationMs = textToRead.Length * 80;
            AnimateProgressBar(durationMs, _cts.Token);

            await TextToSpeech.Default.SpeakAsync(textToRead, options, cancelToken: _cts.Token);
        }
        catch (Exception) { /* Xử lý lỗi */ }
        finally { if (_isPlaying) StopAudio(); }
    }

    private void StopAudio()
    {
        if (!_isPlaying) return;

        // Ra lệnh dừng bộ đọc
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;

        // Trả UI về trạng thái gốc
        _isPlaying = false;
        BtnPlayAudio.Text = "▶"; // Trở lại nút Play
        LblAudioStatus.Text = "Listen to Introduction";
        AudioProgressBar.Progress = 0;
    }

    // Hàm phụ trợ giúp thanh màu đỏ nhích lên từ từ
    private async void AnimateProgressBar(int totalDurationMs, CancellationToken token)
    {
        int delay = 100; // Mỗi 0.1 giây cập nhật 1 lần
        int elapsed = 0;

        while (elapsed < totalDurationMs && !token.IsCancellationRequested)
        {
            await Task.Delay(delay, token);
            elapsed += delay;

            double progress = (double)elapsed / totalDurationMs;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                AudioProgressBar.Progress = progress;
            });
        }
    }
}