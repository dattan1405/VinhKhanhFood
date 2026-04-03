namespace VinhKhanhFood.App;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    // ==========================================
    // LOGIC: AUDIO & LOCALIZATION
    // ==========================================

    // SỬA LỖI: Đảm bảo đúng tên và tham số (object sender, TappedEventArgs e)
    private async void OnLanguageTapped(object sender, TappedEventArgs e)
    {
        LanguageModal.IsVisible = true;
        LanguageModal.Opacity = 0;
        await LanguageModal.FadeTo(1, 200, Easing.SinOut);
    }

    // Khi kéo thanh Slider tốc độ đọc
    private void OnSpeedSliderValueChanged(object sender, ValueChangedEventArgs e)
    {
        double speed = Math.Round(e.NewValue, 1);
        LblSpeedValue.Text = $"{speed:F1}x";
    }

    // Đóng bảng Modal chọn ngôn ngữ
    private async void OnCloseModalTapped(object sender, EventArgs e)
    {
        await LanguageModal.FadeTo(0, 200, Easing.SinIn);
        LanguageModal.IsVisible = false;
    }

    // Khi chọn 1 ngôn ngữ trong danh sách
    private void OnLanguageSelected(object sender, TappedEventArgs e)
    {
        if (sender is Border border && border.GestureRecognizers[0] is TapGestureRecognizer tap && tap.CommandParameter is string selectedLang)
        {
            LblCurrentLanguage.Text = selectedLang;

            if (selectedLang.Contains("VN"))
            {
                App.CurrentLanguage = "vn"; // Cập nhật để Model bốc Name tiếng Việt

                OptVietnamese.BackgroundColor = Color.FromArgb("#33D32F2F");
                OptEnglish.BackgroundColor = Colors.Transparent;
            }
            else
            {
                App.CurrentLanguage = "en"; // Cập nhật để Model bốc Name_EN tiếng Anh

                OptEnglish.BackgroundColor = Color.FromArgb("#33D32F2F");
                OptVietnamese.BackgroundColor = Colors.Transparent;
            }

            // Gọi hàm đóng bảng
            OnCloseModalTapped(this, EventArgs.Empty);
        }
    }

    // ==========================================
    // LOGIC: APP PREFERENCES
    // ==========================================

    private void OnThemeSwitchToggled(object sender, ToggledEventArgs e)
    {
        if (e.Value)
            BackgroundColor = Color.FromArgb("#121212");
        else
            BackgroundColor = Color.FromArgb("#2C2C2C");
    }
}