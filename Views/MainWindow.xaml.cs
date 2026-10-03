using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Yume.Models;
using Yume.Services;
using Yume.Settings;
using Yume.WindowsIntegration;
using Windows.System;
namespace Yume.Views;
public sealed partial class MainWindow : Window
{
    private readonly SettingsService settings = new();
    private readonly WebViewService web;
    private readonly Microsoft.UI.Xaml.Controls.WebView2 Browser;
    private TrayService? tray;
    private MediaIntegrationService? mediaIntegration;
    private bool updating = true;
    private bool exiting;
    private readonly Microsoft.UI.Dispatching.DispatcherQueueTimer volumeSaveTimer;
    private readonly Microsoft.UI.Dispatching.DispatcherQueueTimer searchTimer;
    private bool searchSubmitted;
    private bool searchFocused;
    private bool settingsOpen;
    private SettingsWindow? settingsWindow;
    private readonly List<Microsoft.UI.Xaml.Media.LinearGradientBrush> visualBrushes = new();
    private readonly System.Diagnostics.Stopwatch visualClock = new();
    private Microsoft.UI.Dispatching.DispatcherQueueTimer? visualTimer;
    private bool backgroundSyncPending;
    private string artwork = "";
    private PlaybackState playback = new();
    private string panelMode = "";
    private string navigationPath = "/";
    private readonly ArtworkAccentService accents = new();
    private CancellationTokenSource? accentCancellation;
    private Windows.UI.Color accent = ArtworkAccentService.Neutral;
    private readonly Microsoft.UI.Xaml.Media.SolidColorBrush accentBrush = new(ArtworkAccentService.Neutral);
    public MainWindow()
    {
        InitializeComponent();
        Browser = new Microsoft.UI.Xaml.Controls.WebView2 { DefaultBackgroundColor = Microsoft.UI.Colors.Transparent };
        BrowserHost.Children.Add(Browser);
        Title = "Yume";
        try { StartupService.MigrateLegacyEntry(); } catch { }
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "Yume.ico"));
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(TitleArea);
        AppWindow.TitleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
        AppWindow.TitleBar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;
        AppWindow.TitleBar.ButtonForegroundColor = Microsoft.UI.Colors.LightGray;
        WindowPlacement.Restore(AppWindow, settings.Current.Width, settings.Current.Height, settings.Current.X, settings.Current.Y, 1100, 640);
        ApplyNativeSettings();
        Volume.Value = settings.Current.Volume * 100;
        web = new(Browser, settings);
        web.SignInChanged += active => { ReturnToMusicButton.Visibility = active ? Visibility.Visible : Visibility.Collapsed; if (active) { SidebarPlaylists.Children.Clear(); libraryStatus.Text = "Complete sign-in to see your playlists."; SidebarPlaylists.Children.Add(libraryStatus); } };
        Search.GotFocus += (_, _) => searchFocused = true;
        Search.LostFocus += (_, _) => searchFocused = false;
        searchTimer = DispatcherQueue.CreateTimer();
        searchTimer.Interval = TimeSpan.FromMilliseconds(250);
        searchTimer.IsRepeating = false;
        searchTimer.Tick += async (_, _) =>
        {
            try { if (web.Bridge != null) await web.Bridge.RequestSuggestionsAsync(Search.Text); } catch { Search.ItemsSource = null; }
        };
        volumeSaveTimer = DispatcherQueue.CreateTimer();
        volumeSaveTimer.Interval = TimeSpan.FromMilliseconds(250);
        volumeSaveTimer.IsRepeating = false;
        volumeSaveTimer.Tick += async (_, _) =>
        {
            settings.Save();
            try { await web.UpdateVolumePreferenceAsync(); } catch { }
        };
        web.Loading += (visible, message) =>
        {
            LoadingPanel.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            LoadingText.Text = message;
            var loading = message is "Loading your music..." or "Applying your privacy choice…";
            Spinner.IsActive = loading;
            RetryButton.Visibility = loading ? Visibility.Collapsed : Visibility.Visible;
        };
        web.Ready += () =>
        {
            web.Bridge!.StateChanged += UpdatePlayer;
            web.Bridge.Shortcut += key => { if (key == "settings") _ = ShowSettingsAsync(); else if (key == "search") Search.Focus(FocusState.Keyboard); else if (key == "clear-search") ClearSearch(); else if (key is "queue" or "lyrics") _ = SetPanelAsync(key); };
            web.Bridge.SuggestionsChanged += (query, items) =>
            {
                if (searchSubmitted || !string.Equals(Search.Text.Trim(), query, StringComparison.Ordinal)) return;
                Search.ItemsSource = items;
                Search.IsSuggestionListOpen = items.Length > 0 && searchFocused;
            };
            web.Bridge.ContextChanged += UpdateContext;
            web.Bridge.NavigationChanged += UpdateNavigation;
        };
        Root.SizeChanged += (_, _) => RenderVisualTheme();
        Browser.SizeChanged += (_, _) => RenderVisualTheme();
        web.PageReady += async () =>
        {
            RenderVisualTheme();
            _ = RefreshPlaylistsAsync();
            try { if (web.Bridge != null && panelMode.Length > 0) await web.Bridge.SetPanelAsync(panelMode); } catch { }
        };
        try
        {
            tray = new(WinRT.Interop.WindowNative.GetWindowHandle(this)) { MediaKeys = settings.Current.MediaKeys };
            tray.Command += action => DispatcherQueue.TryEnqueue(() =>
            {
                if (action == "open") RestoreMain();
                else if (action == "exit") { exiting = true; Close(); }
                else _ = CommandAsync(action);
            });
        }
        catch { }
        AppWindow.Closing += (_, args) =>
        {
            if (AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter { State: Microsoft.UI.Windowing.OverlappedPresenterState.Restored })
            {
                settings.Current.Width = AppWindow.Size.Width;
                settings.Current.Height = AppWindow.Size.Height;
                settings.Current.X = AppWindow.Position.X;
                settings.Current.Y = AppWindow.Position.Y;
            }
            settings.Save();
            if (!exiting) { args.Cancel = true; if (AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter) presenter.Minimize(); }
        };
        try
        {
            mediaIntegration = new(WinRT.Interop.WindowNative.GetWindowHandle(this));
            mediaIntegration.Command += action => DispatcherQueue.TryEnqueue(() => _ = CommandAsync(action));
        }
        catch { }
        Closed += (_, _) => { exiting = true; settingsWindow?.Close(); visualTimer?.Stop(); searchTimer.Stop(); volumeSaveTimer.Stop(); settings.Save(); accentCancellation?.Cancel(); accents.Dispose(); mediaIntegration?.Dispose(); tray?.Dispose(); web.Close(); Browser.Close(); };
        AddShortcut(VirtualKey.K, VirtualKeyModifiers.Control, () => Search.Focus(FocusState.Keyboard));
        AddShortcut(VirtualKey.L, VirtualKeyModifiers.Control, () => Search.Focus(FocusState.Keyboard));
        AddShortcut(VirtualKey.M, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift, () => _ = CommandAsync("mute"));
        AddShortcut(VirtualKey.Q, VirtualKeyModifiers.Control, () => _ = SetPanelAsync("queue"));
        AddShortcut(VirtualKey.J, VirtualKeyModifiers.Control, () => _ = SetPanelAsync("lyrics"));
        AddShortcut((VirtualKey)188, VirtualKeyModifiers.Control, () => _ = ShowSettingsAsync());
        AddShortcut(VirtualKey.Escape, VirtualKeyModifiers.None, () => { if (!settingsOpen) _ = SetPanelAsync(""); });
        Root.KeyDown += (_, e) =>
        {
            if (e.Key == VirtualKey.Space && e.OriginalSource is not TextBox && e.OriginalSource is not PasswordBox && e.OriginalSource is not Button && e.OriginalSource is not Slider && e.OriginalSource is not Microsoft.UI.Xaml.Controls.WebView2)
            {
                _ = CommandAsync("toggle");
                e.Handled = true;
            }
        };
        Root.Loaded += async (_, _) => { await web.InitializeAsync(); await ShowWelcomeAsync(); if (settings.Current.StartMinimized && tray != null) AppWindow.Hide(); };
        updating = false;
    }
    private void ReturnToMusic_Click(object sender, RoutedEventArgs e) => web.ReturnToMusic();
    private void AddShortcut(VirtualKey key, VirtualKeyModifiers modifiers, Action action)
    {
        var shortcut = new KeyboardAccelerator { Key = key, Modifiers = modifiers };
        shortcut.Invoked += (_, e) => { action(); e.Handled = true; };
        Root.KeyboardAccelerators.Add(shortcut);
    }
    private async Task CommandAsync(string command, double value = 0)
    {
        try { if (web.Bridge != null) await web.Bridge.CommandAsync(command, value); } catch { }
    }
    private void UpdatePlayer(PlaybackState state)
    {
        playback = state;
        UpdateVisualPlayback();
        updating = true;
        SongTitle.Text = state.Title;
        Artist.Text = state.Artist;
        PlayButton.Content = state.Playing ? "\uE769" : "\uE768";
        PlayButton.IsEnabled = state.Available;
        ShuffleButton.IsEnabled = state.Available && state.CanShuffle;
        RepeatButton.IsEnabled = state.Available && state.CanRepeat;
        ShuffleButton.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(state.ShuffleActive ? AccentTextColor : InactiveColor);
        RepeatButton.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(state.RepeatMode > 0 ? AccentTextColor : InactiveColor);
        RepeatButton.Content = state.RepeatMode == 2 ? "\uE8ED" : "\uE8EE";
        ToolTipService.SetToolTip(RepeatButton, state.RepeatMode switch { 0 => "Repeat: off", 1 => "Repeat: queue", 2 => "Repeat: current song", _ => "Cycle repeat mode" });
        Progress.Maximum = Math.Max(1, state.Duration);
        Progress.Value = Math.Clamp(state.Position, 0, Progress.Maximum);
        Progress.IsEnabled = state.Available && state.CanSeek;
        Elapsed.Text = FormatTime(state.Position);
        Remaining.Text = "−" + FormatTime(Math.Max(0, state.Duration - state.Position));
        if (state.Available) Volume.Value = Math.Round(Math.Clamp(state.Volume, 0, 1) * 100);
        if (artwork != state.Artwork)
        {
            artwork = state.Artwork;
            var uri = Yume.Utilities.ArtworkPolicy.GetUri(artwork);
            Artwork.Source = uri == null ? null : new BitmapImage(uri);
            _ = RefreshAccentAsync();
        }
        updating = false;
        try { mediaIntegration?.Update(state, settings.Current.WindowsMediaIntegration); } catch { }
    }
    private static string FormatTime(double value) { var time = TimeSpan.FromSeconds(Math.Clamp(value, 0, 604800)); return time.TotalHours >= 1 ? time.ToString(@"h\:mm\:ss") : time.ToString(@"m\:ss"); }
    private void Navigate_Click(object sender, RoutedEventArgs e)
    {
        ClearSearch();
        var path = (string)((Button)sender).Tag;
        UpdateNavigation(path);
        web.Navigate(path);
    }
    private void UpdateNavigation(string path)
    {
        navigationPath = path;
        foreach (var button in NavigationItems.Children.OfType<Button>().Concat(SidebarPlaylists.Children.OfType<Button>().Where(b => b.Tag is string)))
        {
            var selected = (string)button.Tag == path;
            button.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(selected ? Windows.UI.Color.FromArgb(45, accent.R, accent.G, accent.B) : Microsoft.UI.Colors.Transparent);
            button.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(selected ? AccentTextColor : InactiveColor);
            button.FontWeight = selected || NavigationItems.Children.Contains(button) ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Normal;
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetItemStatus(button, selected ? "Current page" : "");
        }
    }
    private void Back_Click(object sender, RoutedEventArgs e) { if (Browser.CoreWebView2?.CanGoBack == true) Browser.CoreWebView2.GoBack(); }
    private void Forward_Click(object sender, RoutedEventArgs e) { if (Browser.CoreWebView2?.CanGoForward == true) Browser.CoreWebView2.GoForward(); }
    private void Account_Click(object sender, RoutedEventArgs e) => _ = ShowAccountAsync();
    private void Control_Click(object sender, RoutedEventArgs e) => _ = CommandAsync((string)((Button)sender).Tag);
    private void Retry_Click(object sender, RoutedEventArgs e) => web.Retry();
    private void Search_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput) return;
        searchSubmitted = false;
        searchTimer.Stop();
        Search.ItemsSource = null;
        Search.IsSuggestionListOpen = false;
        if (!string.IsNullOrWhiteSpace(sender.Text)) searchTimer.Start();
        else _ = CancelSuggestionsAsync();
    }
    private void Search_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        var query = (args.ChosenSuggestion as string ?? args.QueryText).Trim();
        if (query.Length == 0) return;
        searchSubmitted = true;
        searchTimer.Stop();
        Search.ItemsSource = null;
        Search.IsSuggestionListOpen = false;
        _ = CancelSuggestionsAsync();
        web.Navigate("/search?q=" + Uri.EscapeDataString(query));
    }
    private async Task CancelSuggestionsAsync()
    {
        try { if (web.Bridge != null) await web.Bridge.RequestSuggestionsAsync(""); } catch { }
    }
    private void ClearSearch()
    {
        searchTimer.Stop();
        Search.Text = "";
        Search.ItemsSource = null;
        Search.IsSuggestionListOpen = false;
        searchSubmitted = false;
        _ = CancelSuggestionsAsync();
    }
    private void Volume_Changed(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (updating) return;
        settings.Current.Volume = e.NewValue / 100;
        volumeSaveTimer.Stop();
        volumeSaveTimer.Start();
        _ = CommandAsync("volume", e.NewValue / 100);
    }
    private void Progress_Changed(object sender, RangeBaseValueChangedEventArgs e) { if (!updating) _ = CommandAsync("seek", e.NewValue); }
    private void Settings_Click(object sender, RoutedEventArgs e) => _ = ShowSettingsAsync();
    private Windows.UI.Color InactiveColor => settings.Current.Theme == "Light" ? Windows.UI.Color.FromArgb(255, 45, 50, 56) : Microsoft.UI.Colors.LightGray;
    private Windows.UI.Color AccentTextColor => settings.Current.Theme == "Light" ? Windows.UI.Color.FromArgb(255, (byte)(accent.R * 0.42), (byte)(accent.G * 0.42), (byte)(accent.B * 0.42)) : accent;
    private void ApplyNativeSettings()
    {
        RenderVisualTheme();
        SidebarPlaylists.Visibility = settings.Current.ShowSidebarPlaylists ? Visibility.Visible : Visibility.Collapsed;
        LikedMusicButton.Visibility = settings.Current.ShowLikedMusic ? Visibility.Visible : Visibility.Collapsed;
        AlbumsButton.Visibility = settings.Current.ShowAlbums ? Visibility.Visible : Visibility.Collapsed;
        ArtistsButton.Visibility = settings.Current.ShowArtists ? Visibility.Visible : Visibility.Collapsed;
        PodcastsButton.Visibility = settings.Current.ShowPodcasts ? Visibility.Visible : Visibility.Collapsed;
        LibraryDivider.Visibility = settings.Current.ShowLikedMusic || settings.Current.ShowAlbums || settings.Current.ShowArtists || settings.Current.ShowPodcasts ? Visibility.Visible : Visibility.Collapsed;
        visualBrushes.Clear();
        var light = settings.Current.Theme == "Light";
        Root.RequestedTheme = light ? ElementTheme.Light : ElementTheme.Dark;
        AppWindow.TitleBar.ButtonForegroundColor = light ? Microsoft.UI.Colors.Black : Microsoft.UI.Colors.LightGray;
        var background = new Microsoft.UI.Xaml.Media.SolidColorBrush(light ? Windows.UI.Color.FromArgb(255, 247, 248, 250) : Windows.UI.Color.FromArgb(255, 17, 18, 19));
        var surface = new Microsoft.UI.Xaml.Media.SolidColorBrush(light ? Microsoft.UI.Colors.White : Windows.UI.Color.FromArgb(255, 25, 27, 29));
        Root.Background = LoadingPanel.Background = background;
        PlayerSurface.Background = ContextPanel.Background = surface;
        TitleArea.Background = SidebarSurface.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(light ? Windows.UI.Color.FromArgb(255, 237, 240, 243) : Windows.UI.Color.FromArgb(255, 12, 13, 14));
        if (settings.Current.Theme == "Yume")
        {
            Root.Background = CreateYumeBackground(13, 14, 23);
            var transparent = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
            LoadingPanel.Background = SidebarSurface.Background = ContextPanel.Background = PlayerSurface.Background = TitleArea.Background = transparent;
        }
        if (AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter) presenter.IsAlwaysOnTop = settings.Current.AlwaysOnTop;
        ApplyAccent();
        UpdateVisualPlayback();
    }
    private Microsoft.UI.Xaml.Media.LinearGradientBrush CreateYumeBackground(byte r, byte g, byte b)
    {
        var brush = new Microsoft.UI.Xaml.Media.LinearGradientBrush { StartPoint = new Windows.Foundation.Point(0, 0), EndPoint = new Windows.Foundation.Point(1, 0) };
        brush.GradientStops.Add(new() { Color = Windows.UI.Color.FromArgb(255, r, g, b), Offset = 0 });
        brush.GradientStops.Add(new() { Color = Windows.UI.Color.FromArgb(255, 24, 20, 39), Offset = 0.35 });
        brush.GradientStops.Add(new() { Color = Windows.UI.Color.FromArgb(255, 43, 28, 68), Offset = 0.65 });
        brush.GradientStops.Add(new() { Color = Windows.UI.Color.FromArgb(255, 25, 43, 72), Offset = 1 });
        visualBrushes.Add(brush);
        return brush;
    }
    private void UpdateVisualPlayback()
    {
        var enabled = settings.Current.Theme == "Yume" && settings.Current.VisualTheme && playback.Playing;
        if (enabled) visualClock.Start(); else visualClock.Stop();
        if (visualTimer == null)
        {
            visualTimer = DispatcherQueue.CreateTimer();
            visualTimer.Interval = TimeSpan.FromMilliseconds(100);
            visualTimer.Tick += (_, _) => RenderVisualTheme();
        }
        if (enabled && !visualTimer.IsRunning) visualTimer.Start(); else if (!enabled) visualTimer.Stop();
        RenderVisualTheme();
    }
    private void RenderVisualTheme()
    {
        var phase = settings.Current.VisualTheme ? visualClock.Elapsed.TotalSeconds * Math.PI / 24 : 0;
        foreach (var brush in visualBrushes)
        {
            brush.GradientStops[1].Offset = 0.35 + 0.15 * Math.Sin(phase);
            brush.GradientStops[2].Offset = 0.65 + 0.15 * Math.Sin(phase);
        }
        _ = SyncWebBackgroundAsync(phase);
    }
    private async Task SyncWebBackgroundAsync(double phase)
    {
        if (backgroundSyncPending || web?.Bridge == null || settings.Current.Theme != "Yume" || Root.ActualWidth <= 0 || Browser.ActualWidth <= 0) return;
        backgroundSyncPending = true;
        try
        {
            var origin = Browser.TransformToVisual(Root).TransformPoint(new Windows.Foundation.Point());
            await web.Bridge.SetBackgroundAsync(Root.ActualWidth, Root.ActualHeight, origin.X, origin.Y, 35 + 15 * Math.Sin(phase), 65 + 15 * Math.Sin(phase));
        }
        catch { }
        finally { backgroundSyncPending = false; }
    }
    private void StylePanelButton(Button button, bool selected)
    {
        var color = settings.Current.Theme == "Yume" ? Windows.UI.Color.FromArgb(255, 169, 151, 245) : AccentTextColor;
        button.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(selected ? Windows.UI.Color.FromArgb(35, color.R, color.G, color.B) : Microsoft.UI.Colors.Transparent);
        button.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(selected ? Windows.UI.Color.FromArgb(125, color.R, color.G, color.B) : Microsoft.UI.Colors.Transparent);
        button.BorderThickness = new Thickness(1);
        button.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(selected ? color : InactiveColor);
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetItemStatus(button, selected ? "Panel open" : "Panel closed");
    }
    private void ApplyAccent()
    {
        accentBrush.Color = accent;
        var brush = accentBrush;
        var textBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(AccentTextColor);
        PlayButton.Background = brush;
        foreach (var button in new[] { PreviousButton, NextButton })
        {
            button.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(settings.Current.Theme == "Light" ? (byte)32 : (byte)48, accent.R, accent.G, accent.B));
            button.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(90, accent.R, accent.G, accent.B));
            button.BorderThickness = new Thickness(1);
            button.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(InactiveColor);
        }
        foreach (var slider in new[] { Progress, Volume })
        {
            slider.Resources["SliderTrackValueFill"] = brush;
            slider.Resources["SliderTrackValueFillPointerOver"] = brush;
            slider.Resources["SliderTrackValueFillPressed"] = brush;
        }
        ShuffleButton.Foreground = playback.ShuffleActive ? textBrush : new Microsoft.UI.Xaml.Media.SolidColorBrush(InactiveColor);
        RepeatButton.Foreground = playback.RepeatMode > 0 ? textBrush : new Microsoft.UI.Xaml.Media.SolidColorBrush(InactiveColor);
        StylePanelButton(QueueButton, panelMode == "queue");
        StylePanelButton(LyricsButton, panelMode == "lyrics");
        UpdateNavigation(navigationPath);
    }
    private async Task RefreshAccentAsync()
    {
        accentCancellation?.Cancel();
        accentCancellation?.Dispose();
        accentCancellation = new();
        var token = accentCancellation.Token;
        var color = settings.Current.ArtworkAccent ? await accents.ExtractAsync(artwork, token) : ArtworkAccentService.Neutral;
        if (token.IsCancellationRequested || exiting) return;
        accent = color;
        PlayButton.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(color);
        ApplyAccent();
    }
    public void RestoreMain() { AppWindow.Show(); if (AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter) presenter.Restore(); Activate(); }
    private void Queue_Click(object sender, RoutedEventArgs e) => _ = SetPanelAsync("queue");
    private void Lyrics_Click(object sender, RoutedEventArgs e) => _ = SetPanelAsync("lyrics");
    private void ClosePanel_Click(object sender, RoutedEventArgs e) => _ = SetPanelAsync("");
    private async Task SetPanelAsync(string mode)
    {
        panelMode = panelMode == mode ? "" : mode;
        ContextPanel.Visibility = panelMode.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
        BrowserHost.Margin = new Thickness(0, 0, panelMode.Length == 0 ? 0 : ContextPanel.Width, 0);
        ContextTitle.Text = panelMode == "lyrics" ? "Lyrics" : "Queue";
        QueueList.Visibility = panelMode == "queue" ? Visibility.Visible : Visibility.Collapsed;
        LyricsScroll.Visibility = panelMode == "lyrics" ? Visibility.Visible : Visibility.Collapsed;
        QueueList.ItemsSource = null; LyricsText.Text = ""; ContextMessage.Text = "Loading…";
        ContextMessage.Visibility = Visibility.Visible;
        ApplyAccent();
        try
        {
            if (web.Bridge != null && NavigationPolicy.IsMusic(Browser.CoreWebView2.Source)) await web.Bridge.SetPanelAsync(panelMode);
            else ContextMessage.Text = "Open YouTube Music and choose a song to see this content.";
        }
        catch { ContextMessage.Text = "Open YouTube Music to view this content."; }
    }
    private void UpdateContext(ContextState context)
    {
        if (context.Panel != panelMode) return;
        QueueList.ItemsSource = context.Items;
        QueueList.SelectedItem = context.Items.FirstOrDefault(item => item.Selected);
        LyricsText.Text = context.Lyrics;
        ContextMessage.Text = context.Message;
        ContextMessage.Visibility = string.IsNullOrEmpty(context.Message) ? Visibility.Collapsed : Visibility.Visible;
    }
    private async void Queue_ItemClick(object sender, ItemClickEventArgs e)
    {
        try { if (e.ClickedItem is QueueItem item && web.Bridge != null) await web.Bridge.SelectQueueAsync(item.Key); } catch { }
    }
    private async void WebsitePanel_Click(object sender, RoutedEventArgs e)
    {
        var mode = panelMode;
        await SetPanelAsync("");
        try { if (web.Bridge != null) await web.Bridge.OpenWebsitePanelAsync(mode); } catch { }
    }
    private Task ShowSettingsAsync()
    {
        if (settingsWindow != null) { settingsWindow.Activate(); return Task.CompletedTask; }
        settingsOpen = true;
        settingsWindow = new SettingsWindow(settings, tray != null, async () =>
        {
            ApplyNativeSettings();
            _ = RefreshPlaylistsAsync();
            _ = RefreshAccentAsync();
            if (tray != null) tray.MediaKeys = settings.Current.MediaKeys;
            try { mediaIntegration?.Update(playback, settings.Current.WindowsMediaIntegration); } catch { }
            if (web.Bridge != null) { await web.Bridge.ApplyStylesAsync(settings.Current); await web.Bridge.ApplyEqualizerAsync(settings.Current); }
            await web.UpdateVolumePreferenceAsync();
            await web.ApplyLanguageAsync();
        }, async () =>
        {
            RestoreMain();
            await CommandAsync("account");
        }, async () => web.Bridge == null ? "Start playback to use the equalizer." : await web.Bridge.ReadAsync<string>("equalizerStatus") ?? "Equalizer status unavailable.", async () => { if (web.Bridge != null) await web.Bridge.ApplyEqualizerAsync(settings.Current); });
        settingsWindow.Closed += (_, _) => { settingsWindow = null; settingsOpen = false; };
        settingsWindow.Activate();
        return Task.CompletedTask;
    }
}



