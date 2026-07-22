using System.Text.Json.Serialization;

namespace SkyCraft.Launcher.Models.Build;

public class ModModel
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }
}