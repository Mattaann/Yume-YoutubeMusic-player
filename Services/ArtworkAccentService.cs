using System.Net.Http;
using Nocturne.Utilities;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI;
namespace Nocturne.Services;
public sealed class ArtworkAccentService : IDisposable
{
    private readonly HttpClient client = new(new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false }) { Timeout = TimeSpan.FromSeconds(6) };
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
            if ((long)decoder.PixelWidth * decoder.PixelHeight > 40_000_000) return Neutral;
            var pixels = (await decoder.GetPixelDataAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, new BitmapTransform { ScaledWidth = 24, ScaledHeight = 24 }, ExifOrientationMode.IgnoreExifOrientation, ColorManagementMode.DoNotColorManage)).DetachPixelData();
            cancellation.ThrowIfCancellationRequested();
            var color = ArtworkPalette.Extract(pixels);
            return Color.FromArgb(255, color.R, color.G, color.B);
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or OperationCanceledException or ArgumentException or System.Runtime.InteropServices.COMException) { return Neutral; }
    }
    public void Dispose() => client.Dispose();
}
