using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using Microsoft.Maui.ApplicationModel;
using Microsoft.EntityFrameworkCore;
using ResQLink.Data;
using ResQLink.Services;
using ResQLink.Services.Users;

#if WINDOWS
using Microsoft.UI.Windowing;
using WinRT.Interop;
using Microsoft.UI;
#endif

namespace ResQLink
{
    public static class MauiProgram
    {
        // NOTE: Must return MauiApp (not Task<MauiApp>) because App.xaml.cs expects a synchronous CreateMauiApp.
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

            var connectionString =
                "Server=localhost;Database=resqlink;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;";

            builder.Services.AddDbContext<AppDbContext>(opt =>
            {
                opt.UseSqlServer(connectionString);
#if DEBUG
                opt.EnableSensitiveDataLogging();
                opt.EnableDetailedErrors();
#endif
            });

            builder.Services.AddScoped<IUserService, UserService>();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

#if WINDOWS
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
                                appWindow?.SetPresenter(AppWindowPresenterKind.FullScreen);
                            }
                            catch { /* swallow */ }
                        });
                    });
                });
            });
#endif

            var app = builder.Build();

            // Synchronous seeding (avoid making CreateMauiApp async)
            using (var scope = app.Services.CreateScope())
            {
                var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger("Startup");
                try
                {
                    var users = scope.ServiceProvider.GetRequiredService<IUserService>();
                    logger.LogInformation("Seeding admin user...");
                    users.EnsureCreatedAndSeedAdminAsync().GetAwaiter().GetResult();
                    logger.LogInformation("Admin seed complete.");
                }
                catch (Exception ex)
                {
                    logger.LogCritical(ex, "Startup seeding failed.");
                    throw;
                }
            }

            return app;
        }
    }
}
