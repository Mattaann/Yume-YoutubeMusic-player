using Microsoft.Win32;
namespace Nocturne.WindowsIntegration;
public static class StartupService
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey);
        return key?.GetValue("Nocturne") is string value && !string.IsNullOrWhiteSpace(value);
    }
    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey);
        if (!enabled) { key.DeleteValue("Nocturne", false); return; }
        var executable = Environment.ProcessPath ?? throw new InvalidOperationException("Application location unavailable.");
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrEmpty(profile) && executable.StartsWith(profile + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) executable = "%USERPROFILE%" + executable[profile.Length..];
        key.SetValue("Nocturne", "\"" + executable + "\" --startup", RegistryValueKind.ExpandString);
    }
}
