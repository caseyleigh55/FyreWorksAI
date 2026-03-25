#if WINDOWS
using FyreWorksAI.Platforms.Windows;
#endif

namespace FyreWorksAI;

/// <summary>
/// Defines the MAUI application shell and root window creation behavior.
/// </summary>
public partial class App : Application
{
    //******************************//
    //******** Construction ********//
    //******************************//

    /// <summary>
    /// Initializes the MAUI application resources.
    /// </summary>
    public App()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Creates the main application window that hosts the operations workspace.
    /// </summary>
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var mainWindow = new Window(new MainPage()) { Title = "FyreWorksAI" };
#if WINDOWS
        mainWindow.Created += OnMainWindowCreated;
#endif
        return mainWindow;
    }

#if WINDOWS
    /// <summary>
    /// Applies the Windows startup presentation once the native host window exists.
    /// </summary>
    private static void OnMainWindowCreated(object? sender, EventArgs e)
    {
        if (sender is not Window window)
        {
            return;
        }

        window.Created -= OnMainWindowCreated;
        WindowsStartupWindowPresenter.ApplyMaximizedWindow(window);
    }
#endif
}
