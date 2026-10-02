# Validation

## Version 1.2.6

- Windows x64 self-contained Release build succeeds, including the application icon, compiled XAML and resource index.
- JavaScript regression suites cover playback, queue selection/deduplication, remembered Home search, suggestions, remembered volume, account/playlist parsing, validated drag-and-drop requests, consent rejection and six-band equalizer behavior.
- Synthetic EQ tests cover gain limits, headroom, bypass, media replacement, source reuse and suspended/cross-origin safeguards.
- Consent fixtures cover Swedish/English Reject all, trusted-host restrictions and fallback when the matching button is absent.
- Live read-only integration checks confirmed account/photo availability and library playlist retrieval. No account details were saved as test fixtures.
- Release contents are scanned for personal paths and excluded browser/profile data.

## Remaining manual checks

- Actual playlist insertion using an account-owned test playlist; automated tests simulate write responses and do not alter a personal playlist.
- Listening tests for EQ presets, drag behavior, audio-context activation and different stream types.
- First-run Google consent with a completely new browser profile.
- Extended playback, network interruptions, website reloads, display changes and media-key interactions.

Tests use synthetic data. Releases exclude diagnostic builds, profiles, credentials, personal settings and screenshots.
