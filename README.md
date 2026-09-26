# Nocturne

Current snapshot: 1.1.0 preview. Includes work in progress on mini player, queue, lyrics, artwork accents and additional settings. The Windows build succeeds, but these additions have not completed interactive validation. The MVP 1 description below records the earlier baseline.

A native Windows music client built with C#, WinUI 3 and Microsoft WebView2. YouTube Music supplies the catalog, recommendations, account management and playback.

## Build

Requires Windows 10 version 2004 or later, an x64 .NET 8 SDK, and access to NuGet. Run `./build.ps1` in PowerShell. The script publishes a self-contained application into the adjacent `Nocturne-portable` directory. Keep that entire directory together and launch `Nocturne.exe`. Microsoft Edge WebView2 Runtime must be installed. No administrator privileges are required by the application.

## MVP 1

- Persistent WebView2 profile; native navigation, search, settings and playback bar.
- Playback metadata, play/pause, previous/next, seek, volume and mute.
- Optional shuffle and repeat commands delegated to the website.
- Dark styling, hidden likes by default, optional video presentation.
- Tray menu with open, playback controls and exit; close-to-tray enabled by default.
- Windows media transport controls and media keyboard integration.
- Loading and retry screens, external links opened in the default browser.

Ctrl+K or Ctrl+L focuses search. Enter searches YouTube Music. Space toggles playback in the website when focus is not inside an editable field. Ctrl+Shift+M toggles mute. Ctrl+, opens settings from the website.

## Data

Preferences and the browser profile are stored in the current Windows account's local application data directory under `Nocturne`. Preferences contain only interface values, window dimensions and volume. Track metadata exists only in memory. There is no application telemetry, account export or authentication logging. Google's website and WebView2 manage cookies and sign-in state. No scripts are injected into authentication pages. The client does not bypass subscription restrictions.

## Scope and limitations

This is an MVP implementation, not a completed or certified music client. Google can refuse sign-in from embedded browsers. No user-agent spoofing, cookie imports or authentication workarounds are included. Website selector changes can affect playback commands or metadata; selectors are centralized in `WebView/Scripts/selectors.js`. Authentication popups navigate within the same WebView, so flows requiring an opener may not work. Account management remains in Google's interface. Restoring Like/Dislike also restores the website's player bar so those controls remain accessible.

Home recommendations and library layouts use the website's own content. The website's header remains available for search and account access. Native navigation uses website links or YouTube Music's internal navigation event. Queue, lyrics, mini player, artwork accents, startup registration, complete theme options and data-clearing UI are deferred to MVP 2. Audio-only stream selection is not implemented; hiding video changes presentation only. Window position restoration is not yet implemented. Media control behavior must be verified with actual playback and the system's other media applications.

The application has no updater. Version information is set in the project file. Distribution builds are unsigned.

## Structure

`Views` contains the native shell. `Services` handles navigation and WebView lifecycle. `WebView` owns the bridge, selectors, event observers and modular stylesheets. `WindowsIntegration` owns the system tray and media transport controls. `Settings` persists non-sensitive preferences.

## References

- [WebView2 in WinUI 3](https://learn.microsoft.com/en-us/windows/apps/develop/ui/controls/webview2)
- [Unpackaged WinUI applications](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/unpackage-winui-app)
- [Desktop media transport controls](https://learn.microsoft.com/en-us/windows/win32/api/systemmediatransportcontrolsinterop/nf-systemmediatransportcontrolsinterop-isystemmediatransportcontrolsinterop-getforwindow)
