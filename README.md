# Yume

![Yume logo](Assets/Yume.png)

Yume is a native Windows desktop music player that presents YouTube Music inside a focused interface. Browse the YouTube Music catalog, sign in to your own Google account, and control playback from a native bottom bar. Built with C#, WinUI 3 and Microsoft WebView2.

## Download and run

Download `Yume-1.2.8-Setup-x64.exe` from this repository's Releases page and run it. Setup installs Yume for the current Windows user, adds a Start menu shortcut and offers an optional desktop shortcut. Uninstall through Windows Settings. Updates and uninstall preserve your local settings and browser profile.

Windows 10 version 2004 or later is required. The .NET runtime is bundled. Setup installs Microsoft Edge WebView2 Runtime when missing; this step needs an internet connection. Administrator privileges are not required.

The Windows x64 ZIP is also available for portable use: extract the entire folder and launch `Yume.exe`, keeping all files together. This is an unsigned preview build, so Windows may display a publisher warning. There is no automatic updater.

## Features

### Playback and discovery

- Native play/pause, previous/next, shuffle, repeat, seek and mute controls.
- Volume displayed as a percentage, with optional volume restoration between sessions.
- Search suggestions from YouTube Music. Home remembers the latest search while you browse other sections during the session.
- Queue and Lyrics panels beside the music view, with the current queue track highlighted.
- Click queue entries to play them. Lyrics depend on availability from YouTube Music.
- Home, Explore, Library, Playlists, Liked music, Albums, Artists and Podcasts navigation.
- Library playlists listed beneath Playlists. Drag a song from the music view or Queue onto a playlist to add it when your account has editing permission.

### Appearance and sound

- Dark, Light and Yume themes, with Yume selected by default.
- A shared violet/blue Visual theme background across the native shell and web content. Motion follows playback, freezes on pause and resumes from the same position.
- Optional artwork-derived control colors, compact content, website animations and video visibility.
- A separate graphical six-band equalizer: 30 Hz, 120 Hz, 350 Hz, 1.2 kHz, 3.5 kHz and 9 kHz, each adjustable from −12 to +12 dB.
- Draggable EQ points, keyboard adjustment, enable/disable, reset and presets: Flat, Bass boost, Vocal, Rock, Electronic and Warm. Changes apply and save automatically.
- EQ boosts reserve headroom and can reduce overall loudness. EQ requires a compatible stream and an active browser audio context.
- Access to YouTube Music's own account menu for audio-quality settings; available choices depend on the service and subscription.

### Account, settings and Windows

- Account dialog with the active account and profile photo, plus sign-out from Yume's local browser session.
- First-run welcome dialog and Contact link to the project owner's GitHub profile.
- Automatically chooses Reject all on supported Google/YouTube consent pages in English or Swedish. If no matching button is found, the consent page remains available for a manual choice.
- General, Appearance and Sound settings. Appearance groups Theme, Show in sidebar, Hide and Content controls.
- Optional sidebar sections, start in tray, always on top and Windows startup.
- Closing the main window always minimizes it to the taskbar. Launching Yume again restores the existing instance; use Exit in the tray menu to quit.
- English is the default language. General settings offers English or Match system for YouTube Music, with English fallback for untranslated native app text. Changing the language reloads the website; restart to update browser controls.
- Media keys, Windows media information, saved window placement and persistent local sign-in.

## Shortcuts

| Shortcut | Action |
| --- | --- |
| Ctrl+K / Ctrl+L | Search |
| Space | Play/pause outside editable fields |
| Ctrl+Shift+M | Mute |
| Ctrl+Q | Toggle Queue |
| Ctrl+J | Toggle Lyrics |
| Ctrl+, | Settings |
| Escape | Close the native side panel |
| Up / Down in Equalizer | Adjust the focused band |
| Home in Equalizer | Reset the focused band to 0 dB |

## Privacy and local data

Release archives contain application files only. They do not include a Google account, cookies, browser profile, playlists, listening history, credentials or developer settings.

Preferences and WebView2 browser data are created locally when the app runs, under `Yume` in the current Windows account's local application-data directory. Upgrades from Nocturne reuse its existing local directory to preserve settings and sign-in. Google and WebView2 manage website cookies and caches.

Yume does not write its own account/profile-photo cache, authentication logs or telemetry. Account information, playlist listings and playback metadata are used in memory by the app. Authenticated YouTube Music requests use the current local website session; credentials are not embedded in the application or release. Sign-out clears cookies and DOM storage in Yume's browser profile, without signing other browsers out.

The Contact link intentionally points to the project owner's GitHub profile. No developer Google-account details are distributed.

## Build and test

Use an x64 .NET 8 SDK on Windows with NuGet access:

```powershell
./build.ps1
```

The script builds in a temporary directory and publishes a self-contained Windows x64 application into the adjacent `Yume-portable` folder.

JavaScript regression tests require Node.js:

```powershell
Get-ChildItem Tests/*.cjs | ForEach-Object { node $_.FullName }
dotnet run --project Tests/CoreTests/CoreTests.csproj
```

See [VALIDATION.md](VALIDATION.md) for coverage and remaining manual checks.

### Build the installer

Install Inno Setup 7 and download Microsoft's signed WebView2 Evergreen Bootstrapper. Publish the app first, then run:

```powershell
./Installer/build-installer.ps1 -AppSource ../Yume-portable -Bootstrapper ./MicrosoftEdgeWebview2Setup.exe -Compiler ISCC.exe -OutputDirectory ../release
```

The script verifies the bootstrapper signature and rejects known profile or diagnostic data in the app folder. Installer sources are included in the source release archive.

## Limitations

Yume is an independent client and is not affiliated with Google or YouTube. It uses YouTube Music's website and internal interfaces, which can change. Google may refuse embedded-browser sign-in. The app does not bypass ads, subscription requirements or playback restrictions; hiding video only changes presentation.

Queue content is limited to loaded website entries. Reordering/removing queue entries and advanced library operations remain in the original website. Lyrics do not have timed line highlighting. Playlist writes, fresh-session consent and EQ behavior should be checked with your own account and audio setup. There is no mini player.

## Project structure

- `Views`: native player, account/contact/welcome dialogs, Settings and Equalizer windows.
- `Services`: WebView lifecycle, navigation policy and artwork accents.
- `WebView`: website integration scripts, playback/EQ bridge and theme styles.
- `WindowsIntegration`: tray, startup, window placement and system media controls.
- `Settings`: local application preferences.
- `Assets`: Yume logo and multi-resolution Windows icon.
- `Tests`: synthetic regression fixtures and core C# checks.

## Contact

For bugs, feedback and suggestions please contact me.
