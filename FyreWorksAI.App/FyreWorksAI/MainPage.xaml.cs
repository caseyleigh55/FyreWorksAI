using Microsoft.AspNetCore.Components.WebView.Maui;

namespace FyreWorksAI;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        blazorWebView.RootComponents.Clear();
        blazorWebView.RootComponents.Add(new RootComponent
        {
            Selector = "#app",
            ComponentType = typeof(FyreWorksAI.Shared.Routes)
        });
    }
}
