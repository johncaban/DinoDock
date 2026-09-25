namespace DinoDock.Models;

/// <summary>
/// User preferences persisted to %LOCALAPPDATA%\DinoDock\settings.json.
/// No credentials are stored here; website sessions live in the WebView2 profile.
/// </summary>
public sealed class AppSettings
{
    public const double DefaultPlayerWidth = 600;
    public const double DefaultPlayerHeight = 400;

    /// <summary>Last player position in device-independent pixels. Null = use the default bottom-right spot.</summary>
    public double? PlayerLeft { get; set; }

    public double? PlayerTop { get; set; }

    public double PlayerWidth { get; set; } = DefaultPlayerWidth;

    public double PlayerHeight { get; set; } = DefaultPlayerHeight;

    public bool AlwaysOnTop { get; set; } = true;

    public string? LastAppId { get; set; }

    /// <summary>Ctrl+Shift+Space and Ctrl+Shift+P system-wide. Set to false if they clash with another app.</summary>
    public bool EnableGlobalHotkeys { get; set; } = true;
}
