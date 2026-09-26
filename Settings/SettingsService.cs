using System.Text.Json;
namespace Nocturne.Settings;
public sealed class Preferences
{
    public bool HideLikes { get; set; } = true;
    public bool ShowVideo { get; set; }
    public bool Compact { get; set; }
    public bool MinimizeToTray { get; set; } = true;
    public bool RememberVolume { get; set; } = true;
    public bool MediaKeys { get; set; } = true;
    public double Volume { get; set; } = 0.7;
    public int Width { get; set; } = 1280;
    public int Height { get; set; } = 860;
    public string Theme { get; set; } = "Dark";
    public bool ArtworkAccent { get; set; } = true;
    public bool Animations { get; set; } = true;
    public bool WindowsMediaIntegration { get; set; } = true;
    public bool StartMinimized { get; set; }
    public bool AlwaysOnTop { get; set; }
    public int? X { get; set; }
    public int? Y { get; set; }
    public int MiniWidth { get; set; } = 420;
    public int MiniHeight { get; set; } = 252;
    public int? MiniX { get; set; }
    public int? MiniY { get; set; }
    public bool MiniAlwaysOnTop { get; set; } = true;
}
public sealed class SettingsService
{
    public static string DataDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Nocturne");
    private string FilePath => Path.Combine(DataDirectory, "settings.json");
    public Preferences Current { get; }
    public SettingsService()
    {
        try { Current = JsonSerializer.Deserialize<Preferences>(File.ReadAllText(FilePath)) ?? new(); }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException) { Current = new(); }
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
