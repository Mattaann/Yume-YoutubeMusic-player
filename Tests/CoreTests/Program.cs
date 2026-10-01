using Nocturne.Utilities;
using Nocturne.Services;
void Require(bool condition) { if (!condition) throw new Exception("Validation failed"); }
Require(ArtworkPalette.Extract([]) == (184, 216, 210));
Require(ArtworkPalette.Extract([128, 128, 128, 255]) == (184, 216, 210));
var red = ArtworkPalette.Extract([20, 20, 240, 255, 20, 20, 240, 255, 240, 20, 20, 255]);
Require(red.R > red.G && red.R > red.B && red.R <= 235 && red.G >= 100);
var blue = ArtworkPalette.Extract([240, 20, 20, 255]);
Require(blue.B > blue.R);
Require(ArtworkPolicy.GetUri("https://lh3.googleusercontent.com/image") != null);
foreach (var address in new[] { "http://lh3.googleusercontent.com/image", "https://lh3.googleusercontent.com.evil.example/image", "https://lh3.googleusercontent.com:444/image", "file:///image" }) Require(ArtworkPolicy.GetUri(address) == null);
Require(NavigationPolicy.IsMusic("https://music.youtube.com/"));
Require(!NavigationPolicy.IsMusic("https://accounts.google.com/"));
Require(!NavigationPolicy.IsInternal("https://music.youtube.com.evil.example/"));
Require(!NavigationPolicy.CanOpenExternal("file:///test"));
Console.WriteLine("PASS: artwork palette, neutral fallback, artwork origin restrictions, navigation isolation");
