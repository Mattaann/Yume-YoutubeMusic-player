namespace Yume.Models;
public sealed record QueueItem
{
    public string Key { get; init; } = "";
    public string Title { get; init; } = "";
    public string Artist { get; init; } = "";
    public string Duration { get; init; } = "";
    public bool Selected { get; init; }
    public string DisplayTitle => Selected ? "▶  " + Title : Title;
    public string Subtitle => string.IsNullOrEmpty(Duration) ? Artist : Artist + " · " + Duration;
}
public sealed record ContextState
{
    public string Panel { get; init; } = "queue";
    public QueueItem[] Items { get; init; } = [];
    public string Lyrics { get; init; } = "";
    public string Message { get; init; } = "";
}
