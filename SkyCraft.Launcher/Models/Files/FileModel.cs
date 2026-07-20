using System.Text.Json.Serialization;

namespace SkyCraft.Launcher.Models.Files;

public class FileModel
{
    [JsonPropertyName("path")]
    public required string Path { get; init; }

    [JsonPropertyName("size")]
    public required long Size { get; init; }

    [JsonPropertyName("sha256")]
    public required string Sha256 { get; init; }
}