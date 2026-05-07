using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.Communication;
using Microsoft.Maui.Media;
using VinhKhanhFood.App.Models;

namespace VinhKhanhFood.App;

public partial class DetailPage : ContentPage
{
    private readonly FoodLocation _currentLocation;
    private bool _isPlaying = false;
    private string? _currentRequestId = null;
    private CancellationTokenSource? _cts;

    public DetailPage(FoodLocation location)
    {
        InitializeComponent();
        _currentLocation = location;
        BindingContext = _currentLocation;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        BindingContext = null;
        BindingContext = _currentLocation;
    }

    private async void OnBackButtonClicked(object sender, EventArgs e)
    {
        StopAudio();
        await Task.Delay(100);
        await Navigation.PopAsync();
    }

    private async void OnGetDirectionsClicked(object sender, EventArgs e)
    {
        try
        {
            Location location = new Location(_currentLocation.Latitude, _currentLocation.Longitude);
            MapLaunchOptions options = new MapLaunchOptions { Name = _currentLocation.Name };
            await Map.Default.OpenAsync(location, options);
        }
        catch (Exception)
        {
            await DisplayAlert("Lỗi", "Không thể mở bản đồ chỉ đường.", "OK");
        }
    }

    private async void OnToggleAudioClicked(object sender, EventArgs e)
    {
        if (_isPlaying)
        {
            await StopAudioAsync();
        }
        else
        {
            await PlayAudioAsync();
        }
    }

    private async Task PlayAudioAsync()
    {
        string textToRead = _currentLocation.DisplayDescription;

        if (string.IsNullOrWhiteSpace(textToRead))
        {
            await DisplayAlert("Thông báo", "Chưa có phần giới thiệu cho ngôn ngữ này.", "OK");
            return;
        }

        _isPlaying = true;
        BtnPlayAudio.Text = "■";
        LblAudioStatus.Text = "Joining queue...";

        _cts = new CancellationTokenSource();

        try
        {
            // Step 1: Enqueue with server
            var apiService = new VinhKhanhFood.App.Services.ApiService();
            var deviceId = VinhKhanhFood.App.Services.DeviceIdProvider.GetDeviceId();
            
            var queueResponse = await apiService.PlayAudioWithQueueAsync(
                _currentLocation.Id,
                textToRead,
                deviceId
            );

            if (queueResponse == null)
            {
                await DisplayAlert("Lỗi", "Không thể kết nối tới queue server", "OK");
                StopAudio();
                return;
            }

            _currentRequestId = queueResponse.RequestId;

            if (queueResponse.Status == "queued")
            {
                LblAudioStatus.Text = $"Position in queue: {queueResponse.Position}";
                // Mở SignalR listener để chờ "start" notification
                await WaitForAudioStartAsync();
            }
            else if (queueResponse.Status == "playing")
            {
                await PlayLocalAudioAsync(textToRead);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error: {ex}");
            await DisplayAlert("Lỗi", "Lỗi phát âm thanh: " + ex.Message, "OK");
            StopAudio();
        }
    }

    private async Task WaitForAudioStartAsync()
    {
        // Chờ tối đa 30 giây, hoặc cho đến khi nhận signal "start" từ server
        int waitCount = 0;
        while (_isPlaying && waitCount < 300) // 30 giây x 100ms
        {
            await Task.Delay(100);
            waitCount++;
        }

        // Nếu vượt quá thời gian chờ mà chưa nhận start → timeout
        if (_isPlaying && waitCount >= 300)
        {
            await DisplayAlert("Timeout", "Quá lâu chờ turn, hủy queue", "OK");
            await StopAudioAsync();
        }
    }

    private async Task PlayLocalAudioAsync(string textToRead)
    {
        try
        {
            List<Locale> locales = (await TextToSpeech.Default.GetLocalesAsync()).ToList();

            Locale? selectedLocale = locales.FirstOrDefault(l =>
                l.Language.StartsWith(App.CurrentLanguage, StringComparison.OrdinalIgnoreCase));

            selectedLocale ??= locales.FirstOrDefault(l =>
                l.Language.StartsWith("vi", StringComparison.OrdinalIgnoreCase));

            selectedLocale ??= locales.FirstOrDefault();

            SpeechOptions options = new SpeechOptions
            {
                Volume = 1.0f,
                Locale = selectedLocale
            };

            LblAudioStatus.Text = "Reading Guide...";

            int durationMs = textToRead.Length * 80;
            AnimateProgressBar(durationMs, _cts.Token);

            await TextToSpeech.Default.SpeakAsync(textToRead, options, cancelToken: _cts.Token);
        }
        catch (TaskCanceledException)
        {
            // user stop
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"TTS error: {ex}");
            await DisplayAlert("Lỗi phát âm thanh", ex.Message, "OK");
        }
        finally
        {
            if (_isPlaying)
            {
                await StopAudioAsync();
            }
        }
    }

    private void StopAudio()
    {
        if (!_isPlaying)
        {
            return;
        }

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;

        // Cancel queue nếu có
        if (!string.IsNullOrEmpty(_currentRequestId))
        {
            _ = CancelQueueAsync();
        }

        _isPlaying = false;
        BtnPlayAudio.Text = "▶";
        LblAudioStatus.Text = "Listen to Introduction";
        AudioProgressBar.Progress = 0;
    }

    private async Task StopAudioAsync()
    {
        StopAudio();
        await Task.CompletedTask;
    }

    private async Task CancelQueueAsync()
    {
        try
        {
            var apiService = new VinhKhanhFood.App.Services.ApiService();
            await apiService.CancelAudioAsync(_currentRequestId, _currentLocation.Id);
        }
        catch { }
    }

    private async void AnimateProgressBar(int totalDurationMs, CancellationToken token)
    {
        int delay = 100;
        int elapsed = 0;

        while (elapsed < totalDurationMs && !token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(delay, token);
            }
            catch (TaskCanceledException)
            {
                break;
            }

            elapsed += delay;
            double progress = (double)elapsed / totalDurationMs;
            if (progress < 0)
            MainThread.BeginInvokeOnMainThread(() =>
            {
                AudioProgressBar.Progress = progress;
            });
        }
    }
}