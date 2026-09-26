namespace Nocturne.Utilities;
public static class ArtworkPolicy
{
    public static Uri? GetUri(string? address)
    {
        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri) || uri.Scheme != "https" || !uri.IsDefaultPort) return null;
        return new[] { ".googleusercontent.com", ".ytimg.com", ".ggpht.com" }.Any(host => uri.Host.EndsWith(host, StringComparison.Ordinal)) ? uri : null;
    }
}
