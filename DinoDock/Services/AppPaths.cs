using System.IO;

namespace DinoDock.Services;

/// <summary>Locations under %LOCALAPPDATA%\DinoDock.</summary>
public static class AppPaths
{
    public static string Root { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DinoDock");

    /// <summary>Persistent WebView2 profile (cookies, logins, cache). Reused on every launch.</summary>
    public static string WebViewData { get; } = Path.Combine(Root, "WebView2");

    public static string SettingsFile { get; } = Path.Combine(Root, "settings.json");

    public static string LogFile { get; } = Path.Combine(Root, "error.log");

    /// <summary>
    /// The app used to be called "Mini Player". Move its data folder (logins, settings) over
    /// to the new name the first time DinoDock runs, so nobody has to sign in again.
    /// </summary>
    public static void MigrateFromOldName()
    {
        try
        {
            var old = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MiniPlayer");
            if (Directory.Exists(old) && !Directory.Exists(Root))
                Directory.Move(old, Root);
        }
        catch
        {
            // Old copy still running or folder locked - just start fresh.
        }
    }
}
