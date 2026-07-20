using System.Text.Json.Serialization;

namespace SkyCraft.Installer.Models;

public class LauncherManifest
{
    [JsonPropertyName("url")]
    public required string Url { get; init; }
    
    [JsonPropertyName("sha256")]
    public required string Sha256 { get; init; }
}