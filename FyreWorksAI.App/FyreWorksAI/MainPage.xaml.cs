using Microsoft.AspNetCore.Components.WebView.Maui;

namespace FyreWorksAI;

/// <summary>
/// Hosts the shared Blazor routes inside the native MAUI content page.
/// </summary>
public partial class MainPage : ContentPage
{
    //******************************//
    //******** Construction ********//
    //******************************//

    /// <summary>
    /// Initializes the native host page and wires the shared Blazor root component.
    /// </summary>
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
