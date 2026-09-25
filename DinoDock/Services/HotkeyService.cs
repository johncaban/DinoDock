using System.Windows.Input;
using System.Windows.Interop;
using DinoDock.Interop;

namespace DinoDock.Services;

/// <summary>
/// System-wide hotkeys via RegisterHotKey on a hidden message-only window.
/// Everything registered here is unregistered in <see cref="Dispose"/>.
/// </summary>
public sealed class HotkeyService : IDisposable
{
    private readonly HwndSource _source;
    private readonly Dictionary<int, Action> _handlers = new();
    private int _nextId = 0x4D50; // arbitrary base ("MP")
    private bool _disposed;

    public HotkeyService()
    {
        var parameters = new HwndSourceParameters("DinoDockHotkeys")
        {
            ParentWindow = NativeMethods.HWND_MESSAGE,
            WindowStyle = 0,
            Width = 0,
            Height = 0,
        };
        _source = new HwndSource(parameters);
        _source.AddHook(WndProc);
    }

    /// <summary>Registers Ctrl/Shift + key. Returns false if another app already owns the combination.</summary>
    public bool Register(ModifierKeys modifiers, Key key, Action handler)
    {
        uint mods = NativeMethods.MOD_NOREPEAT;
        if (modifiers.HasFlag(ModifierKeys.Control)) mods |= NativeMethods.MOD_CONTROL;
        if (modifiers.HasFlag(ModifierKeys.Shift)) mods |= NativeMethods.MOD_SHIFT;

        var id = _nextId++;
        var vk = (uint)KeyInterop.VirtualKeyFromKey(key);
        if (!NativeMethods.RegisterHotKey(_source.Handle, id, mods, vk))
        {
            Log.Write($"Hotkey {modifiers}+{key} is already in use by another application; skipping.");
            return false;
        }

        _handlers[id] = handler;
        return true;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY && _handlers.TryGetValue(wParam.ToInt32(), out var handler))
        {
            handled = true;
            try
            {
                handler();
            }
            catch (Exception ex)
            {
                Log.Write("Hotkey handler failed", ex);
            }
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        foreach (var id in _handlers.Keys)
            NativeMethods.UnregisterHotKey(_source.Handle, id);
        _handlers.Clear();

        _source.RemoveHook(WndProc);
        _source.Dispose();
    }
}
