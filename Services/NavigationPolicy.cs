namespace Yume.Services;
public static class NavigationPolicy
{
    public static bool IsMusic(string address) => Uri.TryCreate(address, UriKind.Absolute, out var uri) && uri.Scheme == "https" && uri.IsDefaultPort && uri.Host == "music.youtube.com";
    public static bool IsInternal(string address)
    {
        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri) || uri.Scheme != "https" || !uri.IsDefaultPort) return false;
        return uri.Host is "music.youtube.com" or "accounts.google.com" or "myaccount.google.com" or "consent.google.com" or "consent.youtube.com" ||
            (uri.Host == "www.youtube.com" && uri.AbsolutePath.StartsWith("/signin", StringComparison.Ordinal));
    }
    public static bool CanOpenExternal(string address) => Uri.TryCreate(address, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https";
}
