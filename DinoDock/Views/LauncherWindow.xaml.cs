using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using DinoDock.Interop;
using DinoDock.ViewModels;

namespace DinoDock.Views;

public partial class LauncherWindow : Window
{
    private readonly LauncherViewModel _viewModel;

    public LauncherWindow(LauncherViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        SourceInitialized += (_, _) => NativeMethods.ApplyModernWindowStyle(this);
        StateChanged += (_, _) => WindowChromeHelper.ApplyMaximizedPadding(this, RootBorder);
    }

    /// <summary>Puts keyboard focus on the last-used card (or the first one) so Enter opens it.</summary>
    public void FocusPreferredCard()
    {
        Dispatcher.InvokeAsync(() =>
        {
            var index = 0;
            for (var i = 0; i < _viewModel.Apps.Count; i++)
            {
                if (_viewModel.Apps[i].IsLastUsed)
                {
                    index = i;
                    break;
                }
            }

            if (CardsList.ItemContainerGenerator.ContainerFromIndex(index) is DependencyObject container &&
                FindChild<Button>(container) is { } button)
            {
                Keyboard.Focus(button);
            }
        }, DispatcherPriority.Loaded);
    }

    private static T? FindChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match)
                return match;
            if (FindChild<T>(child) is { } nested)
                return nested;
        }
        return null;
    }

    private void OnMinimizeClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
