using VinhKhanhFood.App.Services;

namespace VinhKhanhFood.App
{
    public partial class App : Application
    {
        // 1. Biến riêng tư để lưu trong RAM
        private static string _currentLanguage = "vi";
        private readonly HeartbeatService _heartbeatService = new HeartbeatService();

        // 2. Property công khai để các trang khác gọi tới
        public static string CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                if (_currentLanguage != value)
                {
                    _currentLanguage = value;
                    //  Tự động lưu vào bộ nhớ máy mỗi khi giá trị bị thay đổi
                    Microsoft.Maui.Storage.Preferences.Default.Set("app_lang", value);
                }
            }
        }

        public App()
        {
            InitializeComponent();

            CurrentLanguage = Microsoft.Maui.Storage.Preferences.Default.Get("app_lang", "vi");
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            Window window = new Window(new AppShell());

            window.Created += (_, _) => _heartbeatService.Start();
            window.Resumed += (_, _) => _heartbeatService.Start();

            window.Stopped += (_, _) => _heartbeatService.Stop();
            window.Destroying += (_, _) => _heartbeatService.Stop();

            return window;
        }
    }
}