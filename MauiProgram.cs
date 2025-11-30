using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using Microsoft.Maui.ApplicationModel;
using Microsoft.EntityFrameworkCore;
using ResQLink.Data;
using ResQLink.Services;
using ResQLink.Services.Users;
using ResQLink.Services.Validation;
using ResQLink.Services.ErrorHandling;
using ResQLink.Services.Sync;
using System.IO;

#if WINDOWS
using System.Runtime.Versioning;
using Microsoft.UI.Windowing;
using WinRT.Interop;
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
#if WINDOWS
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("Poppins-Regular.ttf", "PoppinsRegular");
                    fonts.AddFont("Poppins-SemiBold.ttf", "PoppinsSemiBold");
                    fonts.AddFont("Poppins-Bold.ttf", "PoppinsBold");
#else
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("Poppins-Regular.ttf", "PoppinsRegular");
                    fonts.AddFont("Poppins-SemiBold.ttf", "PoppinsSemiBold");
                    fonts.AddFont("Poppins-Bold.ttf", "PoppinsBold");
#endif
                });

            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddSingleton<AuthState>();

#if WINDOWS
            var connectionString =
                @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=Resqlink;Integrated Security=True;Encrypt=True;Trust Server Certificate=True";
            builder.Services.AddDbContextFactory<AppDbContext>(opt =>
            {
                opt.UseSqlServer(connectionString);
#if DEBUG
                opt.EnableSensitiveDataLogging();
                opt.EnableDetailedErrors();
#endif
            });
#else
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "resqlink.db");
            builder.Services.AddDbContextFactory<AppDbContext>(opt =>
            {
                opt.UseSqlite($"Data Source={dbPath}");
#if DEBUG
                opt.EnableSensitiveDataLogging();
                opt.EnableDetailedErrors();
#endif
            });
#endif

            // Register Business Services
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IDisasterService, DisasterService>();
            builder.Services.AddScoped<CategoryService>();
            builder.Services.AddScoped<SupplierService>();
            builder.Services.AddScoped<InventoryService>();
            builder.Services.AddScoped<StockService>();
            builder.Services.AddScoped<ResourceAllocationService>();

            // Register Global Validation & Error Handling Services
            builder.Services.AddScoped<IValidationService, ValidationService>();
            builder.Services.AddScoped<IValidationRules, ValidationRules>();
            builder.Services.AddScoped<IErrorHandlerService, ErrorHandlerService>();

            // Sync services
            builder.Services.AddHttpClient("RemoteApi", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            });
            var syncSettings = new SyncSettings
            {
                RemoteEnabled = true,
                RemoteConnectionString = "Server=db32781.public.databaseasp.net; Database=db32781; User Id=db32781; Password=7Cb#g8_X-Q5n; Encrypt=False; MultipleActiveResultSets=True",
                IntervalMinutes = 5
            };
            builder.Services.AddSingleton(syncSettings);
            builder.Services.AddSingleton<ISyncSettingsStorage, PreferencesSyncSettingsStorage>();
            builder.Services.AddScoped<ISyncService, SyncService>();

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
                                appWindow?.SetPresenter(AppWindowPresenterKind.Overlapped);
                                appWindow?.Resize(new Windows.Graphics.SizeInt32(1000, 700));
                            }
                            catch { }
                        });
                    });
                });
            });
#endif

            var app = builder.Build();

            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
                System.Diagnostics.Debug.WriteLine($"UNHANDLED: {e.ExceptionObject}");
            TaskScheduler.UnobservedTaskException += (_, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"UNOBSERVED: {e.Exception}");
                e.SetObserved();
            };

            // Seed database with roles and admin user
            using (var scope = app.Services.CreateScope())
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
                try
                {
                    var users = scope.ServiceProvider.GetRequiredService<IUserService>();
                    logger.LogInformation("Seeding roles and admin user...");
                   // users.EnsureCreatedAndSeedAdminAsync().GetAwaiter().GetResult();
                    logger.LogInformation("Database seeding completed.");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Seeding failed but app will continue.");
                }

                // Load persisted sync settings and start auto-sync if configured
                try
                {
                    var settings = scope.ServiceProvider.GetRequiredService<SyncSettings>();
                    var storage = scope.ServiceProvider.GetRequiredService<ISyncSettingsStorage>();
                    storage.LoadInto(settings);

                    var sync = scope.ServiceProvider.GetRequiredService<ISyncService>();
                    if (settings.RemoteEnabled && settings.IntervalMinutes > 0)
                    {
                        sync.StartAutoSync(TimeSpan.FromMinutes(settings.IntervalMinutes));
                        logger.LogInformation("Auto sync started every {Interval} minutes.", settings.IntervalMinutes);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to initialize sync settings at startup");
                }
            }

            return app;
        }
    }
}
