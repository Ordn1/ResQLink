using Microsoft.AspNetCore.Components.WebView.Maui;

namespace ResQLink
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            Bw.RootComponents.Add(new RootComponent
            {
                Selector = "#app",
                ComponentType = typeof(ResQLink.Components.Routes)
            });
        }
    }
}
