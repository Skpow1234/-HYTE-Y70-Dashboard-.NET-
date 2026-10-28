using System.Text.Json;

namespace HyteY70;

public sealed class AppConfig
{
    public int Fps { get; set; } = 20;

    /// <summary>auto | stats | media</summary>
    public string Mode { get; set; } = "auto";

    public string Gif { get; set; } = "";
    public string Media { get; set; } = "";

    public int Rotate { get; set; } = 0;
    public bool FlipHorizontal { get; set; } = false;
    public bool FlipVertical { get; set; } = false;
    public string Fit { get; set; } = "contain";
    public double Zoom { get; set; } = 1.0;

    public bool ShowClock { get; set; } = true;
    public bool ShowCpu { get; set; } = true;
    public bool ShowGpu { get; set; } = true;
    public bool ShowRam { get; set; } = true;

    public bool StartHidden { get; set; } = false;
    public bool RunAtLogin { get; set; } = false;

    public bool UseCoverFit =>
        string.Equals(Fit, "cover", StringComparison.OrdinalIgnoreCase);

    public static string ConfigPath(string baseDir) => System.IO.Path.Combine(baseDir, "config.json");

    public static AppConfig Load(string baseDir)
    {
        string path = ConfigPath(baseDir);
        if (!System.IO.File.Exists(path)) return new AppConfig();
        return JsonSerializer.Deserialize<AppConfig>(System.IO.File.ReadAllText(path), new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new AppConfig();
    }

    public string? ResolveMedia(string baseDir)
    {
        foreach (string candidate in new[] { Gif, Media })
        {
            if (string.IsNullOrWhiteSpace(candidate)) continue;
            string path = System.IO.Path.IsPathRooted(candidate) ? candidate : System.IO.Path.Combine(baseDir, candidate);
            if (System.IO.File.Exists(path)) return path;
        }
        return null;
    }

    public bool UseMedia(string? mediaPath)
    {
        string mode = (Mode ?? "auto").Trim().ToLowerInvariant();
        return mode switch
        {
            "stats" => false,
            "media" => mediaPath is not null,
            _ => mediaPath is not null
        };
    }
}
