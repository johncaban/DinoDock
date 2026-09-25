using System.Diagnostics;
using System.IO;

namespace DinoDock.Services;

/// <summary>Tiny append-only error log. Never throws.</summary>
public static class Log
{
    private const long MaxBytes = 1024 * 1024;
    private static readonly object Gate = new();

    public static void Write(string message, Exception? ex = null)
    {
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{(ex is null ? "" : Environment.NewLine + ex)}{Environment.NewLine}";
        Debug.Write(line);

        try
        {
            lock (Gate)
            {
                Directory.CreateDirectory(AppPaths.Root);
                var file = new FileInfo(AppPaths.LogFile);
                if (file.Exists && file.Length > MaxBytes)
                    file.Delete();
                File.AppendAllText(AppPaths.LogFile, line);
            }
        }
        catch
        {
            // Logging must never take the app down.
        }
    }
}
