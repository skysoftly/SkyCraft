using System.Text.Json.Serialization;

namespace SkyCraft.Launcher.Models.Build;

public class BuildAssetsModel
{
    [JsonPropertyName("icon")]
    public required string Icon { get; init; }

    [JsonPropertyName("banner")]
    public required string Banner { get; init; }
}