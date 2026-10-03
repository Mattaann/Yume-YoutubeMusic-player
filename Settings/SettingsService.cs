using System.Text.Json;
namespace Yume.Settings;
public sealed class Preferences
{
    public bool WelcomeShown { get; set; }
    public bool ShowSidebarPlaylists { get; set; } = true;
    public bool ShowLikedMusic { get; set; } = true;
    public bool ShowAlbums { get; set; } = true;
    public bool ShowArtists { get; set; } = true;
    public bool ShowPodcasts { get; set; } = true;
    public double[] EqualizerGains { get; set; } = [];
    public string EqualizerPreset { get; set; } = "Flat";
    public bool EqualizerEnabled { get; set; }
    public double BassGain { get; set; }
    public double MidGain { get; set; }
    public double TrebleGain { get; set; }
    public bool HideLikes { get; set; } = true;
    public bool ShowVideo { get; set; }
    public bool Compact { get; set; }
    public string Language { get; set; } = "English";
    public bool RememberVolume { get; set; } = true;
    public bool MediaKeys { get; set; } = true;
    public double Volume { get; set; } = 0.7;
    public int Width { get; set; } = 1280;
    public int Height { get; set; } = 860;
    public string Theme { get; set; } = "Yume";
    public int ThemeDefaultsVersion { get; set; }
    public bool ArtworkAccent { get; set; } = true;
    public bool VisualTheme { get; set; } = true;
    public bool Animations { get; set; } = true;
    public bool WindowsMediaIntegration { get; set; } = true;
    public bool StartMinimized { get; set; }
    public bool AlwaysOnTop { get; set; }
    public int? X { get; set; }
    public int? Y { get; set; }
}
public sealed class SettingsService
{
    public static string DataDirectory { get; } = ResolveDataDirectory();
    private static string ResolveDataDirectory()
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var legacy = Path.Combine(local, "Nocturne");
        return Directory.Exists(legacy) ? legacy : Path.Combine(local, "Yume");
    }
    private string FilePath => Path.Combine(DataDirectory, "settings.json");
    public Preferences Current { get; }
    public SettingsService()
    {
        try { Current = JsonSerializer.Deserialize<Preferences>(File.ReadAllText(FilePath)) ?? new(); }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException) { Current = new(); }
        if (Current.EqualizerGains == null || Current.EqualizerGains.Length != 6) Current.EqualizerGains = new[] { Current.BassGain, Current.BassGain, Current.MidGain, Current.MidGain, Current.TrebleGain, Current.TrebleGain };
        Current.EqualizerGains = Current.EqualizerGains.Select(value => double.IsFinite(value) ? Math.Clamp(value, -12, 12) : 0).ToArray();
        if (Current.EqualizerGains.Any(value => value != 0) && Current.EqualizerPreset == "Flat") Current.EqualizerPreset = "Custom";
        Current.BassGain = double.IsFinite(Current.BassGain) ? Math.Clamp(Current.BassGain, -12, 12) : 0;
        Current.MidGain = double.IsFinite(Current.MidGain) ? Math.Clamp(Current.MidGain, -12, 12) : 0;
        Current.TrebleGain = double.IsFinite(Current.TrebleGain) ? Math.Clamp(Current.TrebleGain, -12, 12) : 0;
        Current.Volume = double.IsFinite(Current.Volume) ? Math.Clamp(Current.Volume, 0, 1) : 0.7;
        if (Current.ThemeDefaultsVersion < 1) { Current.Theme = "Yume"; Current.ThemeDefaultsVersion = 1; Save(); }
        Current.Theme = Current.Theme is "Dark" or "Light" or "Yume" ? Current.Theme : "Yume";
    }
    public bool Save()
    {
        try
        {
            Directory.CreateDirectory(DataDirectory);
            File.WriteAllText(FilePath + ".tmp", JsonSerializer.Serialize(Current));
            File.Move(FilePath + ".tmp", FilePath, true);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return false; }
    }
}
