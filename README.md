# Yume

![Yume logo](Assets/Yume.png)

A native Windows music client built with C#, WinUI 3 and Microsoft WebView2. YouTube Music supplies the catalog, recommendations, account management and playback.

## Run and build

Extract the Windows x64 release archive and launch `Yume.exe`. Keep the extracted files together. Microsoft Edge WebView2 Runtime is required. The application does not require administrator privileges.

To build, use Windows 10 version 2004 or later, an x64 .NET 8 SDK and access to NuGet. Run `./build.ps1` in PowerShell. The script publishes a self-contained application into the adjacent `Yume-portable` directory.

## Features

- Persistent Google session in the local WebView2 profile.
- Native navigation, search and playback controls, including seek, volume, shuffle and repeat.
- Queue and Lyrics buttons in the bottom playback bar, between the timeline and volume.
- Queue shows up to 80 loaded website entries and highlights the current track. Select an entry to play it. Opening this panel does not expand or switch the website's queue.
- Lyrics displays the website's available lyrics. Opening it selects the website's lyrics tab. Missing lyrics show an explanatory message and access to the original panel.
- Artwork-derived accent colors for playback controls and active panel buttons, with a neutral fallback.
- Dark and Light themes, compact website content, animation controls, optional video and Like/Dislike presentation.
- Close to tray, start in tray, always on top, optional Windows startup, media keys and Windows media information.
- Saved window size, position, volume and settings.
- A wider lyrics panel reserves space for its scrollbar.
- The active navigation destination and current queue entry are highlighted.
- Starting a track from Home collapses the website player back to browsing; the native bar continues playback.

With Remember volume enabled, the native volume control sets the remembered level. It is saved shortly after adjustment and applied from the start of each music page, including when the website replaces its media element.

Mini player is not included.

## Shortcuts

| Shortcut | Action |
| --- | --- |
| Ctrl+K / Ctrl+L | Search |
| Space | Play/pause outside editable fields |
| Ctrl+Shift+M | Mute |
| Ctrl+Q | Toggle queue |
| Ctrl+J | Toggle lyrics |
| Ctrl+, | Settings |
| Escape | Close the side panel when native controls have focus |

## Data

Preferences and the browser profile are stored under `Yume` in the current account's local application data directory. Preferences contain interface values only. Queue, lyrics, track metadata and decoded artwork remain in memory. No browser profile, credentials or personal data are included in release archives. There is no application telemetry or authentication logging. Google's website and WebView2 manage cookies and sign-in state; no scripts are injected into authentication pages.

Upgrades from Nocturne reuse its existing local data directory to preserve settings and sign-in. Existing startup registration is transferred to Yume when the renamed application launches.

## Validation

Run `node Tests/bridge.test.cjs`, `node Tests/context.test.cjs`, `node Tests/volume.test.cjs`, `node Tests/navigation.test.cjs` and `dotnet run --project Tests/CoreTests/CoreTests.csproj`. See `VALIDATION.md` for coverage and remaining real-use checks.

## Limitations

This is an unsigned preview build with no installer or automatic updater. Google may refuse embedded-browser sign-in. The app does not bypass subscription restrictions or implement an audio-only stream mode. Hiding video changes presentation only.

Queue and lyrics depend on YouTube Music's loaded content and DOM structure. The queue is not a full editable replacement: reorder and removal remain available through the original website. Lyrics availability depends on the track; timed line highlighting is not implemented. Website changes may require updating the centralized selectors. Account management remains on Google's pages. Restoring Like/Dislike also restores the original player bar.

## Structure

`Views` contains the native shell. `Services` handles WebView lifecycle and artwork accents. `WebView` contains the bridge, selectors, observers and stylesheets. `WindowsIntegration` handles the system tray, startup, window placement and media transport controls. `Settings` persists preferences. `Tests` contains deterministic regression checks.
