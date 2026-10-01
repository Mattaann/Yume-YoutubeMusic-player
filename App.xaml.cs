using Microsoft.UI.Xaml;
namespace Yume;
public partial class App : Application
{
    private Views.MainWindow? window;
    public App() { InitializeComponent(); }
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        window = new Views.MainWindow();
        window.Activate();
    }
}
