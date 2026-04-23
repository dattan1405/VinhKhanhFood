using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.Communication;
using Microsoft.Maui.Media;
using VinhKhanhFood.App.Models;

namespace VinhKhanhFood.App;

public partial class DetailPage : ContentPage
{
    private readonly FoodLocation _currentLocation;
    private bool _isPlaying = false;
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
            StopAudio();
        }
        else
        {
            await PlayAudio();
        }
    }

    private async Task PlayAudio()
    {
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
                StopAudio();
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

        _isPlaying = false;
        BtnPlayAudio.Text = "▶";
        LblAudioStatus.Text = "Listen to Introduction";
        AudioProgressBar.Progress = 0;
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