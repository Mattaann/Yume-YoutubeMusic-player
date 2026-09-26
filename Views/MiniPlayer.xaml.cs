using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Nocturne.Models;
using Nocturne.Settings;
using Nocturne.Utilities;
using Nocturne.WindowsIntegration;
namespace Nocturne.Views;
public sealed partial class MiniPlayer : Window
{
    private readonly SettingsService settings;
    private bool updating = true;
    private string artwork = "";
    public event Action<string, double>? Command;
    public event Action? RestoreRequested;
    public MiniPlayer(SettingsService settings)
    {
        this.settings = settings;
        InitializeComponent();
        Title = "Nocturne · Mini player";
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "Nocturne.ico"));
        WindowPlacement.Restore(AppWindow, settings.Current.MiniWidth, settings.Current.MiniHeight, settings.Current.MiniX, settings.Current.MiniY, 340, 250);
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = false;
            presenter.PreferredMaximumWidth = 720;
            presenter.PreferredMaximumHeight = 420;
            presenter.IsAlwaysOnTop = settings.Current.MiniAlwaysOnTop;
        }
        Pin.IsChecked = settings.Current.MiniAlwaysOnTop;
        AppWindow.Closing += (_, _) =>
        {
            settings.Current.MiniWidth = AppWindow.Size.Width;
            settings.Current.MiniHeight = AppWindow.Size.Height;
            settings.Current.MiniX = AppWindow.Position.X;
            settings.Current.MiniY = AppWindow.Position.Y;
            settings.Save();
        };
        updating = false;
    }
    public void Update(PlaybackState state, Windows.UI.Color accent)
    {
        updating = true;
        Track.Text = state.Title;
        Performer.Text = state.Artist;
        Play.Content = state.Playing ? "\uE769" : "\uE768";
        Play.IsEnabled = state.Available;
        Play.Background = new SolidColorBrush(accent);
        Position.Maximum = Math.Max(1, state.Duration);
        Position.Value = Math.Clamp(state.Position, 0, Position.Maximum);
        Position.IsEnabled = state.CanSeek;
        if (artwork != state.Artwork)
        {
            artwork = state.Artwork;
            var uri = ArtworkPolicy.GetUri(artwork);
            Cover.Source = uri == null ? null : new BitmapImage(uri);
        }
        updating = false;
    }
    private void Control_Click(object sender, RoutedEventArgs e) => Command?.Invoke((string)((Button)sender).Tag, 0);
    private void Position_Changed(object sender, RangeBaseValueChangedEventArgs e) { if (!updating) Command?.Invoke("seek", e.NewValue); }
    private void Pin_Click(object sender, RoutedEventArgs e)
    {
        settings.Current.MiniAlwaysOnTop = Pin.IsChecked == true;
        if (AppWindow.Presenter is OverlappedPresenter presenter) presenter.IsAlwaysOnTop = settings.Current.MiniAlwaysOnTop;
        settings.Save();
    }
    private void Restore_Click(object sender, RoutedEventArgs e) => RestoreRequested?.Invoke();
}
