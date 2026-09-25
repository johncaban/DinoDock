# DinoDock
🦖 🦖 🦖 🦖 🦖 🦖 🦖 

**Your floating web companion.**

DinoDock is a dinosaur-themed mini player for Windows. Pick **Google**, **YouTube**, or **Hulu** and it opens in a small window that stays on top of everything else, so you can watch or browse while you work in other apps.

<img width="523" height="412" alt="launcher" src="https://github.com/user-attachments/assets/6b67e03c-cd98-4ca9-b0fd-25000eeb0259" />
<img width="1873" height="1010" alt="usecase" src="https://github.com/user-attachments/assets/a92625de-ef03-48fd-80e9-6aec85d3f2ef" />



## Features

- Launcher with a 3D Y2K-style dinosaur theme
- Small floating player (resizable, draggable, remembers where you left it)
- **Always on top**: pin it and it stays above VS Code, Discord, your browser, anything
- Back, forward, refresh, and home buttons
- Stays signed in to your accounts between launches
- System tray icon and keyboard shortcuts

## Install

1. Go to the **[Releases](../../releases)** page and download the latest `DinoDock.zip`.
2. Unzip it somewhere permanent, e.g. `C:\Program Files\DinoDock` or `%LOCALAPPDATA%\Programs\DinoDock`.
3. Double-click **`DinoDock.exe`**.
4. *(Optional)* Right-click DinoDock in the taskbar → **Pin to taskbar**.

**Requirements:** Windows 10 or 11. DinoDock uses the Microsoft Edge WebView2 Runtime, which is already on almost every Windows PC. If it's missing, DinoDock shows a button to install it.

## How to use

| Action | How |
|---|---|
| Open a site | Click Google, YouTube, or Hulu in the launcher |
| Move the player | Drag the top bar |
| Resize | Drag any edge |
| Keep on top | Click the 📌 pin button (green = on) |
| Back to the launcher | Click the grid button in the top bar |
| Show / hide the player | **Ctrl + Shift + Space** (works from any app) |
| Toggle always-on-top | **Ctrl + Shift + P** (works from any app) |
| Quit completely | Right-click the tray icon → **Exit** |

## Built with

| | |
|---|---|
| **Language** | C# |
| **Platform** | .NET 10 |
| **UI framework** | WPF (Windows Presentation Foundation) with XAML |
| **Embedded browser** | Microsoft Edge WebView2 |
| **Architecture** | MVVM (Model-View-ViewModel) |
| **Windows integration** | Win32 API (always-on-top, global hotkeys), Windows Forms (tray icon) |
| **3D art** | Blender (dinosaur renders, app icon, logo) |

## Known limitations

- **Hulu** may refuse to play videos in embedded browsers because of its copy protection (DRM). DinoDock doesn't try to get around this.
- **Google sign-in** may be blocked inside embedded browsers. YouTube works fine without signing in.

## Credits

- T-rex and Triceratops 3D models: [Quaternius](https://quaternius.com/packs/animateddinosaurs.html), CC0 (public domain)
- Google & YouTube logos: Iconify "logos" set, CC0. Hulu logo: CoreUI brand icons, CC0
- Logo font: [Bungee](https://fonts.google.com/specimen/Bungee) by David Jonathan Ross, SIL Open Font License

Google, YouTube, and Hulu are trademarks of their respective owners. DinoDock isn't affiliated with any of them.
