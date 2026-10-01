namespace Yume.Utilities;
public static class ArtworkPalette
{
    public static (byte R, byte G, byte B) Extract(byte[] pixels)
    {
        var groups = new Dictionary<int, int>();
        for (var i = 0; i + 3 < pixels.Length; i += 4)
        {
            var r = pixels[i + 2]; var g = pixels[i + 1]; var b = pixels[i];
            var high = Math.Max(r, Math.Max(g, b)); var low = Math.Min(r, Math.Min(g, b));
            if (high < 60 || low > 225 || high - low < 30) continue;
            var key = ((r >> 4) << 8) | ((g >> 4) << 4) | (b >> 4);
            groups[key] = groups.GetValueOrDefault(key) + 1;
        }
        if (groups.Count == 0) return (184, 216, 210);
        var selected = groups.MaxBy(pair => pair.Value).Key;
        static byte Soften(int component) => (byte)Math.Clamp(component * 16 * 0.65 + 85, 100, 235);
        return (Soften((selected >> 8) & 15), Soften((selected >> 4) & 15), Soften(selected & 15));
    }
}
