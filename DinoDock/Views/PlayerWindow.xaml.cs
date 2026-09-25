using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using DinoDock.Interop;
using DinoDock.Models;
using DinoDock.Services;
using DinoDock.ViewModels;

namespace DinoDock.Views;

/// <summary>
/// The floating player. Hosts exactly one WebView2 control, keeps itself on top when pinned,
/// and remembers where it was.
/// </summary>
public partial class PlayerWindow : Window
{
    private const double ScreenMargin = 24;

    private readonly WebViewService _webViews;
    private WebView2? _browser;
    private bool _isInitializing;
    private bool _isClosed;

    public PlayerWindow(PlayerViewModel viewModel, WebViewService webViews, AppSettings settings)
    {
        InitializeComponent();

        ViewModel = viewModel;
        _webViews = webViews;
        DataContext = viewModel;

        ApplyInitialBounds(settings);

        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        viewModel.InitializationRequested += OnInitializationRequested;

        SourceInitialized += (_, _) =>
        {
            NativeMethods.ApplyModernWindowStyle(this);
            ApplyTopmost();
        };
        Loaded += OnLoaded;
        Deactivated += OnDeactivated;
        StateChanged += (_, _) => WindowChromeHelper.ApplyMaximizedPadding(this, RootBorder);
        PreviewKeyDown += OnPreviewKeyDown;
        Closed += OnClosed;
    }

    public PlayerViewModel ViewModel { get; }

    /// <summary>
    /// Applies the pin state. WPF's Topmost already uses SetWindowPos(HWND_TOPMOST); the explicit
    /// call re-asserts it (e.g. after the window was hidden and shown again).
    /// </summary>
    public void ApplyTopmost()
    {
        Topmost = ViewModel.IsPinned;
        NativeMethods.SetTopmost(this, ViewModel.IsPinned);
    }

    // ----- Browser lifetime -----

    private async void OnLoaded(object sender, RoutedEventArgs e) => await InitializeBrowserAsync();

    private async void OnInitializationRequested(object? sender, EventArgs e) => await InitializeBrowserAsync();

    /// <summary>
    /// Creates the WebView2 control on the shared, persistent environment. Safe to call again
    /// after a failure (e.g. once the runtime has been installed, or after the browser crashed).
    /// </summary>
    private async Task InitializeBrowserAsync()
    {
        if (_isInitializing || _isClosed || ViewModel.IsAttached)
            return;

        if (!WebViewService.IsRuntimeInstalled())
        {
            ViewModel.ShowRuntimeMissing();
            return;
        }

        _isInitializing = true;
        ViewModel.IsLoading = true;

        try
        {
            DisposeBrowser();

            var browser = new WebView2
            {
                // Avoid a white flash before the first page paints.
                DefaultBackgroundColor = System.Drawing.Color.FromArgb(255, 0x14, 0x15, 0x19),
            };
            BrowserHost.Children.Add(browser);
            _browser = browser;

            var environment = await _webViews.GetEnvironmentAsync();
            await browser.EnsureCoreWebView2Async(environment);

            if (_isClosed || !ReferenceEquals(_browser, browser))
                return; // window closed (or browser replaced) while we were waiting

            ViewModel.Attach(browser.CoreWebView2);
        }
        catch (WebView2RuntimeNotFoundException)
        {
            if (!_isClosed)
                ViewModel.ShowRuntimeMissing();
        }
        catch (Exception ex)
        {
            Log.Write("WebView2 initialization failed", ex);
            if (!_isClosed)
                ViewModel.ShowInitializationError(ex.Message);
        }
        finally
        {
            _isInitializing = false;
        }
    }

    private void DisposeBrowser()
    {
        if (_browser is null)
            return;

        ViewModel.Detach();
        BrowserHost.Children.Remove(_browser);
        _browser.Dispose();
        _browser = null;
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _isClosed = true;
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        ViewModel.InitializationRequested -= OnInitializationRequested;
        DisposeBrowser();
    }

    // ----- Window behavior -----

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PlayerViewModel.IsPinned))
            ApplyTopmost();
    }

    private void OnDeactivated(object? sender, EventArgs e)
    {
        // Another app took focus. Re-assert topmost so we stay above it.
        if (ViewModel.IsPinned)
            NativeMethods.SetTopmost(this, true);
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && ViewModel.IsFullScreen)
        {
            ViewModel.ExitFullScreen();
            e.Handled = true;
        }
    }

    /// <summary>Restores the last size/position, or uses the bottom-right corner of the primary monitor.</summary>
    private void ApplyInitialBounds(AppSettings settings)
    {
        var virtualScreen = new Rect(
            SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenTop,
            SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight);

        Width = Math.Clamp(ValidOr(settings.PlayerWidth, AppSettings.DefaultPlayerWidth), MinWidth, Math.Max(MinWidth, virtualScreen.Width));
        Height = Math.Clamp(ValidOr(settings.PlayerHeight, AppSettings.DefaultPlayerHeight), MinHeight, Math.Max(MinHeight, virtualScreen.Height));

        if (settings.PlayerLeft is double left && settings.PlayerTop is double top &&
            IsTitleBarVisible(new Rect(left, top, Width, Height), virtualScreen))
        {
            Left = left;
            Top = top;
        }
        else
        {
            var workArea = SystemParameters.WorkArea; // primary monitor, excluding the taskbar
            Left = workArea.Right - Width - ScreenMargin;
            Top = workArea.Bottom - Height - ScreenMargin;
        }
    }

    private static double ValidOr(double value, double fallback) =>
        double.IsFinite(value) && value > 0 ? value : fallback;

    /// <summary>True if enough of the title bar is on-screen to grab (monitor may have been unplugged).</summary>
    private static bool IsTitleBarVisible(Rect window, Rect virtualScreen)
    {
        if (!double.IsFinite(window.Left) || !double.IsFinite(window.Top))
            return false;

        var titleBar = new Rect(window.Left + 40, window.Top, Math.Max(1, window.Width - 80), 30);
        return virtualScreen.IntersectsWith(titleBar);
    }

    private void OnMinimizeClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
