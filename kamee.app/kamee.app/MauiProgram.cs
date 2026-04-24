using System.Reflection;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace kamee.app
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            // Load appsettings.json embedded resource
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream("kamee.app.appsettings.json");
            if (stream != null)
            {
                var config = new ConfigurationBuilder()
                    .AddJsonStream(stream)
                    .Build();
                Constants.SupabaseUrl = config["Supabase:Url"] ?? string.Empty;
                Constants.SupabaseAnonKey = config["Supabase:AnonKey"] ?? string.Empty;
            }

            // Override WebView2 user agent so YouTube doesn't block embeds
#if WINDOWS
            Microsoft.Maui.Handlers.WebViewHandler.Mapper.AppendToMapping("KameeUA", (handler, view) =>
            {
                if (handler.PlatformView is Microsoft.UI.Xaml.Controls.WebView2 wv2)
                    wv2.CoreWebView2Initialized += (s, _) =>
                        s.CoreWebView2.Settings.UserAgent =
                            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36";
            });
#endif

            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            builder.Services.AddSingleton<Services.SupabaseService>();
            builder.Services.AddSingleton<Services.AuthService>();
            builder.Services.AddSingleton<Services.RoomService>();
            builder.Services.AddSingleton<Services.ChatService>();
            builder.Services.AddTransient<Services.SyncService>();

            builder.Services.AddTransient<ViewModels.SplashViewModel>();
            builder.Services.AddTransient<ViewModels.LoginViewModel>();
            builder.Services.AddTransient<ViewModels.HomeViewModel>();
            builder.Services.AddTransient<ViewModels.WatchRoomViewModel>();
            builder.Services.AddTransient<ViewModels.CreateRoomViewModel>();
            builder.Services.AddTransient<ViewModels.ProfileViewModel>();

            builder.Services.AddTransient<Views.Mobile.SplashPage>();
            builder.Services.AddTransient<Views.Mobile.LoginPage>();
            builder.Services.AddTransient<Views.Mobile.HomePage>();
            builder.Services.AddTransient<Views.Mobile.WatchRoomPage>();
            builder.Services.AddTransient<Views.Mobile.CreateRoomPage>();
            builder.Services.AddTransient<Views.Mobile.ProfilePage>();
            builder.Services.AddTransient<Views.Desktop.HomeDesktopPage>();
            builder.Services.AddTransient<Views.Desktop.WatchRoomDesktopPage>();

            return builder.Build();
        }
    }
}
