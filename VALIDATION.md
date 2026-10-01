# Validation

## Version 1.1.7

- Yume is the default theme with a one-time upgrade migration.
- Native AutoSuggestBox uses YouTube Music query suggestions, a 250 ms input debounce, cancellation and stale-response rejection.
- Search text clears on navigation to tracks and other destinations.
- Release build and suggestion, navigation and playback bridge regression tests pass.
- The user confirmed live suggestions and automatic search clearing in the running application.

## Version 1.1.6

- Removed the duplicate website search field while retaining the native search box.
- Search shortcuts inside the WebView now focus the native field and suppress the website shortcut.
- Release build and existing playback bridge regression suite pass.

## Version 1.1.5

- Added a persisted Yume theme with dark violet/blue gradients in the native shell and website.
- Active Queue and Lyrics buttons have a tinted fill and visible border in all themes.
- Windows x64 Release build passes; visual confirmation of the new theme remains pending.

## Version 1.1.4

- Application renamed to Yume, including executable, assembly metadata, window title, tray and settings.
- Supplied logo packaged as a PNG and a Windows icon with 16, 24, 32, 48, 64, 128 and 256 pixel frames.
- Existing profile directory retained for upgrades; startup registration migrates to the renamed executable.
- Windows Release build and all JavaScript and C# regression suites pass.

## Version 1.1.3

- Lyrics panel widened from 310 to 390 pixels with a dedicated scrollbar margin and disabled horizontal scrolling.
- Dark and Light replace Dark and Black; existing Black preferences migrate to Dark.
- Native surfaces, secondary text, dialogs, active controls and injected website styles support Light mode.
- Active navigation and current queue entry have native selection styling. Queue matching can use the current video identifier or metadata when the website has no selected attribute.
- Home playback retains browsing through an event-driven collapse of the website player. Explicit navigation and lyrics opening cancel this behavior.
- Release build and JavaScript regression suites pass, including the Home playback/navigation fixture.
- The user confirmed the final theme, Home behavior and queue highlighting work in the running application.

## Version 1.1.2

- Regression fixtures cover mirrored queue roots, duplicate playlist positions, intentional repeated tracks, hidden entries and playback endpoint dispatch.
- Queue selection targets the playback endpoint or a playable control, never title text.
- Volume fixtures cover initial restore, late website overrides, replacement media elements, player API synchronization and disabling Remember volume.
- Volume preferences are saved after adjustment; document-start restoration replaces the former one-time restoration after media availability.
- Windows x64 Release build passes. The user confirmed the queue and volume fixes work in the running application.

## Version 1.1.1

- Windows x64 self-contained Release build, including compiled XAML and resource index.
- Main window launch and playback metadata confirmed in the running app.
- Queue and lyrics content confirmed by the user during live playback.
- Queue opening subsequently changed to leave the website's panel unchanged; covered by a regression test.
- Queue and Lyrics buttons moved to the bottom playback bar.
- Mini player window, preferences, controls and shortcuts removed.
- JavaScript tests cover bounded seek and volume, advertisements, shuffle/repeat, mute, keyboard focus protection, queue selection, detached rows, message deduplication, track changes, unavailable lyrics, panel closure and queue limits.
- C# tests cover artwork color selection, neutral fallback and allowed artwork/navigation origins.

## Remaining real-use checks

- Long playback sessions, network interruption and website reload recovery.
- Queue variations and synchronized lyrics across different track types and accounts.
- Windows startup after sign-in, multiple displays and media-key interaction with other applications.
- Settings persistence and artwork color changes across a broader range of covers.

The application does not log account data or save track/lyrics fixtures from live sessions. Automated tests use synthetic content.
