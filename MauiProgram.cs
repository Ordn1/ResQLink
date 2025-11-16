using Microsoft.Extensions.Logging;
using ResQLink.Services;
using Microsoft.Maui.LifecycleEvents;
using Microsoft.Maui.ApplicationModel; // for MainThread

#if WINDOWS
using Microsoft.UI.Windowing;
using WinRT.Interop;
using Microsoft.UI;
#endif

namespace ResQLink
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("Poppins-Regular.ttf", "PoppinsRegular");
                    fonts.AddFont("Poppins-SemiBold.ttf", "PoppinsSemiBold");
                    fonts.AddFont("Poppins-Bold.ttf", "PoppinsBold");
                });

            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddSingleton<AuthState>();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

#if WINDOWS
            // Set true fullscreen on Windows but run on UI thread and guard exceptions.
            builder.ConfigureLifecycleEvents(events =>
            {
                events.AddWindows(w =>
                {
                    w.OnWindowCreated(window =>
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            try
                            {
                                var hwnd = WindowNative.GetWindowHandle(window);
                                var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
                                var appWindow = AppWindow.GetFromWindowId(windowId);

                                if (appWindow is not null)
                                {
                                    // FullScreen is valid; keep this for true fullscreen.
                                    appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
                                }
                            }
                            catch
                            {
                                // swallow platform-specific exceptions to avoid crashing startup
                            }
                        });
                    });
                });
            });
#endif

            return builder.Build();
        }
    }
}
