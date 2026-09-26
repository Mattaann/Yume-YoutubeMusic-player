using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Nocturne.Models;
using Nocturne.Services;
using Nocturne.Settings;
namespace Nocturne.WebView;
public sealed class YouTubeMusicBridge
{
    private readonly CoreWebView2 core;
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    public event Action<PlaybackState>? StateChanged;
    public event Action<string>? Shortcut;
    public event Action<ContextState>? ContextChanged;
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
        var names = new[] { "selectors", "metadata", "controls", "navigation", "context", "observer", "bridge" };
        var body = string.Join("\n", names.Select(n => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "WebView", "Scripts", n + ".js"))));
        await core.ExecuteScriptAsync("(()=>{if(location.origin!=='https://music.youtube.com'||window.__nocturne)return;" + body + "})();");
        await ApplyStylesAsync(preferences);
    }
    public async Task ApplyStylesAsync(Preferences preferences)
    {
        if (!NavigationPolicy.IsMusic(core.Source)) return;
        var files = new List<string> { "base", "clean-player" };
        if (preferences.HideLikes) files.Add("hide-likes");
        if (!preferences.ShowVideo) files.Add("hide-video");
        if (preferences.Compact) files.Add("compact");
        if (preferences.Theme == "Black") files.Add("black");
        if (!preferences.Animations) files.Add("no-animation");
        var css = string.Join("\n", files.Select(n => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "WebView", "Styles", n + ".css"))));
        if (!preferences.HideLikes) css += "\n:root[data-nocturne-player] ytmusic-player-bar{opacity:1!important;height:72px!important;pointer-events:auto!important;}:root[data-nocturne-player] ytmusic-app{--ytmusic-player-bar-height:72px!important;}";
        await core.ExecuteScriptAsync("(()=>{if(location.origin!=='https://music.youtube.com')return;let s=document.getElementById('nocturne-style');if(!s){s=document.createElement('style');s.id='nocturne-style';document.head.append(s);}s.textContent=" + JsonSerializer.Serialize(css) + ";})();");
    }
    public async Task CommandAsync(string command, double value = 0)
    {
        if (!NavigationPolicy.IsMusic(core.Source) || !double.IsFinite(value)) return;
        await core.ExecuteScriptAsync("window.__nocturne?.command(" + JsonSerializer.Serialize(new { command, value }) + ")");
    }
    public async Task NavigateAsync(string path)
    {
        if (!NavigationPolicy.IsMusic(core.Source)) return;
        await core.ExecuteScriptAsync("window.__nocturne?.navigate(" + JsonSerializer.Serialize(path) + ")");
    }
    public async Task SetPanelAsync(string panel) => await CallAsync("setContextPanel", panel);
    public async Task SelectQueueAsync(string key) => await CallAsync("selectQueueItem", key);
    public async Task OpenWebsitePanelAsync(string panel) => await CallAsync("openWebsitePanel", panel);
    private async Task CallAsync(string function, string argument)
    {
        if (!NavigationPolicy.IsMusic(core.Source)) return;
        await core.ExecuteScriptAsync("window.__nocturne?." + function + "(" + JsonSerializer.Serialize(argument) + ")");
    }
}
