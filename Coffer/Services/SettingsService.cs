using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Coffer.Services;

public class AppSettings
{
    [JsonPropertyName("autoLockMinutes")]
    public int AutoLockMinutes { get; set; } = 5;

    [JsonPropertyName("clipboardClearSeconds")]
    public int ClipboardClearSeconds { get; set; } = 30;
}

public class SettingsService
{
    private readonly string _path;

    public SettingsService(string dataDirectory)
    {
        _path = Path.Combine(dataDirectory, "settings.json");
    }

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_path))
                return new AppSettings();

            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_path));
            if (settings is null)
                return new AppSettings();

            settings.AutoLockMinutes = Math.Clamp(settings.AutoLockMinutes, 0, 120);
            settings.ClipboardClearSeconds = Math.Clamp(settings.ClipboardClearSeconds, 10, 120);
            return settings;
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);

            // Write atomically so a crash mid-save cannot truncate the settings.
            string tempPath = _path + ".tmp";
            File.WriteAllText(tempPath, JsonSerializer.Serialize(settings));
            File.Move(tempPath, _path, overwrite: true);
        }
        catch
        {
            // Settings are a convenience; failing to write them must not break the vault.
        }
    }
}