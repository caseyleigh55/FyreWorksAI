using Microsoft.UI;
using Microsoft.UI.Windowing;
using WinRT.Interop;

namespace FyreWorksAI.Platforms.Windows;

//******************************//
//****** Window Startup ********//
//******************************//

internal static class WindowsStartupWindowPresenter
{
    //******************************//
    //******* Presentation *********//
    //******************************//

    internal static void ApplyMaximizedWindow(Window window)
    {
        if (window.Handler?.PlatformView is not Microsoft.Maui.MauiWinUIWindow nativeWindow)
        {
            return;
        }

        var windowHandle = WindowNative.GetWindowHandle(nativeWindow);
        var windowId = Win32Interop.GetWindowIdFromWindow(windowHandle);
        var appWindow = AppWindow.GetFromWindowId(windowId);

        try
        {
            if (appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.Maximize();
            }
        }
        catch
        {
            // Fall back to the platform default windowed presentation if maximize fails.
        }
    }
}
