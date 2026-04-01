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
        string textToRead = _currentLocation.Description;
        if (string.IsNullOrWhiteSpace(textToRead))
        {
            await DisplayAlert("Thông báo", "Quán này chưa có phần giới thiệu để nghe.", "OK");
            return;
        }

        // 1. Cập nhật giao diện sang trạng thái "Đang phát"
        _isPlaying = true;
        BtnPlayAudio.Text = "■"; // Đổi icon thành nút Stop hình vuông
        LblAudioStatus.Text = "Playing Introduction...";
        AudioProgressBar.Progress = 0;

        _cts = new CancellationTokenSource();

        // 2. Kích hoạt thanh ProgressBar chạy từ từ
        // Mẹo: Cứ 1 ký tự chữ cái tốn khoảng 70 mili-giây để đọc
        int durationMs = textToRead.Length * 70;
        AnimateProgressBar(durationMs, _cts.Token);

        try
        {
            // 3. Tìm giọng đọc Tiếng Việt (nếu máy có cài)
            var locales = await TextToSpeech.Default.GetLocalesAsync();
            var vnLocale = locales.FirstOrDefault(l => l.Language.Contains("vi"));

            var options = new SpeechOptions()
            {
                Volume = 1.0f,
                Locale = vnLocale
            };

            // 4. Bắt đầu đọc (Lệnh này sẽ chạy cho đến khi đọc xong hoặc bị _cts.Cancel)
            await TextToSpeech.Default.SpeakAsync(textToRead, options, cancelToken: _cts.Token);
        }
        catch (TaskCanceledException)
        {
            // Bỏ qua lỗi vặt khi người dùng bấm dừng ngang
        }
        finally
        {
            // 5. Trả giao diện về bình thường khi đọc xong
            if (_isPlaying) StopAudio();
        }
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