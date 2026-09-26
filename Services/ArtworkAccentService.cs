using System.Net.Http;
using Nocturne.Utilities;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI;
namespace Nocturne.Services;
public sealed class ArtworkAccentService : IDisposable
{
    private readonly HttpClient client = new(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(6) };
    public static Color Neutral => Color.FromArgb(255, 184, 216, 210);
    public async Task<Color> ExtractAsync(string address, CancellationToken cancellation)
    {
        var uri = ArtworkPolicy.GetUri(address);
        if (uri == null) return Neutral;
        try
        {
            using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellation);
            if (!response.IsSuccessStatusCode || response.Content.Headers.ContentLength > 4_000_000) return Neutral;
            await using var source = await response.Content.ReadAsStreamAsync(cancellation);
            using var buffer = new MemoryStream();
            var chunk = new byte[8192];
            int count;
            while ((count = await source.ReadAsync(chunk, cancellation)) > 0)
            {
                if (buffer.Length + count > 4_000_000) return Neutral;
                buffer.Write(chunk, 0, count);
            }
            using var stream = new InMemoryRandomAccessStream();
            using (var writer = new DataWriter(stream.GetOutputStreamAt(0)))
            {
                writer.WriteBytes(buffer.ToArray());
                await writer.StoreAsync();
            }
            stream.Seek(0);
            var decoder = await BitmapDecoder.CreateAsync(stream);
            var pixels = (await decoder.GetPixelDataAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, new BitmapTransform { ScaledWidth = 24, ScaledHeight = 24 }, ExifOrientationMode.IgnoreExifOrientation, ColorManagementMode.DoNotColorManage)).DetachPixelData();
            cancellation.ThrowIfCancellationRequested();
            var groups = new Dictionary<int, int>();
            for (var i = 0; i + 3 < pixels.Length; i += 4)
            {
                var r = pixels[i + 2]; var g = pixels[i + 1]; var b = pixels[i];
                var high = Math.Max(r, Math.Max(g, b)); var low = Math.Min(r, Math.Min(g, b));
                if (high < 60 || low > 225 || high - low < 30) continue;
                var key = ((r >> 4) << 8) | ((g >> 4) << 4) | (b >> 4);
                groups[key] = groups.GetValueOrDefault(key) + 1;
            }
            if (groups.Count == 0) return Neutral;
            var selected = groups.MaxBy(pair => pair.Value).Key;
            static byte Soften(int component) => (byte)Math.Clamp(component * 16 * 0.65 + 85, 100, 235);
            return Color.FromArgb(255, Soften((selected >> 8) & 15), Soften((selected >> 4) & 15), Soften(selected & 15));
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or OperationCanceledException or System.Runtime.InteropServices.COMException) { return Neutral; }
    }
    public void Dispose() => client.Dispose();
}
