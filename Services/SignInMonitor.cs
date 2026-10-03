using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
namespace Yume.Services;
public sealed class SignInMonitor : IDisposable
{
    private readonly Panel host;
    private readonly WebView2 probe = new() { Width = 1, Height = 1, Opacity = 0, IsHitTestVisible = false, IsTabStop = false };
    private readonly CancellationTokenSource stop = new();
    private bool disposed;
    public SignInMonitor(Panel host) { this.host = host; host.Children.Add(probe); }
    public async Task<bool> WaitAsync(CoreWebView2Environment environment)
    {
        try
        {
            await probe.EnsureCoreWebView2Async(environment);
            if (disposed) return false;
            var core = probe.CoreWebView2;
            core.IsMuted = true;
            core.Settings.AreDevToolsEnabled = false;
            core.Settings.IsPasswordAutosaveEnabled = false;
            core.NavigationStarting += (_, e) => { if (!NavigationPolicy.IsMusic(e.Uri)) e.Cancel = true; };
            core.NewWindowRequested += (_, e) => e.Handled = true;
            core.PermissionRequested += (_, e) => e.State = CoreWebView2PermissionState.Deny;
            core.DownloadStarting += (_, e) => e.Cancel = true;
            for (var attempt = 0; attempt < 60 && !disposed; attempt++)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stop.Token);
                var loaded = new TaskCompletionSource<bool>();
                void Completed(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs args) => loaded.TrySetResult(args.IsSuccess);
                core.NavigationCompleted += Completed;
                try
                {
                    core.Navigate("https://music.youtube.com/");
                    var success = await loaded.Task.WaitAsync(TimeSpan.FromSeconds(20), stop.Token);
                    if (success && NavigationPolicy.IsMusic(core.Source))
                    {
                        var signedIn = await core.ExecuteScriptAsync("window.ytcfg?.get?.('LOGGED_IN') === true");
                        if (!disposed && signedIn == "true") return true;
                    }
                }
                catch (TimeoutException) { core.Stop(); }
                finally { core.NavigationCompleted -= Completed; }
            }
        }
        catch (Exception) { }
        return false;
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        stop.Cancel();
        probe.Close();
        host.Children.Remove(probe);
    }
}
