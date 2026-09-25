using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using DinoDock.Services;

namespace DinoDock;

/// <summary>
/// Composition root: creates the services, registers hotkeys and the tray icon, and shows the launcher.
/// </summary>
public partial class App : Application
{
    private Mutex? _singleInstanceMutex;
    private SettingsService? _settings;
    private WindowService? _windows;
    private HotkeyService? _hotkeys;
    private TrayService? _tray;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Only one copy at a time: two copies would fight over the hotkeys and tray icon.
        _singleInstanceMutex = new Mutex(initiallyOwned: true, @"Local\DinoDock.SingleInstance", out var isFirstInstance);
        if (!isFirstInstance)
        {
            _singleInstanceMutex.Dispose();
            _singleInstanceMutex = null;
            MessageBox.Show("DinoDock is already running. Look for its icon in the system tray (bottom-right of the taskbar).",
                "DinoDock", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Log.Write("Unobserved task exception", args.Exception);
            args.SetObserved();
        };

        AppPaths.MigrateFromOldName();

        _settings = new SettingsService();
        _settings.Load();

        var webViews = new WebViewService();
        _windows = new WindowService(_settings, webViews);

        try
        {
            _tray = new TrayService(_windows);
        }
        catch (Exception ex)
        {
            Log.Write("Tray icon could not be created", ex); // non-essential
        }

        _hotkeys = new HotkeyService();
        if (_settings.Current.EnableGlobalHotkeys)
        {
            _hotkeys.Register(ModifierKeys.Control | ModifierKeys.Shift, Key.Space, _windows.TogglePlayerVisibility);
            _hotkeys.Register(ModifierKeys.Control | ModifierKeys.Shift, Key.P, _windows.TogglePinned);
        }

        _windows.ShowLauncher();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hotkeys?.Dispose();
        _tray?.Dispose();
        _settings?.Save();

        if (_singleInstanceMutex is not null)
        {
            _singleInstanceMutex.ReleaseMutex();
            _singleInstanceMutex.Dispose();
        }

        base.OnExit(e);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Write("Unhandled UI exception", e.Exception);
        e.Handled = true; // keep running; the error is logged

        MessageBox.Show(
            $"Something went wrong, but DinoDock will keep running.\n\n{e.Exception.Message}\n\nDetails were written to:\n{AppPaths.LogFile}",
            "DinoDock", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}
