using Microsoft.UI.Xaml;
using System;
using System.IO;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ResQLink.WinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : MauiWinUIApplication
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            ClearEFCoreModelCache();
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        /// <summary>
        /// Clear EF Core model cache to force schema refresh
        /// </summary>
        private void ClearEFCoreModelCache()
        {
            try
            {
                var cacheDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Microsoft", "EntityFrameworkCore", "ModelCache"
                );

                if (Directory.Exists(cacheDir))
                {
                    Directory.Delete(cacheDir, true);
                    System.Diagnostics.Debug.WriteLine("EF Core model cache cleared!");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Could not clear EF Core cache: {ex.Message}");
            }
        }
    }
}
