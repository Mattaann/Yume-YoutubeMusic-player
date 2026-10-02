using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Yume.Settings;
namespace Yume.Views;
public sealed class EqualizerWindow : Window
{
    private static readonly double[] Frequencies = [30, 120, 350, 1200, 3500, 9000];
    private static readonly string[] Labels = ["30 Hz", "120 Hz", "350 Hz", "1.2 kHz", "3.5 kHz", "9 kHz"];
    private static readonly string[] Colors = ["#FF626C", "#37CEE8", "#C477FF", "#53E78A", "#E8CE48", "#FFA44D"];
    private static readonly Dictionary<string, double[]> Presets = new()
    {
        ["Flat"] = [0, 0, 0, 0, 0, 0],
        ["Bass boost"] = [6, 4, 1, 0, 0, 0],
        ["Vocal"] = [-3, -2, 1, 4, 3, 0],
        ["Rock"] = [4, 3, -2, 0, 3, 4],
        ["Electronic"] = [5, 4, -2, -1, 3, 5],
        ["Warm"] = [2, 3, 2, 0, -2, -3]
    };
    private readonly Canvas graph = new() { MinHeight = 240 };
    private readonly Polyline curve = new() { StrokeThickness = 2, IsHitTestVisible = false };
    private readonly Thumb[] handles = new Thumb[6];
    private readonly TextBlock[] values = new TextBlock[6];
    private readonly Line[] guides = new Line[5];
    private readonly TextBlock[] ticks = new TextBlock[5];
    private readonly Line[] columns = new Line[6];
    private readonly ComboBox preset = new() { HorizontalAlignment = HorizontalAlignment.Stretch };
    private readonly TextBlock status = new() { TextWrapping = TextWrapping.Wrap, FontSize = 12, Opacity = 0.75 };
    private readonly SettingsService settings;
    private readonly Func<Task> apply;
    private readonly Func<Task<string>> readStatus;
    private readonly Microsoft.UI.Dispatching.DispatcherQueueTimer timer;
    private bool updating;
    private bool applying;
    private bool dirty;
    private bool closed;
    public EqualizerWindow(SettingsService settings, Func<Task> apply, Func<Task<string>> readStatus)
    {
        this.settings = settings; this.apply = apply; this.readStatus = readStatus;
        Title = "Yume · Equalizer";
        AppWindow.SetIcon(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Yume.ico"));
        AppWindow.Resize(new Windows.Graphics.SizeInt32(680, 560));
        if (AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter) { presenter.PreferredMinimumWidth = 540; presenter.PreferredMinimumHeight = 500; }
        var light = settings.Current.Theme == "Light";
        var root = new Grid { Padding = new Thickness(24), RowSpacing = 18, RequestedTheme = light ? ElementTheme.Light : ElementTheme.Dark, Background = Brush(light ? "#F7F8FA" : "#100F1B") };
        root.RowDefinitions.Add(new() { Height = GridLength.Auto }); root.RowDefinitions.Add(new() { Height = GridLength.Auto });
        root.RowDefinitions.Add(new() { Height = new GridLength(1, GridUnitType.Star) }); root.RowDefinitions.Add(new() { Height = GridLength.Auto }); root.RowDefinitions.Add(new() { Height = GridLength.Auto });
        var header = new Grid(); header.ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) }); header.ColumnDefinitions.Add(new() { Width = GridLength.Auto }); header.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        header.Children.Add(new TextBlock { Text = "Equalizer", FontSize = 26, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center });
        var enabled = new ToggleSwitch { IsOn = settings.Current.EqualizerEnabled, VerticalAlignment = VerticalAlignment.Center };
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(enabled, "Enable equalizer");
        Grid.SetColumn(enabled, 1); header.Children.Add(enabled);
        var reset = new Button { Content = "↺", FontSize = 24, Margin = new Thickness(12, 0, 0, 0) }; ToolTipService.SetToolTip(reset, "Reset to Flat");
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(reset, "Reset equalizer");
        Grid.SetColumn(reset, 2); header.Children.Add(reset); root.Children.Add(header);
        preset.ItemsSource = Presets.Keys.Append("Custom").ToArray();
        preset.SelectedItem = Presets.TryGetValue(settings.Current.EqualizerPreset, out var stored) && stored.SequenceEqual(settings.Current.EqualizerGains) ? settings.Current.EqualizerPreset : "Custom";
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(preset, "Equalizer preset");
        Grid.SetRow(preset, 1); root.Children.Add(preset);
        var surface = new Border { Background = Brush(light ? "#ECEEF3" : "#191826"), CornerRadius = new CornerRadius(14), Child = graph };
        Grid.SetRow(surface, 2); root.Children.Add(surface);
        curve.Stroke = Brush(light ? "#51416D" : "#D4C7F2");
        for (var i = 0; i < 5; i++)
        {
            guides[i] = new Line { Stroke = Brush(light ? "#CED0D8" : "#363442"), StrokeThickness = i == 2 ? 1.5 : 1, IsHitTestVisible = false };
            ticks[i] = new TextBlock { Text = new[] { "+12", "+6", "0", "−6", "−12" }[i], FontSize = 11, Opacity = 0.65, IsHitTestVisible = false };
            graph.Children.Add(guides[i]); graph.Children.Add(ticks[i]);
        }
        for (var i = 0; i < 6; i++) { columns[i] = new Line { Stroke = Brush(light ? "#DADCE3" : "#292735"), StrokeThickness = 1, IsHitTestVisible = false }; graph.Children.Add(columns[i]); }
        graph.Children.Add(curve);
        var labels = new Grid { ColumnSpacing = 6 };
        for (var i = 0; i < 6; i++)
        {
            var index = i; labels.ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) });
            var group = new StackPanel { Spacing = 4, HorizontalAlignment = HorizontalAlignment.Center };
            group.Children.Add(new TextBlock { Text = "●", Foreground = Brush(Colors[i]), HorizontalAlignment = HorizontalAlignment.Center });
            group.Children.Add(new TextBlock { Text = Labels[i], FontSize = 12, HorizontalAlignment = HorizontalAlignment.Center });
            values[i] = new TextBlock { FontSize = 12, HorizontalAlignment = HorizontalAlignment.Center, Opacity = 0.7 }; group.Children.Add(values[i]);
            Grid.SetColumn(group, i); labels.Children.Add(group);
            var thumb = new Thumb { Width = 26, Height = 26, IsTabStop = true };
            thumb.Template = (ControlTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='Thumb'><Ellipse Fill='" + Colors[i] + "' Stroke='White' StrokeThickness='2'/></ControlTemplate>");
            thumb.DragDelta += (_, e) => ChangeBand(index, settings.Current.EqualizerGains[index] - e.VerticalChange * 24 / PlotHeight);
            thumb.KeyDown += (_, e) =>
            {
                if (e.Key is Windows.System.VirtualKey.Up or Windows.System.VirtualKey.Down)
                { ChangeBand(index, settings.Current.EqualizerGains[index] + (e.Key == Windows.System.VirtualKey.Up ? 1 : -1)); e.Handled = true; }
                else if (e.Key == Windows.System.VirtualKey.Home) { ChangeBand(index, 0); e.Handled = true; }
            };
            handles[i] = thumb; graph.Children.Add(thumb);
        }
        Grid.SetRow(labels, 3); root.Children.Add(labels);
        var footer = new StackPanel { Spacing = 6 };
        footer.Children.Add(new TextBlock { Text = "Drag points up or down · ±12 dB · Changes save automatically", FontSize = 12, Opacity = 0.7, TextWrapping = TextWrapping.Wrap });
        footer.Children.Add(status); Grid.SetRow(footer, 4); root.Children.Add(footer);
        timer = DispatcherQueue.CreateTimer(); timer.Interval = TimeSpan.FromMilliseconds(100); timer.IsRepeating = false;
        timer.Tick += async (_, _) => await CommitAsync();
        enabled.Toggled += (_, _) => { settings.Current.EqualizerEnabled = enabled.IsOn; Changed(); };
        preset.SelectionChanged += (_, _) => { if (!updating && preset.SelectedItem is string name && Presets.TryGetValue(name, out var gains)) { settings.Current.EqualizerGains = gains.ToArray(); settings.Current.EqualizerPreset = name; Changed(); } };
        reset.Click += (_, _) => { settings.Current.EqualizerGains = new double[6]; settings.Current.EqualizerPreset = "Flat"; updating = true; preset.SelectedItem = "Flat"; updating = false; Changed(); };
        graph.SizeChanged += (_, _) => Draw();
        Closed += async (_, _) => { closed = true; timer.Stop(); await CommitAsync(); };
        Content = root; Draw();
        status.Text = settings.Current.EqualizerEnabled ? "Adjust a band or choose a preset." : "Equalizer off. Your curve is kept for next time.";
    }
    private static SolidColorBrush Brush(string hex) => new(Windows.UI.Color.FromArgb(255, Convert.ToByte(hex.Substring(1, 2), 16), Convert.ToByte(hex.Substring(3, 2), 16), Convert.ToByte(hex.Substring(5, 2), 16)));
    private double PlotHeight => Math.Max(1, graph.ActualHeight - 40);
    private double X(int index) => 48 + Math.Log(Frequencies[index] / 30) / Math.Log(9000.0 / 30) * Math.Max(1, graph.ActualWidth - 72);
    private double Y(double gain) => 20 + (12 - gain) / 24 * PlotHeight;
    private void ChangeBand(int index, double value)
    {
        settings.Current.EqualizerGains[index] = Math.Round(Math.Clamp(value, -12, 12), 1);
        settings.Current.EqualizerPreset = "Custom"; updating = true; preset.SelectedItem = "Custom"; updating = false; Changed();
    }
    private void Changed() { Draw(); dirty = true; timer.Stop(); timer.Start(); }
    private void Draw()
    {
        for (var i = 0; i < 5; i++) { var y = Y(12 - i * 6); guides[i].X1 = 44; guides[i].X2 = Math.Max(44, graph.ActualWidth - 20); guides[i].Y1 = guides[i].Y2 = y; Canvas.SetLeft(ticks[i], 10); Canvas.SetTop(ticks[i], y - 8); }
        var points = new PointCollection();
        points.Add(new Windows.Foundation.Point(44, Y(settings.Current.EqualizerGains[0])));
        for (var i = 0; i < 6; i++)
        {
            var gain = settings.Current.EqualizerGains[i]; var x = X(i); var y = Y(gain);
            columns[i].X1 = columns[i].X2 = x; columns[i].Y1 = 20; columns[i].Y2 = Y(-12);
            Canvas.SetLeft(handles[i], x - 13); Canvas.SetTop(handles[i], y - 13); points.Add(new Windows.Foundation.Point(x, y));
            values[i].Text = $"{gain:+0.#;-0.#;0} dB";
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(handles[i], Labels[i] + ", " + values[i].Text + ". Use up and down arrows to adjust.");
        }
        points.Add(new Windows.Foundation.Point(Math.Max(44, graph.ActualWidth - 20), Y(settings.Current.EqualizerGains[5]))); curve.Points = points;
    }
    private async Task CommitAsync()
    {
        if (applying) return;
        applying = true;
        try
        {
            do { dirty = false; var saved = settings.Save(); await apply(); if (!closed) status.Text = saved ? await readStatus() : "Applied, but could not save to disk."; } while (dirty);
        }
        catch { if (!closed) status.Text = "Could not apply equalizer. Start playback and try again."; }
        finally { applying = false; }
    }
}

