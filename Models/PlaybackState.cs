namespace Nocturne.Models;
public sealed record PlaybackState
{
    public string Title { get; init; } = "Choose something to play";
    public string Artist { get; init; } = "YouTube Music";
    public string Album { get; init; } = "";
    public string Artwork { get; init; } = "";
    public bool Playing { get; init; }
    public bool Available { get; init; }
    public bool CanSeek { get; init; }
    public bool CanShuffle { get; init; }
    public bool CanRepeat { get; init; }
    public bool ShuffleActive { get; init; }
    public int RepeatMode { get; init; } = -1;
    public double Position { get; init; }
    public double Duration { get; init; }
    public double Volume { get; init; } = 1;
    public bool Muted { get; init; }
}
