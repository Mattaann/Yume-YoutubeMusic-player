# Validation

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
