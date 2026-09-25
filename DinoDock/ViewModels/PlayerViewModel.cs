using Microsoft.Web.WebView2.Core;
using DinoDock.Models;
using DinoDock.Services;

namespace DinoDock.ViewModels;

/// <summary>
/// State and commands for the floating player. The window creates the WebView2 control and
/// hands its <see cref="CoreWebView2"/> to <see cref="Attach"/>; everything browser-related
/// (navigation, title, history, errors) is then handled here.
/// </summary>
public sealed class PlayerViewModel : ObservableObject
{
    private const string PauseMediaScript =
        "document.querySelectorAll('video,audio').forEach(function(m){try{m.pause();}catch(e){}});";

    private const string ExitFullScreenScript =
        "if (document.fullscreenElement) { document.exitFullscreen(); }";

    private CoreWebView2? _core;
    private MiniApp? _currentApp;
    private string _pageTitle = "";
    private bool _isPinned;
    private bool _canGoBack;
    private bool _canGoForward;
    private bool _isLoading;
    private bool _hasError;
    private bool _isRuntimeMissing;
    private string _errorTitle = "";
    private string _errorMessage = "";
    private bool _isFullScreen;
    private Uri? _lastRequestedUri;

    public PlayerViewModel(bool isPinned)
    {
        _isPinned = isPinned;

        BackCommand = new RelayCommand(() => _core?.GoBack(), () => CanGoBack);
        ForwardCommand = new RelayCommand(() => _core?.GoForward(), () => CanGoForward);
        RefreshCommand = new RelayCommand(Refresh, () => _core is not null);
        HomeCommand = new RelayCommand(GoHome, () => _core is not null && CurrentApp is not null);
        TogglePinCommand = new RelayCommand(() => IsPinned = !IsPinned);
        ShowLauncherCommand = new RelayCommand(() => LauncherRequested?.Invoke(this, EventArgs.Empty));
        RetryCommand = new RelayCommand(Retry);
        InstallRuntimeCommand = new RelayCommand(WebViewService.OpenRuntimeDownloadPage);
    }

    /// <summary>Raised when the user wants to go back to the app picker.</summary>
    public event EventHandler? LauncherRequested;

    /// <summary>Raised when the browser must be (re)initialized, e.g. after installing the runtime.</summary>
    public event EventHandler? InitializationRequested;

    public RelayCommand BackCommand { get; }
    public RelayCommand ForwardCommand { get; }
    public RelayCommand RefreshCommand { get; }
    public RelayCommand HomeCommand { get; }
    public RelayCommand TogglePinCommand { get; }
    public RelayCommand ShowLauncherCommand { get; }
    public RelayCommand RetryCommand { get; }
    public RelayCommand InstallRuntimeCommand { get; }

    public MiniApp? CurrentApp
    {
        get => _currentApp;
        private set
        {
            if (SetProperty(ref _currentApp, value))
            {
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(WindowTitle));
                HomeCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>Page title, falling back to the app name while a page loads.</summary>
    public string Title => !string.IsNullOrWhiteSpace(_pageTitle) ? _pageTitle : CurrentApp?.Name ?? "DinoDock";

    /// <summary>Text for the taskbar / Alt+Tab.</summary>
    public string WindowTitle => CurrentApp is null ? "DinoDock" : $"{CurrentApp.Name} - DinoDock";

    public bool IsPinned
    {
        get => _isPinned;
        set
        {
            if (SetProperty(ref _isPinned, value))
                OnPropertyChanged(nameof(PinToolTip));
        }
    }

    public string PinToolTip => IsPinned
        ? "Always on top: On (Ctrl+Shift+P)"
        : "Always on top: Off (Ctrl+Shift+P)";

    public bool CanGoBack
    {
        get => _canGoBack;
        private set
        {
            if (SetProperty(ref _canGoBack, value))
                BackCommand.RaiseCanExecuteChanged();
        }
    }

    public bool CanGoForward
    {
        get => _canGoForward;
        private set
        {
            if (SetProperty(ref _canGoForward, value))
                ForwardCommand.RaiseCanExecuteChanged();
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public bool HasError
    {
        get => _hasError;
        private set
        {
            if (SetProperty(ref _hasError, value))
                OnPropertyChanged(nameof(ShowBrowser));
        }
    }

    /// <summary>
    /// WebView2 is a native child window and always draws on top of WPF content ("airspace"),
    /// so it is collapsed while the error panel is shown.
    /// </summary>
    public bool ShowBrowser => !HasError;

    public bool IsRuntimeMissing
    {
        get => _isRuntimeMissing;
        private set => SetProperty(ref _isRuntimeMissing, value);
    }

    public string ErrorTitle
    {
        get => _errorTitle;
        private set => SetProperty(ref _errorTitle, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    /// <summary>True while the page has an element in HTML fullscreen (e.g. a YouTube video).</summary>
    public bool IsFullScreen
    {
        get => _isFullScreen;
        private set => SetProperty(ref _isFullScreen, value);
    }

    public bool IsAttached => _core is not null;

    /// <summary>
    /// Shows <paramref name="app"/>. If it is already the current app its page (and playback
    /// position) is kept; otherwise the player navigates to the app's home page.
    /// </summary>
    public void Open(MiniApp app)
    {
        var isSameApp = CurrentApp?.Id == app.Id;
        CurrentApp = app;

        if (_core is null)
            return; // Attach() navigates once the browser is ready.

        if (!isSameApp || HasError)
            Navigate(app.HomeUrl);
    }

    public void Attach(CoreWebView2 core)
    {
        Detach();
        _core = core;

        var settings = core.Settings;
        settings.IsStatusBarEnabled = false;
        settings.IsZoomControlEnabled = true;
#if !DEBUG
        settings.AreDevToolsEnabled = false;
#endif

        core.NavigationStarting += OnNavigationStarting;
        core.NavigationCompleted += OnNavigationCompleted;
        core.HistoryChanged += OnHistoryChanged;
        core.DocumentTitleChanged += OnDocumentTitleChanged;
        core.NewWindowRequested += OnNewWindowRequested;
        core.ContainsFullScreenElementChanged += OnFullScreenChanged;
        core.ProcessFailed += OnProcessFailed;

        IsRuntimeMissing = false;
        HasError = false;
        RefreshCommand.RaiseCanExecuteChanged();
        HomeCommand.RaiseCanExecuteChanged();

        if (CurrentApp is not null)
            Navigate(CurrentApp.HomeUrl);
    }

    public void Detach()
    {
        if (_core is null)
            return;

        _core.NavigationStarting -= OnNavigationStarting;
        _core.NavigationCompleted -= OnNavigationCompleted;
        _core.HistoryChanged -= OnHistoryChanged;
        _core.DocumentTitleChanged -= OnDocumentTitleChanged;
        _core.NewWindowRequested -= OnNewWindowRequested;
        _core.ContainsFullScreenElementChanged -= OnFullScreenChanged;
        _core.ProcessFailed -= OnProcessFailed;
        _core = null;

        CanGoBack = false;
        CanGoForward = false;
        IsLoading = false;
        RefreshCommand.RaiseCanExecuteChanged();
        HomeCommand.RaiseCanExecuteChanged();
    }

    /// <summary>Pauses any playing video/audio (used when the player is hidden).</summary>
    public void PauseMedia() => RunScript(PauseMediaScript);

    public void ExitFullScreen() => RunScript(ExitFullScreenScript);

    public void ShowRuntimeMissing()
    {
        IsLoading = false;
        IsRuntimeMissing = true;
        ErrorTitle = "WebView2 Runtime required";
        ErrorMessage = "DinoDock shows websites using the Microsoft Edge WebView2 Runtime, which isn't installed on this PC. " +
                       "Install it (free, from Microsoft), then click Try again.";
        HasError = true;
    }

    public void ShowInitializationError(string details)
    {
        IsLoading = false;
        IsRuntimeMissing = false;
        ErrorTitle = "The browser couldn't start";
        ErrorMessage = $"{details}\n\nTry again, or restart DinoDock.";
        HasError = true;
    }

    private void Navigate(Uri uri)
    {
        if (_core is null)
            return;

        try
        {
            HasError = false;
            _core.Navigate(uri.AbsoluteUri);
        }
        catch (Exception ex)
        {
            Log.Write($"Navigate to {uri} failed", ex);
            ShowNavigationError("This address couldn't be opened.");
        }
    }

    private void Refresh()
    {
        if (_core is null)
            return;

        if (HasError)
            Retry();
        else
            _core.Reload();
    }

    private void GoHome()
    {
        if (CurrentApp is not null)
            Navigate(CurrentApp.HomeUrl);
    }

    private void Retry()
    {
        if (_core is null)
        {
            // Browser never started (e.g. runtime was missing) - ask the window to try again.
            HasError = false;
            InitializationRequested?.Invoke(this, EventArgs.Empty);
            return;
        }

        var target = _lastRequestedUri ?? CurrentApp?.HomeUrl;
        if (target is not null)
            Navigate(target);
    }

    private async void RunScript(string script)
    {
        if (_core is null)
            return;

        try
        {
            await _core.ExecuteScriptAsync(script);
        }
        catch (Exception ex)
        {
            // The page may be mid-navigation or the browser may be closing; nothing to do.
            Log.Write("Script execution failed", ex);
        }
    }

    private void OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        IsLoading = true;
        if (Uri.TryCreate(e.Uri, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            _lastRequestedUri = uri;
    }

    private void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        IsLoading = false;
        UpdateHistory();

        if (e.IsSuccess)
        {
            HasError = false;
            return;
        }

        switch (e.WebErrorStatus)
        {
            // Cancelled because the user (or page) started another navigation - not an error.
            case CoreWebView2WebErrorStatus.OperationCanceled:
            case CoreWebView2WebErrorStatus.ConnectionAborted:
                return;

            // HTTP 4xx/5xx: let the website show its own error page.
            case CoreWebView2WebErrorStatus.Unknown when e.HttpStatusCode >= 400:
                HasError = false;
                return;
        }

        ShowNavigationError(DescribeError(e.WebErrorStatus));
    }

    private void ShowNavigationError(string message)
    {
        IsRuntimeMissing = false;
        ErrorTitle = "Couldn't load this page";
        ErrorMessage = message;
        HasError = true;
    }

    private static string DescribeError(CoreWebView2WebErrorStatus status) => status switch
    {
        CoreWebView2WebErrorStatus.Disconnected or
        CoreWebView2WebErrorStatus.CannotConnect or
        CoreWebView2WebErrorStatus.ConnectionReset or
        CoreWebView2WebErrorStatus.ServerUnreachable => "Check your internet connection and try again.",
        CoreWebView2WebErrorStatus.HostNameNotResolved => "The site's address couldn't be found. Check your internet connection.",
        CoreWebView2WebErrorStatus.Timeout => "The site took too long to respond.",
        CoreWebView2WebErrorStatus.CertificateCommonNameIsIncorrect or
        CoreWebView2WebErrorStatus.CertificateExpired or
        CoreWebView2WebErrorStatus.ClientCertificateContainsErrors or
        CoreWebView2WebErrorStatus.CertificateRevoked or
        CoreWebView2WebErrorStatus.CertificateIsInvalid => "The site's security certificate isn't valid, so the page was blocked.",
        _ => $"Something went wrong while loading the page ({status}).",
    };

    private void OnHistoryChanged(object? sender, object e) => UpdateHistory();

    private void UpdateHistory()
    {
        CanGoBack = _core?.CanGoBack ?? false;
        CanGoForward = _core?.CanGoForward ?? false;
    }

    private void OnDocumentTitleChanged(object? sender, object e)
    {
        _pageTitle = _core?.DocumentTitle ?? "";
        OnPropertyChanged(nameof(Title));
    }

    private void OnNewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
    {
        // Keep everything inside the single player window instead of spawning
        // extra browser windows (which would also open *behind* a pinned player).
        e.Handled = true;
        if (Uri.TryCreate(e.Uri, UriKind.Absolute, out var uri))
            Navigate(uri);
    }

    private void OnFullScreenChanged(object? sender, object e) =>
        IsFullScreen = _core?.ContainsFullScreenElement ?? false;

    private void OnProcessFailed(object? sender, CoreWebView2ProcessFailedEventArgs e)
    {
        Log.Write($"WebView2 process failed: {e.ProcessFailedKind} ({e.Reason})");

        switch (e.ProcessFailedKind)
        {
            case CoreWebView2ProcessFailedKind.BrowserProcessExited:
                // The control can't be reused after this; Try again rebuilds it.
                Detach();
                ShowInitializationError("The browser process stopped unexpectedly.");
                break;

            case CoreWebView2ProcessFailedKind.RenderProcessExited:
            case CoreWebView2ProcessFailedKind.RenderProcessUnresponsive:
                ShowNavigationError("The page stopped responding.");
                break;
        }
    }
}
