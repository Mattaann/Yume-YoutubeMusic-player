using Microsoft.UI.Xaml;
namespace Nocturne;
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
