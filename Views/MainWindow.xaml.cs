using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Nocturne.Models;
using Nocturne.Services;
using Nocturne.Settings;
using Nocturne.WindowsIntegration;
using Windows.System;
namespace Nocturne.Views;
public sealed partial class MainWindow : Window
{
    private readonly SettingsService settings = new();
    private readonly WebViewService web;
    private readonly Microsoft.UI.Xaml.Controls.WebView2 Browser;
    private TrayService? tray;
    private MediaIntegrationService? mediaIntegration;
    private bool updating = true;
    private bool exiting;
    private bool volumeRestored;
    private bool settingsOpen;
    private string artwork = "";
    private MiniPlayer? mini;
    private PlaybackState playback = new();
    private string panelMode = "";
    private readonly ArtworkAccentService accents = new();
    private CancellationTokenSource? accentCancellation;
    private Windows.UI.Color accent = ArtworkAccentService.Neutral;
    public MainWindow()
    {
        InitializeComponent();
        Browser = new Microsoft.UI.Xaml.Controls.WebView2();
        BrowserHost.Children.Add(Browser);
        Title = "Nocturne";
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "Nocturne.ico"));
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(TitleArea);
        AppWindow.TitleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
        AppWindow.TitleBar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;
        AppWindow.TitleBar.ButtonForegroundColor = Microsoft.UI.Colors.LightGray;
        WindowPlacement.Restore(AppWindow, settings.Current.Width, settings.Current.Height, settings.Current.X, settings.Current.Y, 1100, 640);
        ApplyNativeSettings();
        Volume.Value = settings.Current.Volume;
        web = new(Browser, settings);
        web.Loading += (visible, message) =>
        {
            LoadingPanel.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            LoadingText.Text = message;
            var loading = message == "Loading your music...";
            Spinner.IsActive = loading;
            RetryButton.Visibility = loading ? Visibility.Collapsed : Visibility.Visible;
        };
        web.Ready += () =>
        {
            web.Bridge!.StateChanged += UpdatePlayer;
            web.Bridge.Shortcut += key => { if (key == "settings") _ = ShowSettingsAsync(); else if (key == "mini") ShowMini(); else if (key is "queue" or "lyrics") _ = SetPanelAsync(key); };
            web.Bridge.ContextChanged += UpdateContext;
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
        catch { settings.Current.MinimizeToTray = false; }
        AppWindow.Closing += (_, args) =>
        {
            settings.Current.Width = AppWindow.Size.Width;
            settings.Current.Height = AppWindow.Size.Height;
            settings.Current.X = AppWindow.Position.X;
            settings.Current.Y = AppWindow.Position.Y;
            settings.Save();
            if (!exiting && settings.Current.MinimizeToTray && tray != null) { args.Cancel = true; AppWindow.Hide(); }
        };
        try
        {
            mediaIntegration = new(WinRT.Interop.WindowNative.GetWindowHandle(this));
            mediaIntegration.Command += action => DispatcherQueue.TryEnqueue(() => _ = CommandAsync(action));
        }
        catch { }
        Closed += (_, _) => { exiting = true; mini?.Close(); accentCancellation?.Cancel(); accents.Dispose(); mediaIntegration?.Dispose(); tray?.Dispose(); Browser.Close(); };
        AddShortcut(VirtualKey.K, VirtualKeyModifiers.Control, () => Search.Focus(FocusState.Keyboard));
        AddShortcut(VirtualKey.L, VirtualKeyModifiers.Control, () => Search.Focus(FocusState.Keyboard));
        AddShortcut(VirtualKey.M, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift, () => _ = CommandAsync("mute"));
        AddShortcut(VirtualKey.M, VirtualKeyModifiers.Control, ShowMini);
        AddShortcut(VirtualKey.Q, VirtualKeyModifiers.Control, () => _ = SetPanelAsync("queue"));
        AddShortcut(VirtualKey.J, VirtualKeyModifiers.Control, () => _ = SetPanelAsync("lyrics"));
        Root.KeyDown += (_, e) =>
        {
            if (e.Key == VirtualKey.Space && e.OriginalSource is not TextBox && e.OriginalSource is not PasswordBox && e.OriginalSource is not Button && e.OriginalSource is not Slider && e.OriginalSource is not Microsoft.UI.Xaml.Controls.WebView2)
            {
                _ = CommandAsync("toggle");
                e.Handled = true;
            }
        };
        Root.Loaded += async (_, _) => { await web.InitializeAsync(); if (settings.Current.StartMinimized && tray != null) AppWindow.Hide(); };
        updating = false;
    }
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
        updating = true;
        SongTitle.Text = state.Title;
        Artist.Text = state.Artist;
        PlayButton.Content = state.Playing ? "\uE769" : "\uE768";
        PlayButton.IsEnabled = state.Available;
        ShuffleButton.IsEnabled = state.Available && state.CanShuffle;
        RepeatButton.IsEnabled = state.Available && state.CanRepeat;
        ShuffleButton.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(state.ShuffleActive ? Microsoft.UI.Colors.Aquamarine : Microsoft.UI.Colors.LightGray);
        RepeatButton.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(state.RepeatMode > 0 ? Microsoft.UI.Colors.Aquamarine : Microsoft.UI.Colors.LightGray);
        RepeatButton.Content = state.RepeatMode == 2 ? "\uE8ED" : "\uE8EE";
        ToolTipService.SetToolTip(RepeatButton, state.RepeatMode switch { 0 => "Repeat: off", 1 => "Repeat: queue", 2 => "Repeat: current song", _ => "Cycle repeat mode" });
        Progress.Maximum = Math.Max(1, state.Duration);
        Progress.Value = Math.Clamp(state.Position, 0, Progress.Maximum);
        Progress.IsEnabled = state.Available && state.CanSeek;
        Elapsed.Text = FormatTime(state.Position);
        Remaining.Text = "−" + FormatTime(Math.Max(0, state.Duration - state.Position));
        Volume.Value = Math.Clamp(state.Volume, 0, 1);
        if (artwork != state.Artwork)
        {
            artwork = state.Artwork;
            Artwork.Source = Uri.TryCreate(artwork, UriKind.Absolute, out var uri) && uri.Scheme == "https" && (uri.Host.EndsWith(".googleusercontent.com", StringComparison.Ordinal) || uri.Host.EndsWith(".ytimg.com", StringComparison.Ordinal) || uri.Host.EndsWith(".ggpht.com", StringComparison.Ordinal)) ? new BitmapImage(uri) : null;
            _ = RefreshAccentAsync();
        }
        updating = false;
        mini?.Update(state, accent);
        try { mediaIntegration?.Update(state, settings.Current.WindowsMediaIntegration); } catch { }
        if (state.Available && !volumeRestored)
        {
            volumeRestored = true;
            if (settings.Current.RememberVolume) _ = CommandAsync("volume", settings.Current.Volume);
        }
    }
    private static string FormatTime(double value) { var time = TimeSpan.FromSeconds(Math.Clamp(value, 0, 604800)); return time.TotalHours >= 1 ? time.ToString(@"h\:mm\:ss") : time.ToString(@"m\:ss"); }
    private void Navigate_Click(object sender, RoutedEventArgs e) => web.Navigate((string)((Button)sender).Tag);
    private void Back_Click(object sender, RoutedEventArgs e) { if (Browser.CoreWebView2?.CanGoBack == true) Browser.CoreWebView2.GoBack(); }
    private void Forward_Click(object sender, RoutedEventArgs e) { if (Browser.CoreWebView2?.CanGoForward == true) Browser.CoreWebView2.GoForward(); }
    private void Account_Click(object sender, RoutedEventArgs e) => _ = CommandAsync("account");
    private void Control_Click(object sender, RoutedEventArgs e) => _ = CommandAsync((string)((Button)sender).Tag);
    private void Retry_Click(object sender, RoutedEventArgs e) => web.Retry();
    private void Search_KeyDown(object sender, KeyRoutedEventArgs e) { if (e.Key == VirtualKey.Enter && !string.IsNullOrWhiteSpace(Search.Text)) { web.Navigate("/search?q=" + Uri.EscapeDataString(Search.Text.Trim())); e.Handled = true; } }
    private void Volume_Changed(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (updating) return;
        settings.Current.Volume = e.NewValue;
        _ = CommandAsync("volume", e.NewValue);
    }
    private void Progress_Changed(object sender, RangeBaseValueChangedEventArgs e) { if (!updating) _ = CommandAsync("seek", e.NewValue); }
    private void Settings_Click(object sender, RoutedEventArgs e) => _ = ShowSettingsAsync();
    private void ApplyNativeSettings()
    {
        Root.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(settings.Current.Theme == "Black" ? Microsoft.UI.Colors.Black : Windows.UI.Color.FromArgb(255, 17, 18, 19));
        if (AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter) presenter.IsAlwaysOnTop = settings.Current.AlwaysOnTop;
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
        mini?.Update(playback, color);
    }
    private void Mini_Click(object sender, RoutedEventArgs e) => ShowMini();
    private void ShowMini()
    {
        if (mini == null)
        {
            mini = new(settings);
            mini.Command += (command, value) => _ = CommandAsync(command, value);
            mini.RestoreRequested += RestoreMain;
            mini.Closed += (_, _) => { mini = null; if (!exiting) { AppWindow.Show(); Activate(); } };
        }
        mini.Update(playback, accent);
        mini.Activate();
        AppWindow.Hide();
    }
    private void RestoreMain() { mini?.Close(); AppWindow.Show(); Activate(); }
    private void Queue_Click(object sender, RoutedEventArgs e) => _ = SetPanelAsync("queue");
    private void Lyrics_Click(object sender, RoutedEventArgs e) => _ = SetPanelAsync("lyrics");
    private void ClosePanel_Click(object sender, RoutedEventArgs e) => _ = SetPanelAsync("");
    private async Task SetPanelAsync(string mode)
    {
        panelMode = panelMode == mode ? "" : mode;
        ContextPanel.Visibility = panelMode.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
        BrowserHost.Margin = new Thickness(0, 0, panelMode.Length == 0 ? 0 : 310, 0);
        ContextTitle.Text = panelMode == "lyrics" ? "Lyrics" : "Queue";
        QueueList.Visibility = panelMode == "queue" ? Visibility.Visible : Visibility.Collapsed;
        LyricsScroll.Visibility = panelMode == "lyrics" ? Visibility.Visible : Visibility.Collapsed;
        QueueList.ItemsSource = null; LyricsText.Text = ""; ContextMessage.Text = "Loading…";
        try { if (web.Bridge != null) await web.Bridge.SetPanelAsync(panelMode); } catch { ContextMessage.Text = "Open YouTube Music to view this content."; }
    }
    private void UpdateContext(ContextState context)
    {
        if (context.Panel != panelMode) return;
        QueueList.ItemsSource = context.Items;
        LyricsText.Text = context.Lyrics;
        ContextMessage.Text = context.Message;
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
    private async Task ShowSettingsAsync()
    {
        if (settingsOpen) return;
        settingsOpen = true;
        try
        {
            var panel = new StackPanel { Spacing = 18 };
            var likes = new ToggleSwitch { Header = "Hide Like / Dislike", IsOn = settings.Current.HideLikes };
            var video = new ToggleSwitch { Header = "Show video content", IsOn = settings.Current.ShowVideo };
            var compact = new ToggleSwitch { Header = "Compact content", IsOn = settings.Current.Compact };
            var minimize = new ToggleSwitch { Header = "Close to system tray", IsOn = settings.Current.MinimizeToTray, IsEnabled = tray != null };
            var volume = new ToggleSwitch { Header = "Remember volume", IsOn = settings.Current.RememberVolume };
            var media = new ToggleSwitch { Header = "Media key support", IsOn = settings.Current.MediaKeys };
            foreach (var item in new[] { likes, video, compact, minimize, volume, media }) panel.Children.Add(item);
            var theme = new ComboBox { Header = "Theme", ItemsSource = new[] { "Dark", "Black" }, SelectedItem = settings.Current.Theme, HorizontalAlignment = HorizontalAlignment.Stretch };
            panel.Children.Add(theme);
            var accentToggle = new ToggleSwitch { Header = "Use artwork accent colors", IsOn = settings.Current.ArtworkAccent };
            var animations = new ToggleSwitch { Header = "Website animations", IsOn = settings.Current.Animations };
            var systemMedia = new ToggleSwitch { Header = "Windows media information", IsOn = settings.Current.WindowsMediaIntegration };
            var topmost = new ToggleSwitch { Header = "Always on top", IsOn = settings.Current.AlwaysOnTop };
            var startMinimized = new ToggleSwitch { Header = "Start in system tray", IsOn = settings.Current.StartMinimized, IsEnabled = tray != null };
            var startupEnabled = StartupService.IsEnabled();
            var startup = new ToggleSwitch { Header = "Start with Windows", IsOn = startupEnabled };
            foreach (var item in new[] { accentToggle, animations, systemMedia, topmost, startMinimized, startup }) panel.Children.Add(item);
            var version = typeof(App).Assembly.GetName().Version;
            panel.Children.Add(new TextBlock { Text = $"Nocturne {version?.Major}.{version?.Minor}.{version?.Build} · MVP 2\nCtrl+M: mini player · Ctrl+Q: queue · Ctrl+J: lyrics\nSign-in stays within Google's pages.\nWebsite data remains in the local WebView2 profile.", TextWrapping = TextWrapping.Wrap, FontSize = 12 });
            var dialog = new ContentDialog { XamlRoot = Root.XamlRoot, RequestedTheme = ElementTheme.Dark, Title = "Settings", Content = new ScrollViewer { Content = panel }, PrimaryButtonText = "Save", CloseButtonText = "Cancel" };
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                settings.Current.HideLikes = likes.IsOn;
                settings.Current.ShowVideo = video.IsOn;
                settings.Current.Compact = compact.IsOn;
                settings.Current.MinimizeToTray = minimize.IsOn;
                settings.Current.RememberVolume = volume.IsOn;
                settings.Current.MediaKeys = media.IsOn;
                settings.Current.Theme = theme.SelectedItem as string ?? "Dark";
                settings.Current.ArtworkAccent = accentToggle.IsOn;
                settings.Current.Animations = animations.IsOn;
                settings.Current.WindowsMediaIntegration = systemMedia.IsOn;
                settings.Current.AlwaysOnTop = topmost.IsOn;
                settings.Current.StartMinimized = startMinimized.IsOn;
                if (startup.IsOn != startupEnabled)
                {
                    try { StartupService.SetEnabled(startup.IsOn); }
                    catch { await new ContentDialog { XamlRoot = Root.XamlRoot, RequestedTheme = ElementTheme.Dark, Title = "Startup setting", Content = "Windows could not update this setting. Your other settings will still be saved.", CloseButtonText = "OK" }.ShowAsync(); }
                }
                ApplyNativeSettings();
                _ = RefreshAccentAsync();
                if (tray != null) tray.MediaKeys = media.IsOn;
                if (!settings.Save()) LoadingText.Text = "Settings could not be saved.";
                if (web.Bridge != null) await web.Bridge.ApplyStylesAsync(settings.Current);
            }
        }
        catch { }
        finally { settingsOpen = false; }
    }
}
