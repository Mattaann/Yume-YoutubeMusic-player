using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel.DataTransfer;
using Yume.Models;
namespace Yume.Views;
public sealed partial class MainWindow
{
    private bool accountOpen;
    private bool libraryLoading;
    private readonly TextBlock libraryStatus = new() { TextWrapping = TextWrapping.Wrap, FontSize = 11, Opacity = 0.7, Margin = new Thickness(8, 4, 4, 4) };
    private async Task ShowAccountAsync()
    {
        if (accountOpen || informationOpen) return;
        accountOpen = true;
        try
        {
            var panel = new StackPanel { Spacing = 14, MinWidth = 320 };
            var name = new TextBlock { Text = "Loading account…", FontSize = 22, TextWrapping = TextWrapping.Wrap };
            var email = new TextBlock { TextWrapping = TextWrapping.Wrap, Opacity = 0.75 };
            var photo = new Image { Width = 80, Height = 80, HorizontalAlignment = HorizontalAlignment.Left };
            panel.Children.Add(photo); panel.Children.Add(name); panel.Children.Add(email);
            panel.Children.Add(new TextBlock { Text = "Your YouTube Music account in Yume", Opacity = 0.65 });
            var dialog = new ContentDialog { XamlRoot = Root.XamlRoot, RequestedTheme = Root.RequestedTheme, Title = "Account", Content = panel, CloseButtonText = "Close", SecondaryButtonText = "YouTube Music menu", PrimaryButtonText = "Sign out", IsPrimaryButtonEnabled = false };
            async Task LoadAsync()
            {
                try
                {
                    var info = web.Bridge == null ? null : await web.Bridge.ReadAccountAsync();
                    if (!accountOpen) return;
                    name.Text = info?.Name ?? "Account unavailable";
                    email.Text = info?.Email ?? "Open the YouTube Music menu to check your account.";
                    dialog.IsPrimaryButtonEnabled = info?.SignedIn == true;
                    if (info != null && Uri.TryCreate(info.Photo, UriKind.Absolute, out var uri) && uri.Scheme == "https" && (uri.Host.EndsWith(".googleusercontent.com", StringComparison.OrdinalIgnoreCase) || uri.Host.EndsWith(".ggpht.com", StringComparison.OrdinalIgnoreCase)))
                    {
                        using var client = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(8), MaxResponseContentBufferSize = 2 * 1024 * 1024 };
                        var bytes = await client.GetByteArrayAsync(uri);
                        using var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream();
                        using (var writer = new Windows.Storage.Streams.DataWriter(stream.GetOutputStreamAt(0))) { writer.WriteBytes(bytes); await writer.StoreAsync(); }
                        stream.Seek(0);
                        var bitmap = new BitmapImage(); await bitmap.SetSourceAsync(stream);
                        if (accountOpen) photo.Source = bitmap;
                    }
                }
                catch { email.Text = "Could not load all account details. Use the YouTube Music menu."; }
            }
            var shown = dialog.ShowAsync();
            _ = LoadAsync();
            var result = await shown;
            if (result == ContentDialogResult.Primary && web.Bridge != null)
            {
                SidebarPlaylists.Children.Clear();
                await web.Bridge.SignOutAsync();
            }
            else if (result == ContentDialogResult.Secondary) await CommandAsync("account");
        }
        catch { }
        finally { accountOpen = false; }
    }
    private void Queue_DragItemsStarting(object sender, DragItemsStartingEventArgs args)
    {
        if (args.Items.FirstOrDefault() is not QueueItem item) { args.Cancel = true; return; }
        args.Data.SetText("yume-queue:" + item.Key);
        args.Data.RequestedOperation = DataPackageOperation.Copy;
    }
    private async Task RefreshPlaylistsAsync()
    {
        if (libraryLoading || web?.Bridge == null || !settings.Current.ShowSidebarPlaylists) return;
        libraryLoading = true;
        try
        {
            libraryStatus.Text = "Loading playlists…";
            SidebarPlaylists.Children.Clear(); SidebarPlaylists.Children.Add(libraryStatus);
            var result = await web.Bridge.ReadLibraryAsync();
            SidebarPlaylists.Children.Clear();
            foreach (var playlist in result?.Items ?? [])
            {
                if (string.IsNullOrWhiteSpace(playlist.Id) || playlist.Title.Length > 200) continue;
                var button = new Button { Content = new TextBlock { Text = playlist.Title, TextTrimming = TextTrimming.CharacterEllipsis }, Tag = "/playlist?list=" + Uri.EscapeDataString(playlist.Id), HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Left, Padding = new Thickness(10, 7, 6, 7), AllowDrop = true };
                ToolTipService.SetToolTip(button, playlist.Title);
                button.Click += Navigate_Click;
                button.DragOver += (_, args) =>
                {
                    if (!args.DataView.Contains(StandardDataFormats.Text)) return;
                    args.AcceptedOperation = DataPackageOperation.Copy;
                    args.DragUIOverride.Caption = "Add to " + playlist.Title;
                    args.DragUIOverride.IsCaptionVisible = true;
                };
                button.Drop += async (_, args) =>
                {
                    var deferral = args.GetDeferral();
                    try
                    {
                        var text = await args.DataView.GetTextAsync();
                        if (text.StartsWith("yume-queue:", StringComparison.Ordinal))
                        {
                            libraryStatus.Text = "Adding song…";
                            libraryStatus.Text = await web.Bridge.AddQueueSongAsync(playlist.Id, text[11..]) ?? "Could not add the song.";
                            return;
                        }
                        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri) || uri.Scheme != "https" || uri.Host != "music.youtube.com" || uri.AbsolutePath != "/watch") return;
                        var query = new Windows.Foundation.WwwFormUrlDecoder(uri.Query);
                        var id = query.GetFirstValueByName("v");
                        libraryStatus.Text = "Adding song…";
                        libraryStatus.Text = await web.Bridge.AddSongAsync(playlist.Id, id) ?? "Could not add the song. Please try again.";
                    }
                    catch { libraryStatus.Text = "Could not add the song. Drag a song from the music view."; }
                    finally { deferral.Complete(); }
                };
                SidebarPlaylists.Children.Add(button);
            }
            libraryStatus.Text = result?.Error ?? "Could not load playlists.";
            if (result?.Items.Length == 0 && string.IsNullOrEmpty(libraryStatus.Text)) libraryStatus.Text = "No playlists found.";
            SidebarPlaylists.Children.Add(libraryStatus);
            var refresh = new Button { Content = "Refresh playlists", FontSize = 11, Opacity = 0.7 };
            refresh.Click += async (_, _) => await RefreshPlaylistsAsync();
            SidebarPlaylists.Children.Add(refresh);
            UpdateNavigation(navigationPath);
        }
        catch { libraryStatus.Text = "Could not load playlists. Try again when YouTube Music is ready."; }
        finally { libraryLoading = false; }
    }
}
