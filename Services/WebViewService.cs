using System.Diagnostics;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Yume.Settings;
using Yume.WebView;
namespace Yume.Services;
public sealed class WebViewService
{
    private readonly WebView2 view;
    private readonly SettingsService settings;
    private bool initializing;
    private string? volumeScriptId;
    private readonly SemaphoreSlim volumeScriptLock = new(1, 1);
    public YouTubeMusicBridge? Bridge { get; private set; }
    public event Action<bool, string>? Loading;
    public event Action? Ready;
    public event Action? PageReady;
    public WebViewService(WebView2 view, SettingsService settings) { this.view = view; this.settings = settings; }
    public async Task InitializeAsync()
    {
        if (initializing || Bridge != null) return;
        initializing = true;
        try
        {
            var environment = await CoreWebView2Environment.CreateWithOptionsAsync(null, Path.Combine(SettingsService.DataDirectory, "WebView2"), null);
            await view.EnsureCoreWebView2Async(environment);
            view.DefaultBackgroundColor = Microsoft.UI.Colors.Transparent;
            var core = view.CoreWebView2;
            core.Settings.AreDevToolsEnabled = false;
            core.Settings.IsStatusBarEnabled = false;
            core.Settings.AreDefaultContextMenusEnabled = false;
            core.Settings.IsPasswordAutosaveEnabled = false;
            core.Settings.IsGeneralAutofillEnabled = false;
            await UpdateVolumePreferenceAsync();
            core.NavigationStarting += (_, e) =>
            {
                if (!NavigationPolicy.IsInternal(e.Uri)) { e.Cancel = true; OpenExternal(e.Uri); return; }
                Loading?.Invoke(true, "Loading your music...");
            };
            core.NewWindowRequested += (_, e) =>
            {
                e.Handled = true;
                if (NavigationPolicy.IsInternal(e.Uri)) core.Navigate(e.Uri); else OpenExternal(e.Uri);
            };
            core.PermissionRequested += (_, e) => e.State = CoreWebView2PermissionState.Deny;
            core.DownloadStarting += (_, e) => e.Cancel = true;
            core.ProcessFailed += (_, _) => Loading?.Invoke(true, "The music player stopped. Try again.");
            Bridge = new(core);
            Ready?.Invoke();
            core.NavigationCompleted += async (_, e) =>
            {
                if (!e.IsSuccess) { Loading?.Invoke(true, "Can't connect to YouTube Music"); return; }
                if (Uri.TryCreate(core.Source, UriKind.Absolute, out var page) && page.Scheme == "https" && page.Host is "consent.youtube.com" or "consent.google.com")
                {
                    try
                    {
                        var script = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "WebView", "Scripts", "consent.js"));
                        if (await core.ExecuteScriptAsync(script) == "true")
                        {
                            Loading?.Invoke(true, "Applying your privacy choice…");
                            await Task.Delay(5000);
                            if (core.Source == page.AbsoluteUri) Loading?.Invoke(false, "");
                            return;
                        }
                    }
                    catch { }
                    Loading?.Invoke(false, "");
                    return;
                }
                try { await Bridge.InjectAsync(settings.Current); Loading?.Invoke(false, ""); PageReady?.Invoke(); }
                catch { Loading?.Invoke(true, "Couldn't initialize the player. Try again."); }
            };
            core.Navigate("https://music.youtube.com");
        }
        catch { Loading?.Invoke(true, "Couldn't start WebView2. Install Microsoft Edge WebView2 Runtime, then restart."); }
        finally { initializing = false; }
    }
    public async void Navigate(string path)
    {
        try
        {
            if (Bridge != null && NavigationPolicy.IsMusic(view.CoreWebView2.Source)) await Bridge.NavigateAsync(path);
            else view.CoreWebView2?.Navigate("https://music.youtube.com" + path);
        }
        catch { Loading?.Invoke(true, "Couldn't navigate. Try again."); }
    }
    public void Retry() { if (view.CoreWebView2 != null) view.CoreWebView2.Reload(); else _ = InitializeAsync(); }
    public async Task UpdateVolumePreferenceAsync()
    {
        await volumeScriptLock.WaitAsync();
        try
        {
            var core = view.CoreWebView2;
            if (core == null) return;
            var value = System.Text.Json.JsonSerializer.Serialize(settings.Current.Volume);
            var remember = settings.Current.RememberVolume ? "true" : "false";
            var body = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "WebView", "Scripts", "volume.js"));
            var script = "(()=>{if(location.origin!=='https://music.youtube.com')return;" + body + ";window.__yumeVolume.configure(" + value + "," + remember + ");})();";
            var previous = volumeScriptId;
            volumeScriptId = await core.AddScriptToExecuteOnDocumentCreatedAsync(script);
            if (previous != null) core.RemoveScriptToExecuteOnDocumentCreated(previous);
            if (NavigationPolicy.IsMusic(core.Source)) await core.ExecuteScriptAsync(script);
        }
        finally { volumeScriptLock.Release(); }
    }
    public static void OpenExternal(string address)
    {
        if (!NavigationPolicy.CanOpenExternal(address)) return;
        try { Process.Start(new ProcessStartInfo(address) { UseShellExecute = true }); } catch { }
    }
}

