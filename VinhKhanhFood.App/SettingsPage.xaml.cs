namespace VinhKhanhFood.App;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
        // khi vừa mở trang Settings, nó highlight đúng ngôn ngữ đang dùng
        UpdateLanguageUI(App.CurrentLanguage);

        // Cập nhật text hiển thị ban đầu
        LblCurrentLanguage.Text = App.CurrentLanguage switch
        {
            "vi" => "VN Tiếng Việt",
            "en" => "US English",
            "ko" => "KR Korean",
            "ja" => "JP Japanese",
            "zh" => "CN Chinese",
            _ => "VN Tiếng Việt"
        };
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
        // Lấy langCode trực tiếp từ CommandParameter mà Đạt đã set ở XAML
        var langCode = e.Parameter as string;

        if (!string.IsNullOrEmpty(langCode))
        {
            App.CurrentLanguage = langCode;

            // Cập nhật text hiển thị
            LblCurrentLanguage.Text = langCode switch
            {
                "vi" => "VN Tiếng Việt",
                "en" => "US English",
                "ko" => "KR Korean",
                "ja" => "JP Japanese",
                "zh" => "CN Chinese",
                _ => "VN Tiếng Việt"
            };

            UpdateLanguageUI(langCode);
            MessagingCenter.Send<object>(this, "LanguageChanged");
            OnCloseModalTapped(this, EventArgs.Empty);
        }
    }

    // Hàm phụ để đổi màu các Border cho đẹp
    private void UpdateLanguageUI(string langCode)
    {
        // Kiểm tra xem các Border đã được khởi tạo chưa trước khi đổi màu
        if (OptVietnamese != null)
            OptVietnamese.BackgroundColor = langCode == "vi" ? Color.FromArgb("#33D32F2F") : Colors.Transparent;

        if (OptEnglish != null)
            OptEnglish.BackgroundColor = langCode == "en" ? Color.FromArgb("#33D32F2F") : Colors.Transparent;

        if (OptKorean != null)
            OptKorean.BackgroundColor = langCode == "ko" ? Color.FromArgb("#33D32F2F") : Colors.Transparent;

        if (OptJapanese != null)
            OptJapanese.BackgroundColor = langCode == "ja" ? Color.FromArgb("#33D32F2F") : Colors.Transparent;

        if (OptChinese != null)
            OptChinese.BackgroundColor = langCode == "zh" ? Color.FromArgb("#33D32F2F") : Colors.Transparent;
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