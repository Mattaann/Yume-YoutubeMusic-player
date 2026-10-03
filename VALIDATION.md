# Validation

## Version 1.2.10 test build

- A temporary hidden WebView shares the existing environment during sign-in from a signed-out session. It checks YouTube Music's authenticated state before returning the main view to music.
- Checks are bounded, muted and canceled when leaving the sign-in flow or closing the app. The monitor does not log account details or read credential values.
- Live monitor test detected an existing authenticated session. The diagnostic recorded one boolean and is excluded from the test build.
- Release build, core C# tests and all nine JavaScript suites pass. Fresh sign-in with two-step verification still requires manual validation. Switching an already signed-in account uses the normal redirect or Back to music button.

## Version 1.2.9 test build

- Navigation no longer rewrites and cancels authentication return requests to add a language query; the local language preference remains in effect.
- Canceled and superseded navigations no longer show connection failures. Player scripts only initialize on YouTube Music.
- Trusted account intermediates stay inside Yume; recognized post-sign-in landing pages return to music. A Back to music button is available during sign-in.
- Core tests cover sign-in intermediates, return pages, challenge preservation and hostile-origin rejection. All nine JavaScript suites and the Release build pass.
- End-to-end Google sign-in and two-step verification require manual testing; no account was signed out for automated testing.

## Version 1.2.8 test build

- Uses the site's English language code and updates only the language field in the local YouTube PREF cookie, preserving other preferences.
- Live language check confirmed both the document language and YouTube Music configuration are English. The temporary check recorded booleans only and is not included in the test build.
- Release build and all nine JavaScript suites pass. Match system uses the Windows user's preferred language list.

## Version 1.2.7 test build

- Windows x64 Release build, nine JavaScript suites and core C# checks pass.
- Live startup test succeeds. Closing the main window keeps the process running; launching the executable again exits the second process and retains one instance.
- English is selected by default. Installer source defaults to English without reusing a previous setup language; no new installer was built.
- Manual verification remains for account-specific YouTube Music language, Match system and repinning the new executable to the taskbar.

## Version 1.2.6

- Windows x64 self-contained Release build succeeds, including the application icon, compiled XAML and resource index.
- JavaScript regression suites cover playback, queue selection/deduplication, remembered Home search, suggestions, remembered volume, account/playlist parsing, validated drag-and-drop requests, consent rejection and six-band equalizer behavior.
- Synthetic EQ tests cover gain limits, headroom, bypass, media replacement, source reuse and suspended/cross-origin safeguards.
- Consent fixtures cover Swedish/English Reject all, trusted-host restrictions and fallback when the matching button is absent.
- Live read-only integration checks confirmed account/photo availability and library playlist retrieval. No account details were saved as test fixtures.
- Release contents are scanned for personal paths and excluded browser/profile data.

## Remaining manual checks

- Installer: installing on a machine without WebView2, interactive shortcut selection and upgrading a previous installed version.

- Actual playlist insertion using an account-owned test playlist; automated tests simulate write responses and do not alter a personal playlist.
- Listening tests for EQ presets, drag behavior, audio-context activation and different stream types.
- First-run Google consent with a completely new browser profile.
- Extended playback, network interruptions, website reloads, display changes and media-key interactions.

Tests use synthetic data. Releases exclude diagnostic builds, profiles, credentials, personal settings and screenshots.

## Installer 1.2.6

- Inno Setup 7 compilation succeeds with the Yume icon and English/Swedish setup languages.
- Silent per-user installation to an isolated test directory succeeds. All 470 installed application files match the published payload by SHA-256.
- Silent uninstall succeeds and removes the test executable and uninstall registration.
- The test uses an existing WebView2 runtime. The prerequisite bootstrapper has a valid Microsoft signature.
- Setup does not bundle user profiles or settings and does not remove the separate application-data directory.

## Installer 1.2.8

- English is the default setup language, including upgrades.
- Silent installation and uninstall pass; all 470 payload files match by SHA-256.
- Source, application payload and Git history privacy scan reports no findings.


## Installer 1.2.10

- User confirmed successful sign-in and return to music in the test build.
- Installer build, isolated silent installation and uninstall pass. All 470 application files match the published payload by SHA-256.
- Application and source privacy scan reports no findings; temporary session diagnostics are excluded.

