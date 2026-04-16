using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace VinhKhanhFood.App
{
    [Activity(
        Theme = "@style/Maui.SplashTheme",
        MainLauncher = true,
        LaunchMode = LaunchMode.SingleTask,
        ConfigurationChanges =
            ConfigChanges.ScreenSize |
            ConfigChanges.Orientation |
            ConfigChanges.UiMode |
            ConfigChanges.ScreenLayout |
            ConfigChanges.SmallestScreenSize |
            ConfigChanges.Density)]
    [IntentFilter(
        new[] { Intent.ActionView },
        Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
        DataScheme = "vinhkhanhfood",
        DataHost = "launch")]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            HandleDeepLink(Intent);
        }

        protected override void OnNewIntent(Intent? intent)
        {
            base.OnNewIntent(intent);
            HandleDeepLink(intent);
        }

        private static void HandleDeepLink(Intent? intent)
        {
            var data = intent?.DataString;
            System.Diagnostics.Debug.WriteLine($"📱 Intent Data: {data}");

            if (!string.IsNullOrEmpty(data) && data.Contains("token=", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var uri = new Uri(data);
                    var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                    string? token = query.Get("token");

                    if (!string.IsNullOrWhiteSpace(token))
                    {
                        Preferences.Default.Set("pending_token", token);
                        System.Diagnostics.Debug.WriteLine($"✅ Token saved: {token}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Parse error: {ex.Message}");
                }
            }
        }
    }
}