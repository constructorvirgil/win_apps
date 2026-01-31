using System.Text.Json;
using System.IO;

namespace VirtualHidSimulator.App.Settings;

internal sealed class AppSettings
{
    public HotkeySettings ScreenshotHotkey { get; set; } = new();
    public bool ScreenshotIncludeCursor { get; set; } = true;
}

internal sealed class HotkeySettings
{
    public uint Modifiers { get; set; } = HotkeyModifiers.Control | HotkeyModifiers.Alt;
    public uint VirtualKey { get; set; } = (uint)System.Windows.Input.KeyInterop.VirtualKeyFromKey(System.Windows.Input.Key.S);
}

internal static class HotkeyModifiers
{
    public const uint Alt = 0x0001;
    public const uint Control = 0x0002;
    public const uint Shift = 0x0004;
    public const uint Win = 0x0008;
}

internal static class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static string SettingsPath
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "VirtualHidSimulator");
            return Path.Combine(dir, "settings.json");
        }
    }

    public static AppSettings Load()
    {
        try
        {
            var path = SettingsPath;
            if (!File.Exists(path)) return new AppSettings();

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public static void Save(AppSettings settings)
    {
        var path = SettingsPath;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(path, json);
    }
}
