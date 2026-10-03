using Microsoft.UI.Xaml;
namespace Yume;
public partial class App : Application
{
    private Views.MainWindow? window;
    private Mutex? instance;
    private EventWaitHandle? activation;
    private RegisteredWaitHandle? activationWait;
    public App()
    {
        var preferences = new Settings.SettingsService().Current;
        var culture = preferences.Language == "Match system" ? System.Globalization.CultureInfo.InstalledUICulture : System.Globalization.CultureInfo.GetCultureInfo("en-US");
        System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = culture;
        System.Globalization.CultureInfo.DefaultThreadCurrentCulture = culture;
        InitializeComponent();
    }
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        instance = new Mutex(true, @"Local\Yume.Desktop.Instance", out var first);
        activation = new EventWaitHandle(false, EventResetMode.AutoReset, @"Local\Yume.Desktop.Activate");
        if (!first) { activation.Set(); Exit(); return; }
        window = new Views.MainWindow();
        activationWait = ThreadPool.RegisterWaitForSingleObject(activation, (_, _) => window.DispatcherQueue.TryEnqueue(() => window.RestoreMain()), null, Timeout.Infinite, false);
        window.Closed += (_, _) => { activationWait?.Unregister(null); activation.Dispose(); instance.ReleaseMutex(); instance.Dispose(); };
        window.Activate();
    }
}
