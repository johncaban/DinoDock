using System.Windows;
using Forms = System.Windows.Forms;

namespace DinoDock.Services;

/// <summary>System tray icon with a small menu. Uses WinForms' NotifyIcon, which WPF lacks.</summary>
public sealed class TrayService : IDisposable
{
    private readonly WindowService _windows;
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly Forms.ToolStripMenuItem _showPlayerItem;
    private readonly Forms.ToolStripMenuItem _pinItem;
    private readonly System.Drawing.Icon? _icon;

    public TrayService(WindowService windows)
    {
        _windows = windows;
        _icon = LoadIcon();

        _showPlayerItem = new Forms.ToolStripMenuItem("Show DinoDock", null, (_, _) => _windows.ShowPlayerOrLauncher());
        var launcherItem = new Forms.ToolStripMenuItem("Open Launcher", null, (_, _) => _windows.ShowLauncher());
        _pinItem = new Forms.ToolStripMenuItem("Always on Top", null, (_, _) => _windows.TogglePinned());
        var exitItem = new Forms.ToolStripMenuItem("Exit", null, (_, _) => _windows.ExitApplication());

        var menu = new Forms.ContextMenuStrip();
        menu.Items.AddRange(new Forms.ToolStripItem[]
        {
            _showPlayerItem,
            launcherItem,
            _pinItem,
            new Forms.ToolStripSeparator(),
            exitItem,
        });
        menu.Opening += (_, _) => UpdateMenu();

        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = _icon ?? System.Drawing.SystemIcons.Application,
            Text = "DinoDock",
            ContextMenuStrip = menu,
            Visible = true,
        };
        _notifyIcon.MouseClick += (_, e) =>
        {
            if (e.Button == Forms.MouseButtons.Left)
                _windows.ShowPlayerOrLauncher();
        };

        _windows.StateChanged += (_, _) => UpdateMenu();
        UpdateMenu();
    }

    private void UpdateMenu()
    {
        _pinItem.Checked = _windows.IsPinned;
        _showPlayerItem.Text = _windows.HasPlayer ? "Show DinoDock" : "Open Last App";
    }

    private static System.Drawing.Icon? LoadIcon()
    {
        try
        {
            var resource = Application.GetResourceStream(new Uri("pack://application:,,,/Assets/AppIcon.ico"));
            if (resource is null)
                return null;

            using var stream = resource.Stream;
            return new System.Drawing.Icon(stream, Forms.SystemInformation.SmallIconSize);
        }
        catch (Exception ex)
        {
            Log.Write("Could not load tray icon", ex);
            return null;
        }
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.ContextMenuStrip?.Dispose();
        _notifyIcon.Dispose();
        _icon?.Dispose();
    }
}
