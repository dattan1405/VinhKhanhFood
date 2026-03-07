namespace VinhKhanhFood.App
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        // Xử lý khi bấm nút Nghe thuyết minh
        private async void OnPlayAudioClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Audio Guide", "Đang khởi động thuyết minh Phố Vĩnh Khánh...", "OK");
        }
    }
}