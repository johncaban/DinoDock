using System.ComponentModel;
using System.Windows;
using DinoDock.Models;
using DinoDock.ViewModels;
using DinoDock.Views;

namespace DinoDock.Services;

/// <summary>
/// Coordinates the launcher and the (single) player window: which one is visible, reusing the
/// player when another app is picked, pin state, saving the player's position, and app exit.
/// </summary>
public sealed class WindowService
{
    private readonly SettingsService _settings;
    private readonly WebViewService _webViews;

    private LauncherWindow? _launcher;
    private LauncherViewModel? _launcherViewModel;
    private PlayerWindow? _player;
    private bool _isExiting;

    public WindowService(SettingsService settings, WebViewService webViews)
    {
        _settings = settings;
        _webViews = webViews;
    }

    /// <summary>Raised when pin state or player existence changes (used by the tray menu).</summary>
    public event EventHandler? StateChanged;

    public bool IsPinned => _settings.Current.AlwaysOnTop;

    public bool HasPlayer => _player is not null;

    // ----- Launcher -----

    public void ShowLauncher()
    {
        if (_isExiting)
            return;

        if (_launcher is null)
        {
            _launcherViewModel = new LauncherViewModel(AppCatalog.All, OpenApp);
            _launcher = new LauncherWindow(_launcherViewModel);
            _launcher.Closing += OnLauncherClosing;
            _launcher.Closed += OnLauncherClosed;
        }

        _launcherViewModel!.SetLastUsed(_settings.Current.LastAppId);

        _launcher.Show();
        if (_launcher.WindowState == WindowState.Minimized)
            _launcher.WindowState = WindowState.Normal;
        _launcher.Activate();
        _launcher.FocusPreferredCard();
    }

    private void OnLauncherClosing(object? sender, CancelEventArgs e)
    {
        // With a player open, closing the launcher just tucks it away.
        if (!_isExiting && _player is not null)
        {
            e.Cancel = true;
            _launcher?.Hide();
        }
    }

    private void OnLauncherClosed(object? sender, EventArgs e)
    {
        _launcher = null;
        _launcherViewModel = null;

        // Closing the launcher with nothing else open quits the app.
        if (!_isExiting && _player is null)
            ExitApplication();
    }

    // ----- Player -----

    /// <summary>Opens <paramref name="app"/> in the player, creating it or reusing the existing one.</summary>
    public void OpenApp(MiniApp app)
    {
        _settings.Current.LastAppId = app.Id;
        _settings.Save();

        if (_player is null)
        {
            var viewModel = new PlayerViewModel(_settings.Current.AlwaysOnTop);
            viewModel.PropertyChanged += OnPlayerViewModelPropertyChanged;
            viewModel.LauncherRequested += (_, _) => ReturnToLauncher();

            _player = new PlayerWindow(viewModel, _webViews, _settings.Current);
            _player.Closing += (_, _) => SavePlayerBounds();
            _player.Closed += OnPlayerClosed;

            viewModel.Open(app);
            _player.Show();
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            _player.ViewModel.Open(app);
            ShowPlayer(activate: true);
        }

        _launcher?.Hide();
    }

    /// <summary>Global hotkey / tray: hide the player if it's showing, otherwise bring it back.</summary>
    public void TogglePlayerVisibility()
    {
        if (_player is null)
        {
            ShowPlayerOrLauncher();
            return;
        }

        if (_player.IsVisible && _player.WindowState != WindowState.Minimized)
            HidePlayer();
        else
            ShowPlayer(activate: false); // don't steal focus from what the user is working in
    }

    /// <summary>Shows the existing player, reopens the last app, or falls back to the launcher.</summary>
    public void ShowPlayerOrLauncher()
    {
        if (_player is not null)
        {
            ShowPlayer(activate: true);
            return;
        }

        var lastApp = AppCatalog.Find(_settings.Current.LastAppId);
        if (lastApp is not null)
            OpenApp(lastApp);
        else
            ShowLauncher();
    }

    public void TogglePinned() => SetPinned(!IsPinned);

    public void SetPinned(bool pinned)
    {
        if (_player is not null)
        {
            // The view model change flows back through OnPlayerViewModelPropertyChanged.
            _player.ViewModel.IsPinned = pinned;
        }
        else if (_settings.Current.AlwaysOnTop != pinned)
        {
            _settings.Current.AlwaysOnTop = pinned;
            _settings.Save();
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void ShowPlayer(bool activate)
    {
        if (_player is null)
            return;

        _player.ShowActivated = activate;
        _player.Show();
        _player.ShowActivated = true;

        if (_player.WindowState == WindowState.Minimized)
            _player.WindowState = WindowState.Normal;

        if (activate)
            _player.Activate();

        _player.ApplyTopmost();
    }

    private void HidePlayer()
    {
        if (_player is null)
            return;

        SavePlayerBounds();
        _player.ViewModel.PauseMedia();
        _player.Hide();
    }

    private void ReturnToLauncher()
    {
        HidePlayer();
        ShowLauncher();
    }

    private void OnPlayerViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PlayerViewModel.IsPinned) && sender is PlayerViewModel vm)
        {
            _settings.Current.AlwaysOnTop = vm.IsPinned;
            _settings.Save();
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnPlayerClosed(object? sender, EventArgs e)
    {
        if (_player is not null)
            _player.ViewModel.PropertyChanged -= OnPlayerViewModelPropertyChanged;

        _player = null;
        StateChanged?.Invoke(this, EventArgs.Empty);

        if (!_isExiting)
            ShowLauncher();
    }

    private void SavePlayerBounds()
    {
        if (_player is null)
            return;

        var bounds = _player.WindowState == WindowState.Normal
            ? new Rect(_player.Left, _player.Top, _player.ActualWidth, _player.ActualHeight)
            : _player.RestoreBounds;

        if (bounds.IsEmpty || bounds.Width <= 0 || bounds.Height <= 0 || double.IsNaN(bounds.Left))
            return;

        var s = _settings.Current;
        s.PlayerLeft = bounds.Left;
        s.PlayerTop = bounds.Top;
        s.PlayerWidth = bounds.Width;
        s.PlayerHeight = bounds.Height;
        _settings.Save();
    }

    // ----- Exit -----

    public void ExitApplication()
    {
        if (_isExiting)
            return;
        _isExiting = true;

        SavePlayerBounds();
        _player?.Close();   // disposes the WebView2 control
        _launcher?.Close();

        Application.Current.Shutdown();
    }
}
