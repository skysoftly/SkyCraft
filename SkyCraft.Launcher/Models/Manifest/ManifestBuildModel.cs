using System.Text.Json.Serialization;

namespace SkyCraft.Launcher.Models.Manifest;

public class ManifestBuildModel
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("url")]
    public required string Url { get; init; }
}