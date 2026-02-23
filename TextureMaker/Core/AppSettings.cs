using System.IO;
using System.Text.Json;

namespace TextureMaker.Core;

public class AppSettings
{
    public double PreviewLeft       { get; set; } = 100;
    public double PreviewTop        { get; set; } = 60;
    public double PreviewWidth      { get; set; } = 360;
    public double PreviewHeight     { get; set; } = 400;
    public bool   SnapLeft          { get; set; } = false;
    public bool   SnapRight         { get; set; } = false;
    public bool   SnapTop           { get; set; } = false;
    public bool   SnapBottom        { get; set; } = false;

    // ── Persistence ───────────────────────────────────────────────────

    private static readonly string _path = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "TextureMaker", "settings.json");

    private static readonly JsonSerializerOptions _opts = new() { WriteIndented = true };

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(_path))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_path), _opts)
                       ?? new AppSettings();
        }
        catch { /* ignore corrupt settings */ }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, JsonSerializer.Serialize(this, _opts));
        }
        catch { /* ignore write errors */ }
    }
}
