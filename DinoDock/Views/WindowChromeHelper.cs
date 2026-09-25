using System.Windows;
using System.Windows.Controls;

namespace DinoDock.Views;

internal static class WindowChromeHelper
{
    /// <summary>
    /// A borderless (WindowChrome) window extends past the screen edges when maximized by the
    /// size of the resize border. Pad the content by that amount so nothing gets cut off.
    /// </summary>
    public static void ApplyMaximizedPadding(Window window, Border root)
    {
        if (window.WindowState == WindowState.Maximized)
        {
            root.Margin = new Thickness(7);
            root.BorderThickness = new Thickness(0);
        }
        else
        {
            root.Margin = new Thickness(0);
            root.BorderThickness = new Thickness(1);
        }
    }
}
