using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Yume.Models;
using Yume.Services;
using Yume.Settings;
namespace Yume.WebView;
public sealed class YouTubeMusicBridge
{
    private readonly CoreWebView2 core;
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    public event Action<PlaybackState>? StateChanged;
    public event Action<string>? Shortcut;
    public event Action<ContextState>? ContextChanged;
    public event Action<string>? NavigationChanged;
    public event Action<string, string[]>? SuggestionsChanged;
    public YouTubeMusicBridge(CoreWebView2 core)
    {
        this.core = core;
        core.WebMessageReceived += Receive;
    }
    private void Receive(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        if (!NavigationPolicy.IsMusic(args.Source) || !NavigationPolicy.IsMusic(core.Source)) return;
        try
        {
            var json = args.WebMessageAsJson;
            if (json.Length > 131072) return;
            using var message = JsonDocument.Parse(json);
            var root = message.RootElement;
            if (!root.TryGetProperty("type", out var type)) return;
            if (type.GetString() == "state")
            {
                var state = root.GetProperty("data").Deserialize<PlaybackState>(Json);
                if (state != null && double.IsFinite(state.Position) && double.IsFinite(state.Duration) && double.IsFinite(state.Volume)) StateChanged?.Invoke(state);
            }
            else if (type.GetString() == "shortcut") Shortcut?.Invoke(root.GetProperty("key").GetString() ?? "");
            else if (type.GetString() == "navigation") NavigationChanged?.Invoke(root.GetProperty("path").GetString() ?? "");
            else if (type.GetString() == "suggestions")
            {
                var query = root.GetProperty("query").GetString();
                var items = root.GetProperty("items").Deserialize<string[]>();
                if (query is { Length: <= 200 } && items is { Length: <= 10 } && items.All(item => item is { Length: <= 200 })) SuggestionsChanged?.Invoke(query, items);
            }
            else if (type.GetString() == "context")
            {
                var context = root.GetProperty("data").Deserialize<ContextState>(Json);
                if (context != null && context.Items is { Length: <= 80 } && context.Lyrics is { Length: <= 20000 } && context.Items.All(item => item != null)) ContextChanged?.Invoke(context);
            }
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or KeyNotFoundException) { }
    }
    public async Task InjectAsync(Preferences preferences)
    {
        if (!NavigationPolicy.IsMusic(core.Source)) return;
        var names = new[] { "selectors", "metadata", "controls", "navigation", "context", "suggestions", "library", "equalizer", "observer", "bridge" };
        var body = string.Join("\n", names.Select(n => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "WebView", "Scripts", n + ".js"))));
        await core.ExecuteScriptAsync("(()=>{if(location.origin!=='https://music.youtube.com'||window.__yume)return;" + body + "})();");
        await ApplyStylesAsync(preferences);
        await ApplyEqualizerAsync(preferences);
    }
    public async Task ApplyStylesAsync(Preferences preferences)
    {
        if (!NavigationPolicy.IsMusic(core.Source)) return;
        var files = new List<string> { "base", "clean-player" };
        if (preferences.HideLikes) files.Add("hide-likes");
        if (!preferences.ShowVideo) files.Add("hide-video");
        if (preferences.Compact) files.Add("compact");
        if (preferences.Theme == "Light") files.Add("light");
        if (preferences.Theme == "Yume") files.Add("yume");
        if (!preferences.Animations) files.Add("no-animation");
        var css = string.Join("\n", files.Select(n => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "WebView", "Styles", n + ".css"))));
        if (!preferences.HideLikes) css += "\n:root[data-yume-player] ytmusic-player-bar{opacity:1!important;height:72px!important;pointer-events:auto!important;}:root[data-yume-player] ytmusic-app{--ytmusic-player-bar-height:72px!important;}";
        await core.ExecuteScriptAsync("(()=>{if(location.origin!=='https://music.youtube.com')return;let s=document.getElementById('yume-style');if(!s){s=document.createElement('style');s.id='yume-style';document.head.append(s);}s.textContent=" + JsonSerializer.Serialize(css) + ";})();");
    }
    public async Task ApplyEqualizerAsync(Preferences preferences)
    {
        if (!NavigationPolicy.IsMusic(core.Source)) return;
        var options = JsonSerializer.Serialize(new { enabled = preferences.EqualizerEnabled, gains = preferences.EqualizerGains });
        await core.ExecuteScriptAsync("window.__yume?.configureEqualizer(" + options + ")");
    }
    public async Task SetBackgroundAsync(double width, double height, double x, double y, double purple, double blue)
    {
        if (!NavigationPolicy.IsMusic(core.Source)) return;
        var values = JsonSerializer.Serialize(new[] { width, height, x, y, purple, blue });
        await core.ExecuteScriptAsync("(()=>{const v=" + values + ";const s=document.documentElement.style;s.setProperty('--yume-shared-background','linear-gradient(90deg,#0d0e17 0%,#181427 '+v[4]+'%,#2b1c44 '+v[5]+'%,#192b48 100%)');s.setProperty('--yume-shared-size',v[0]+'px '+v[1]+'px');s.setProperty('--yume-shared-position',(-v[2])+'px '+(-v[3])+'px');})();");
    }
    public async Task<T?> ReadAsync<T>(string function)
    {
        if (!NavigationPolicy.IsMusic(core.Source)) return default;
        var script = "window.__yume?." + function + "()";
        var ticket = Guid.NewGuid().ToString("N");
        await core.ExecuteScriptAsync("window.__yumeRequests ??= {}; window.__yumeRequests['" + ticket + "']=null; Promise.resolve(" + script + ").then(v=>{if('" + ticket + "' in window.__yumeRequests)window.__yumeRequests['" + ticket + "']={value:v}},()=>{if('" + ticket + "' in window.__yumeRequests)window.__yumeRequests['" + ticket + "']={value:null}});");
        try
        {
            for (var attempt = 0; attempt < 100; attempt++)
            {
                await Task.Delay(150);
                if (!NavigationPolicy.IsMusic(core.Source)) return default;
                var json = await core.ExecuteScriptAsync("window.__yumeRequests?.['" + ticket + "'] ?? null");
                if (json == "null") continue;
                using var result = JsonDocument.Parse(json);
                return result.RootElement.GetProperty("value").Deserialize<T>(Json);
            }
            return default;
        }
        finally { if (NavigationPolicy.IsMusic(core.Source)) await core.ExecuteScriptAsync("delete window.__yumeRequests?.['" + ticket + "']"); }
    }
    public Task<AccountInfo?> ReadAccountAsync() => ReadAsync<AccountInfo>("readAccount");
    public Task<LibraryResult?> ReadLibraryAsync() => ReadAsync<LibraryResult>("readLibraryPlaylists");
    public async Task<string?> AddSongAsync(string playlistId, string videoId)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(playlistId, @"^[\w-]+$") || !System.Text.RegularExpressions.Regex.IsMatch(videoId, @"^[\w-]{11}$")) return "Invalid song or playlist.";
        return await ReadAsync<string>("addDraggedSong.bind(null," + JsonSerializer.Serialize(playlistId) + "," + JsonSerializer.Serialize(videoId) + ")");
    }
    public async Task<string?> AddQueueSongAsync(string playlistId, string key)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(playlistId, @"^[\w-]+$") || !System.Text.RegularExpressions.Regex.IsMatch(key, @"^q[0-9]+$")) return "Invalid queue item.";
        return await ReadAsync<string>("addQueuedSong.bind(null," + JsonSerializer.Serialize(playlistId) + "," + JsonSerializer.Serialize(key) + ")");
    }
    public async Task SignOutAsync()
    {
        await core.Profile.ClearBrowsingDataAsync(CoreWebView2BrowsingDataKinds.Cookies | CoreWebView2BrowsingDataKinds.AllDomStorage);
        core.Navigate("https://music.youtube.com");
    }
    public async Task CommandAsync(string command, double value = 0)
    {
        if (!NavigationPolicy.IsMusic(core.Source) || !double.IsFinite(value)) return;
        await core.ExecuteScriptAsync("window.__yume?.command(" + JsonSerializer.Serialize(new { command, value }) + ")");
    }
    public async Task NavigateAsync(string path)
    {
        if (!NavigationPolicy.IsMusic(core.Source)) return;
        await core.ExecuteScriptAsync("window.__yume?.navigate(" + JsonSerializer.Serialize(path) + ")");
    }
    public async Task SetPanelAsync(string panel) => await CallAsync("setContextPanel", panel);
    public async Task RequestSuggestionsAsync(string query) => await CallAsync("requestSuggestions", query);
    public async Task SelectQueueAsync(string key) => await CallAsync("selectQueueItem", key);
    public async Task OpenWebsitePanelAsync(string panel) => await CallAsync("openWebsitePanel", panel);
    private async Task CallAsync(string function, string argument)
    {
        if (!NavigationPolicy.IsMusic(core.Source)) return;
        await core.ExecuteScriptAsync("window.__yume?." + function + "(" + JsonSerializer.Serialize(argument) + ")");
    }
}
