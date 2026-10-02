namespace Yume.Models;
public sealed class AccountInfo
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Photo { get; set; } = "";
    public bool SignedIn { get; set; }
}
public sealed class LibraryPlaylist
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
}
public sealed class LibraryResult
{
    public LibraryPlaylist[] Items { get; set; } = [];
    public string Error { get; set; } = "";
}
