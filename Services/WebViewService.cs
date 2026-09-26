using System.Diagnostics;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Nocturne.Settings;
using Nocturne.WebView;
namespace Nocturne.Services;
public sealed class WebViewService
{
    private readonly WebView2 view;
    private readonly SettingsService settings;
    private bool initializing;
    public YouTubeMusicBridge? Bridge { get; private set; }
    public event Action<bool, string>? Loading;
    public event Action? Ready;
    public WebViewService(WebView2 view, SettingsService settings) { this.view = view; this.settings = settings; }
    public async Task InitializeAsync()
    {
        if (initializing || Bridge != null) return;
        initializing = true;
        try
        {
            var environment = await CoreWebView2Environment.CreateWithOptionsAsync(null, Path.Combine(SettingsService.DataDirectory, "WebView2"), null);
            await view.EnsureCoreWebView2Async(environment);
            var core = view.CoreWebView2;
            core.Settings.AreDevToolsEnabled = false;
            core.Settings.IsStatusBarEnabled = false;
            core.Settings.AreDefaultContextMenusEnabled = false;
            core.Settings.IsPasswordAutosaveEnabled = false;
            core.Settings.IsGeneralAutofillEnabled = false;
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
                try { await Bridge.InjectAsync(settings.Current); Loading?.Invoke(false, ""); }
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
    public static void OpenExternal(string address)
    {
        if (!NavigationPolicy.CanOpenExternal(address)) return;
        try { Process.Start(new ProcessStartInfo(address) { UseShellExecute = true }); } catch { }
    }
}
