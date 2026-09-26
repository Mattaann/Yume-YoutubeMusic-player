# Validation

## Current snapshot

Version 1.1.0 preview builds successfully. JavaScript playback fixtures and C# navigation policy tests pass. The main window launches. Interactive validation of the new mini player, queue, lyrics and settings remains incomplete. The records below describe the MVP 1 baseline.

## Completed

- Windows x64 self-contained Release publication using .NET 8 and WinUI 3.
- Application launch and visual confirmation that YouTube Music loads.
- Native settings dialog, final sidebar styling and Explore navigation verified in the running application.
- Playback metadata, artwork, elapsed time and play state verified during live playback.
- User confirmed audio and the player controls except shuffle/repeat in the initial build; shuffle/repeat were subsequently corrected.
- Inclusion of compiled XAML and the application resource index in the portable distribution.
- JavaScript fixture tests: metadata, play/pause dispatch, bounded volume and seek values, mute, and editable-field shortcut protection.
- JavaScript fixture tests for shuffle dispatch, all three repeat states and blocked seeking during advertisements.
- C# navigation tests: allowed music and authentication origins, rejection of deceptive domains, invalid ports and unsafe external URL schemes.

## Requires real-use verification

- Google sign-in, account switching and session restoration after restarting.
- Actual playback, next/previous, seeking, volume and uninterrupted navigation with a signed-in account.
- Global media keys and Windows media controls alongside other media applications.
- Tray restoration, settings persistence and behavior after loss of connectivity.
- Shuffle/repeat in the revised build and all library destinations with a signed-in account.

The MVP is suitable for local testing. These checks do not establish production readiness. Mini player, lyrics and queue panels remain outside MVP 1.
