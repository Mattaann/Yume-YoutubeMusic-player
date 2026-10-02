using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Yume.Settings;
using Yume.WindowsIntegration;
namespace Yume.Views;
public sealed class SettingsWindow : Window
{
    private EqualizerWindow? equalizerWindow;
    private readonly Grid root = new();
    private readonly StackPanel body = new() { Spacing = 20 };
    private readonly TextBlock status = new() { TextWrapping = TextWrapping.Wrap, MaxWidth = 440 };
    private readonly Dictionary<string, StackPanel> pages = new();
    private readonly Dictionary<string, ToggleSwitch> toggles = new();
    private readonly List<Button> tabs = new();
    private readonly ComboBox theme = new() { Header = "Theme", ItemsSource = new[] { "Yume", "Dark", "Light" }, HorizontalAlignment = HorizontalAlignment.Stretch };
    public SettingsWindow(SettingsService settings, bool hasTray, Func<Task> apply, Func<Task> openAccount, Func<Task<string>> eqStatus, Func<Task> applyEq)
    {
        Title = "Yume · Settings";
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "Yume.ico"));
        AppWindow.Resize(new Windows.Graphics.SizeInt32(880, 720));
        if (AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter) presenter.PreferredMinimumWidth = 700;
        root.Padding = new Thickness(24);
        root.RowDefinitions.Add(new() { Height = GridLength.Auto });
        root.RowDefinitions.Add(new() { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new() { Height = GridLength.Auto });
        var heading = new StackPanel { Spacing = 4, Margin = new Thickness(0, 0, 0, 24) };
        heading.Children.Add(new TextBlock { Text = "Yume", FontSize = 28, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        heading.Children.Add(new TextBlock { Text = "Your music. Your way.", Opacity = 0.7 });
        root.Children.Add(heading);
        var content = new Grid { ColumnSpacing = 28 };
        content.ColumnDefinitions.Add(new() { Width = new GridLength(160) });
        content.ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(content, 1); root.Children.Add(content);
        var navigation = new StackPanel { Spacing = 8 };
        content.Children.Add(navigation);
        var divider = new Border { Width = 1, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 0, -14, 0), Background = new SolidColorBrush(Windows.UI.Color.FromArgb(70, 140, 140, 150)) };
        content.Children.Add(divider);
        var scroll = new ScrollViewer { Content = body, Padding = new Thickness(0, 0, 20, 16), HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        Grid.SetColumn(scroll, 1); content.Children.Add(scroll);
        foreach (var name in new[] { "General", "Appearance", "Sound" })
        {
            var page = new StackPanel { Spacing = 20 };
            page.Children.Add(new TextBlock { Text = name, FontSize = 24, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
            pages[name] = page;
            var tab = new Button { Content = name, Tag = name, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Left, Padding = new Thickness(16, 12, 16, 12) };
            tab.Click += (_, _) => SelectPage(name);
            tabs.Add(tab); navigation.Children.Add(tab);
        }
        var p = settings.Current;
        AddToggle("General", "tray", "Close to system tray", p.MinimizeToTray, hasTray);
        AddToggle("General", "minimized", "Start in system tray", p.StartMinimized, hasTray);
        AddToggle("General", "top", "Always on top", p.AlwaysOnTop);
        var startupEnabled = StartupService.IsEnabled();
        AddToggle("General", "startup", "Start with Windows", startupEnabled);
        var version = typeof(App).Assembly.GetName().Version;
        pages["General"].Children.Add(Note($"Yume {version?.Major}.{version?.Minor}.{version?.Build}\nCtrl+K: search · Ctrl+Q: queue · Ctrl+J: lyrics"));
        AddGroup("Appearance", "Theme", () =>
        {
            theme.SelectedItem = p.Theme;
            pages["Appearance"].Children.Add(theme);
            AddToggle("Appearance", "visual", "Visual theme", p.VisualTheme, p.Theme == "Yume");
            pages["Appearance"].Children.Add(Note("Soft purple and blue colors drift while music plays. Pause freezes the motion; play continues from the same position. Available with the Yume theme."));
            AddToggle("Appearance", "accent", "Use artwork accent colors", p.ArtworkAccent);
        });
        theme.SelectionChanged += (_, _) => toggles["visual"].IsEnabled = (string?)theme.SelectedItem == "Yume";
        AddGroup("Appearance", "Show in sidebar", () =>
        {
            AddToggle("Appearance", "sidebarPlaylists", "Playlists under Playlists", p.ShowSidebarPlaylists);
            AddToggle("Appearance", "likedMusic", "Liked music", p.ShowLikedMusic);
            AddToggle("Appearance", "albums", "Albums", p.ShowAlbums);
            AddToggle("Appearance", "artists", "Artists", p.ShowArtists);
            AddToggle("Appearance", "podcasts", "Podcasts", p.ShowPodcasts);
        });
        AddGroup("Appearance", "Hide", () =>
        {
            AddToggle("Appearance", "likes", "Hide Like / Dislike", p.HideLikes);
        });
        AddGroup("Appearance", "Content", () =>
        {
            AddToggle("Appearance", "video", "Show video content", p.ShowVideo);
            AddToggle("Appearance", "animation", "Website animations", p.Animations);
            AddToggle("Appearance", "compact", "Compact content", p.Compact);
        });
        var equalizerButton = new Button { Content = "Open Equalizer", HorizontalAlignment = HorizontalAlignment.Stretch };
        equalizerButton.Click += (_, _) =>
        {
            if (equalizerWindow != null) { equalizerWindow.Activate(); return; }
            equalizerWindow = new EqualizerWindow(settings, applyEq, eqStatus);
            equalizerWindow.Closed += (_, _) => equalizerWindow = null;
            equalizerWindow.Activate();
        };
        pages["Sound"].Children.Add(equalizerButton);
        pages["Sound"].Children.Add(Note("Six frequency bands, draggable controls and presets. Changes in the Equalizer apply and save automatically."));
        Closed += (_, _) => equalizerWindow?.Close();
        AddToggle("Sound", "volume", "Remember volume", p.RememberVolume);
        AddToggle("Sound", "media", "Media key support", p.MediaKeys);
        AddToggle("Sound", "system", "Windows media information", p.WindowsMediaIntegration);
        pages["Sound"].Children.Add(new TextBlock { Text = "Audio quality", FontSize = 18, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        pages["Sound"].Children.Add(Note("YouTube Music manages audio quality. Open the account menu, then Settings > Playback > Audio quality. Available choices depend on your account and subscription."));
        var quality = new Button { Content = "Open YouTube Music account menu" };
        quality.Click += async (_, _) => { try { await openAccount(); } catch { status.Text = "Open Account in the main window to change YouTube Music settings."; } };
        pages["Sound"].Children.Add(quality);
        var footer = new Grid { ColumnSpacing = 12, Margin = new Thickness(0, 20, 0, 0) };
        footer.ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) });
        footer.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        footer.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        footer.Children.Add(status);
        var close = new Button { Content = "Close" }; close.Click += (_, _) => Close();
        Grid.SetColumn(close, 1); footer.Children.Add(close);
        var save = new Button { Content = "Save", Style = (Style)Application.Current.Resources["AccentButtonStyle"] };
        Grid.SetColumn(save, 2); footer.Children.Add(save);
        save.Click += async (_, _) =>
        {
            save.IsEnabled = false;
            try
            {
                if (On("startup") != startupEnabled) { StartupService.SetEnabled(On("startup")); startupEnabled = On("startup"); }
                p.MinimizeToTray = On("tray"); p.StartMinimized = On("minimized"); p.AlwaysOnTop = On("top");
                p.Theme = theme.SelectedItem as string ?? "Yume"; p.VisualTheme = On("visual"); p.ArtworkAccent = On("accent");
                p.ShowSidebarPlaylists = On("sidebarPlaylists"); p.ShowLikedMusic = On("likedMusic"); p.ShowAlbums = On("albums"); p.ShowArtists = On("artists"); p.ShowPodcasts = On("podcasts");
                p.Animations = On("animation"); p.Compact = On("compact"); p.HideLikes = On("likes"); p.ShowVideo = On("video");
                p.RememberVolume = On("volume"); p.MediaKeys = On("media"); p.WindowsMediaIntegration = On("system");
                var saved = settings.Save();
                ApplyTheme(p.Theme);
                await apply();
                status.Text = saved ? "Settings saved. " + await eqStatus() : "Applied for this session. Settings could not be saved to disk.";
            }
            catch { status.Text = "Could not apply all settings. Please try again."; }
            finally { save.IsEnabled = true; }
        };
        Grid.SetRow(footer, 2); root.Children.Add(footer);
        Content = root; ApplyTheme(p.Theme); SelectPage("General");
    }
    private void AddGroup(string page, string title, Action build)
    {
        var parent = pages[page];
        var group = new StackPanel { Spacing = 16 };
        group.Children.Add(new TextBlock { Text = title, FontSize = 18, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        pages[page] = group;
        try { build(); }
        finally { pages[page] = parent; }
        parent.Children.Add(new Border
        {
            Child = group, Padding = new Thickness(18), CornerRadius = new CornerRadius(10), BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(55, 140, 140, 150)),
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(8, 140, 140, 150))
        });
    }
    private bool On(string key) => toggles[key].IsOn;
    private static TextBlock Note(string text) => new() { Text = text, TextWrapping = TextWrapping.Wrap, Opacity = 0.7 };
    private void AddToggle(string page, string key, string label, bool value, bool enabled = true)
    {
        var toggle = new ToggleSwitch { Header = label, IsOn = value, IsEnabled = enabled };
        toggles[key] = toggle; pages[page].Children.Add(toggle);
    }
    private void SelectPage(string name)
    {
        body.Children.Clear(); body.Children.Add(pages[name]);
        foreach (var tab in tabs)
        {
            bool selected = (string)tab.Tag == name;
            tab.BorderBrush = new SolidColorBrush(selected ? Windows.UI.Color.FromArgb(180, 155, 130, 245) : Microsoft.UI.Colors.Transparent);
            tab.Background = new SolidColorBrush(selected ? Windows.UI.Color.FromArgb(35, 155, 130, 245) : Microsoft.UI.Colors.Transparent);
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetItemStatus(tab, selected ? "Current category" : "");
        }
    }
    private void ApplyTheme(string name)
    {
        root.RequestedTheme = name == "Light" ? ElementTheme.Light : ElementTheme.Dark;
        root.Background = new SolidColorBrush(name == "Light" ? Windows.UI.Color.FromArgb(255, 247, 248, 250) : name == "Yume" ? Windows.UI.Color.FromArgb(255, 16, 15, 27) : Windows.UI.Color.FromArgb(255, 17, 18, 19));
    }
}
