using Yume.Models;
using Windows.Media;
using Windows.Storage.Streams;
namespace Yume.WindowsIntegration;
public sealed class MediaIntegrationService : IDisposable
{
    private readonly SystemMediaTransportControls controls;
    private string lastMetadata = "";
    public event Action<string>? Command;
    public MediaIntegrationService(nint window)
    {
        controls = SystemMediaTransportControlsInterop.GetForWindow(window);
        controls.IsPlayEnabled = true;
        controls.IsPauseEnabled = true;
        controls.IsNextEnabled = true;
        controls.IsPreviousEnabled = true;
        controls.ButtonPressed += OnButton;
    }
    private void OnButton(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs e)
    {
        var command = e.Button switch { SystemMediaTransportControlsButton.Play => "play", SystemMediaTransportControlsButton.Pause => "pause", SystemMediaTransportControlsButton.Next => "next", SystemMediaTransportControlsButton.Previous => "previous", _ => null };
        if (command != null) Command?.Invoke(command);
    }
    public void Update(PlaybackState state, bool enabled)
    {
        controls.IsEnabled = enabled && state.Available;
        controls.PlaybackStatus = state.Playing ? MediaPlaybackStatus.Playing : MediaPlaybackStatus.Paused;
        var key = state.Title + "\n" + state.Artist + "\n" + state.Artwork;
        if (key == lastMetadata) return;
        lastMetadata = key;
        var display = controls.DisplayUpdater;
        display.Type = MediaPlaybackType.Music;
        display.MusicProperties.Title = state.Title;
        display.MusicProperties.Artist = state.Artist;
        display.MusicProperties.AlbumTitle = state.Album;
        display.Thumbnail = null;
        if (Uri.TryCreate(state.Artwork, UriKind.Absolute, out var uri) && uri.Scheme == "https" && (uri.Host.EndsWith(".googleusercontent.com", StringComparison.Ordinal) || uri.Host.EndsWith(".ytimg.com", StringComparison.Ordinal) || uri.Host.EndsWith(".ggpht.com", StringComparison.Ordinal))) display.Thumbnail = RandomAccessStreamReference.CreateFromUri(uri);
        display.Update();
    }
    public void Dispose() { controls.ButtonPressed -= OnButton; controls.IsEnabled = false; controls.DisplayUpdater.ClearAll(); }
}
