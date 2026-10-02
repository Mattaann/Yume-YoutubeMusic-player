using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Yume.Services;
namespace Yume.Views;
public sealed partial class MainWindow
{
    private bool informationOpen;
    private async void Contact_Click(object sender, RoutedEventArgs e)
    {
        if (informationOpen || accountOpen) return;
        informationOpen = true;
        try
        {
            var content = new StackPanel { Spacing = 16 };
            content.Children.Add(new TextBlock { Text = "Found a bug, having a problem, or want to suggest an improvement? Visit my GitHub profile for the project and ways to get in touch.", TextWrapping = TextWrapping.Wrap });
            var link = new HyperlinkButton { Content = "github.com/Mattaann", HorizontalAlignment = HorizontalAlignment.Left };
            link.Click += (_, _) => WebViewService.OpenExternal("https://github.com/Mattaann");
            content.Children.Add(link);
            await new ContentDialog { XamlRoot = Root.XamlRoot, RequestedTheme = Root.RequestedTheme, Title = "Contact", Content = content, CloseButtonText = "Close" }.ShowAsync();
        }
        finally { informationOpen = false; }
    }
    private async Task ShowWelcomeAsync()
    {
        if (settings.Current.WelcomeShown || informationOpen) return;
        informationOpen = true;
        try
        {
            var content = new StackPanel { Spacing = 16 };
            void Paragraph(string text) => content.Children.Add(new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap });
            void Heading(string text) => content.Children.Add(new TextBlock { Text = text, FontSize = 18, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 0) });
            Paragraph("Yume is an open-source music player built to provide a cleaner and more focused YouTube Music experience.");
            Paragraph("For the best experience, we recommend that you:");
            Paragraph("Sign in with your Google account to access your playlists, library, recommendations, likes, and listening history.");
            Paragraph("Use YouTube Premium for the smoothest experience, including ad-free playback and fewer playback restrictions.");
            Heading("Open Source");
            Paragraph("Yume is completely open source. The project is built with transparency in mind, and you are welcome to explore the source code, contribute, or suggest improvements.");
            Heading("Found a bug or have feedback?");
            Paragraph("If something isn't working as expected, or if you have an idea that could make Yume better, head over to the Contact section.");
            Paragraph("Bug reports, feature requests, feedback, and general experiences are always appreciated.");
            Paragraph("Enjoy your music.");
            await new ContentDialog { XamlRoot = Root.XamlRoot, RequestedTheme = Root.RequestedTheme, Title = "Welcome to Yume 👋", Content = new ScrollViewer { Content = content, MaxHeight = 470, Padding = new Thickness(0, 0, 16, 0) }, CloseButtonText = "Let's go" }.ShowAsync();
            settings.Current.WelcomeShown = true;
            settings.Save();
        }
        finally { informationOpen = false; }
    }
}
