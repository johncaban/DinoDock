# DinoDock

A small Windows desktop app that opens **Google**, **Hulu**, or **YouTube** in a compact, always-on-top floating window, so you can watch or browse while you work in another app.

It's a native WPF app (C# / .NET 10) with Microsoft Edge **WebView2** embedded. No Electron, no web server.

---

## Quick start (never used C# before? start here)

1. **Install the .NET 10 SDK** (free, from Microsoft):
   https://dotnet.microsoft.com/download/dotnet/10.0 → under *SDK*, pick **Windows x64 Installer**. Run it and click through.
2. **Unzip** this folder somewhere, e.g. `Documents\DinoDock`.
3. **Double-click `Run.bat`.** The first build downloads the WebView2 package and takes a minute or so; then the launcher opens.

Later launches: run `DinoDock\bin\Release\net10.0-windows\DinoDock.exe` directly (right-click it → *Pin to Start* or *Send to → Desktop* for a shortcut), or just use `Run.bat` again.

### Installing it properly
To run DinoDock from a fixed place (so you can pin it to the taskbar and keep the code elsewhere), open a terminal in this folder and run:
```
dotnet publish DinoDock\DinoDock.csproj -c Release -o "%LOCALAPPDATA%\Programs\DinoDock"
```
Then run `%LOCALAPPDATA%\Programs\DinoDock\DinoDock.exe` and pin it. Rerun the same command after any change.

### Or from a terminal
```powershell
cd path\to\DinoDock
dotnet run --project DinoDock
```

### Or in Visual Studio
Install **Visual Studio 2026 Community** with the **.NET desktop development** workload, open `DinoDock.sln`, and press **F5**.

---

## Prerequisites

| Requirement | Notes |
|---|---|
| Windows 10 (1809+) or Windows 11 | Rounded corners are Windows 11 only. |
| .NET 10 SDK | Needed to build. Anyone just running a built copy needs the .NET 10 Desktop Runtime. |
| Microsoft Edge WebView2 Runtime | Already installed on Windows 11 and most up-to-date Windows 10 PCs. |

### Installing the WebView2 Runtime
If it's missing, the player shows a **"WebView2 Runtime required"** screen with a **Get WebView2 Runtime** button. You can also install it directly: https://developer.microsoft.com/microsoft-edge/webview2/ → *Evergreen Bootstrapper*. After installing, click **Try again**.

---

## Using it

- **Launcher**: pick Google, Hulu or YouTube (mouse, or Tab/arrow keys + Enter). The card you used last is marked *Last used*.
- **Player control bar**: Back · Forward · Refresh · Home (the app's start page) · title · Launcher · **Pin** · Minimize · Close.
  - Drag the bar to move the window; drag any edge to resize (minimum 350×220).
  - **Pin** (highlighted blue when on) keeps the player above every normal window, even when you're typing in VS Code, Discord, etc.
- **Tray icon** (bottom-right of the taskbar): Show DinoDock, Open Launcher, Always on Top, Exit.
- **Shortcuts**

  | Keys | Action | Scope |
  |---|---|---|
  | Ctrl+Shift+Space | Show / hide the player | System-wide |
  | Ctrl+Shift+P | Toggle always-on-top | System-wide |
  | F5 | Refresh | Player focused |
  | Esc | Leave video fullscreen | Player focused |

  Ctrl+Shift+Space is also VS Code's "trigger parameter hints" shortcut; while DinoDock runs it takes priority. To turn the system-wide shortcuts off, set `"EnableGlobalHotkeys": false` in the settings file (below) and restart. If another app already owns a shortcut, DinoDock skips it and logs a note.

- **Closing**: the player's ✕ closes it and returns to the launcher. Closing the launcher while no player is open exits the app. *Exit* in the tray menu always quits.
- Hiding the player (launcher button or Ctrl+Shift+Space) **pauses** the video, so nothing keeps playing out of sight.

### Where things are stored
| Path | What |
|---|---|
| `%LOCALAPPDATA%\DinoDock\WebView2\` | Browser profile: cookies and logins, so you stay signed in. Delete it to sign out of everything. |
| `%LOCALAPPDATA%\DinoDock\settings.json` | Player position/size, always-on-top, last app, hotkey switch. The app never stores passwords. |
| `%LOCALAPPDATA%\DinoDock\error.log` | Errors, if any. |

---

## Architecture

```
DinoDock.sln
Run.bat                       double-click build + run
DinoDock/
  App.xaml(.cs)               startup: single-instance check, services, hotkeys, tray, error handler
  Themes/Theme.xaml           colors, fonts, button + card styles and animations
  Models/
    MiniApp.cs                one website: name, description, URL, icon glyph, accent colors
    AppCatalog.cs             the list of websites shown in the launcher
    AppSettings.cs            persisted preferences
  ViewModels/
    LauncherViewModel.cs      launcher cards + "open" command
    PlayerViewModel.cs        navigation commands, title, pin state, errors; talks to CoreWebView2
    ObservableObject.cs, RelayCommand.cs   small MVVM helpers (no external MVVM library)
  Views/
    LauncherWindow.xaml(.cs)  app picker
    PlayerWindow.xaml(.cs)    floating player; creates/disposes the WebView2 control
    WindowChromeHelper.cs
  Services/
    WindowService.cs          which window is shown, single player reuse, pin, exit
    WebViewService.cs         one shared WebView2 environment on the persistent profile folder
    SettingsService.cs        JSON load/save
    HotkeyService.cs          RegisterHotKey / UnregisterHotKey
    TrayService.cs            NotifyIcon + menu
    AppPaths.cs, Log.cs
  Interop/NativeMethods.cs    SetWindowPos (topmost), hotkeys, DWM corner/dark-mode attributes
```

**How the pieces fit.** `App` builds the services and asks `WindowService` to show the launcher. Clicking a card runs `LauncherViewModel.OpenCommand` → `WindowService.OpenApp`, which either creates the one `PlayerWindow` or tells the existing one to switch sites. `PlayerWindow` creates a `WebView2` control on the shared environment from `WebViewService` and hands its `CoreWebView2` to `PlayerViewModel`, which handles navigation, history, title, fullscreen, and error events.

**Always on top.** WPF's `Topmost` puts the window in the Win32 *topmost* band with `SetWindowPos(HWND_TOPMOST)`. The player also calls `SetWindowPos` directly when it's shown again and whenever it loses focus, so it stays pinned after hiding, restoring, and app switches. Unpinned, it's an ordinary window.

**Custom title bar.** Both windows use `WindowChrome`, so dragging, edge resizing, Aero Snap, and double-click-to-maximize are handled by Windows itself. `AllowsTransparency` is deliberately *not* used: WebView2 is a native child window and won't render inside a layered/transparent WPF window. For the same reason, the error panel is shown by collapsing the browser rather than drawing over it.

**One browser.** Only one `PlayerWindow` and one `WebView2` exist. Going back to the launcher hides the player (keeping its page) instead of destroying it, so picking the same app again resumes where you were. Closing the player disposes the control.

---

## Adding another website

Open `DinoDock/Models/AppCatalog.cs` and add an entry to `All`:

```csharp
new MiniApp
{
    Id = "twitch",                       // lowercase, unique
    Name = "Twitch",
    Description = "Watch streams",
    HomeUrl = new Uri("https://www.twitch.tv/"),
    IconPathData = PlayCircleGlyph,      // any 24x24 SVG path data
    AccentStart = Color.FromRgb(0x91, 0x46, 0xFF),
    AccentEnd = Color.FromRgb(0x5C, 0x2D, 0xB8),
},
```

The launcher builds its cards from this list, so nothing else needs changing. With four or more cards the layout wraps; widen the launcher or reduce the card width in `Theme.xaml` (`AppCardStyle`).

**Custom icons.** A card uses `DinoDock/Assets/Icons/{Id}.png` if it exists (e.g. `youtube.png`), otherwise it draws the built-in glyph from `AppCatalog.cs` on a colored tile. For a new site, drop its logo PNG in that folder and rebuild.

---

## Known limitations

- **Hulu may refuse to play.** Hulu uses DRM (Widevine/PlayReady) and checks which browser it's running in. WebView2 isn't a browser Hulu officially supports, so you might see a "browser not supported" or playback error even if the page loads. DinoDock doesn't try to get around DRM, content protection, or browser checks. If Hulu won't play, use its official app or a supported browser.
- **Google sign-in** can show "This browser or app may not be secure" in embedded browsers. YouTube usually works for watching without signing in. If sign-in is blocked, there's no supported workaround.
- **Fullscreen**: when a site's fullscreen button is used, the video fills the player's browser area (hiding the page around it) and the control bar stays visible so you can still move and pin the window. It doesn't take over the whole monitor, on purpose, since this is a floating player. Esc or the site's own button exits.
- **Links that open new windows** (`target=_blank`, pop-ups) open in the same player instead of extra windows. Sign-in flows that depend on a separate pop-up window may not complete.
- Pinned windows stay above *normal* windows. Other topmost windows (Task Manager with "Always on top", some games in exclusive fullscreen) can still cover it; that's how Windows handles the topmost band.
- Only one player window at a time (by design for v1).

---

## Credits

| Asset | Source | License |
|---|---|---|
| T-rex and Triceratops 3D models (app icon and launcher background) | [Quaternius](https://quaternius.com/packs/animateddinosaurs.html) "Animated Dinosaur Pack", GLB copies from [BryanRalston/livinglab-volcano](https://github.com/BryanRalston/livinglab-volcano/tree/main/assets/dinos) | CC0 1.0 (public domain) |
| Google and YouTube logos (`Assets/Icons/google.png`, `youtube.png`) | Iconify "logos" set (svgporn / Gil Barbara) | CC0 1.0 |
| Hulu logo (`Assets/Icons/hulu.png`) | CoreUI brand icons via Iconify | CC0 1.0 |
| Bungee typeface (used to render `Assets/DinoDockTitle.png`) | [Bungee](https://fonts.google.com/specimen/Bungee) by David Jonathan Ross | SIL Open Font License 1.1 |

The app icon, launcher background and 3D DinoDock logo are original renders made in Blender from the models above (recolored glossy green, posed, lit, with a neon grid floor), with claw marks and glow added afterwards. Google, YouTube and Hulu logos are trademarks of their owners and are used here only to label links to those services.
