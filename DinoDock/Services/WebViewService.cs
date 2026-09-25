using System.Diagnostics;
using System.IO;
using Microsoft.Web.WebView2.Core;

namespace DinoDock.Services;

/// <summary>
/// Owns the single shared <see cref="CoreWebView2Environment"/>, backed by a persistent
/// user-data folder so logins and cookies survive restarts.
/// </summary>
public sealed class WebViewService
{
    /// <summary>Microsoft's Evergreen WebView2 Runtime bootstrapper download.</summary>
    public const string RuntimeDownloadUrl = "https://go.microsoft.com/fwlink/p/?LinkId=2124703";

    private Task<CoreWebView2Environment>? _environmentTask;

    /// <summary>Returns true when the Edge WebView2 Runtime is installed.</summary>
    public static bool IsRuntimeInstalled()
    {
        try
        {
            return !string.IsNullOrEmpty(CoreWebView2Environment.GetAvailableBrowserVersionString());
        }
        catch (WebView2RuntimeNotFoundException)
        {
            return false;
        }
        catch (Exception ex)
        {
            Log.Write("Could not query WebView2 runtime version", ex);
            return false;
        }
    }

    /// <summary>
    /// Creates the environment once and hands the same instance to every caller.
    /// If creation failed, the next call tries again. Must be called on the UI thread.
    /// </summary>
    public Task<CoreWebView2Environment> GetEnvironmentAsync()
    {
        if (_environmentTask is null || _environmentTask.IsFaulted || _environmentTask.IsCanceled)
        {
            Directory.CreateDirectory(AppPaths.WebViewData);
            _environmentTask = CoreWebView2Environment.CreateAsync(
                browserExecutableFolder: null,
                userDataFolder: AppPaths.WebViewData);
        }

        return _environmentTask;
    }

    public static void OpenRuntimeDownloadPage()
    {
        try
        {
            Process.Start(new ProcessStartInfo(RuntimeDownloadUrl) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Log.Write("Could not open the WebView2 download page", ex);
        }
    }
}
