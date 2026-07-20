using System.Text.Json.Serialization;

namespace SkyCraft.Launcher.Models.Build;

public class MinecraftModel
{
    [JsonPropertyName("version")]
    public required string Version { get; init; }

    [JsonPropertyName("loader")]
    public required string Loader { get; init; }

    [JsonPropertyName("loaderVersion")]
    public required string LoaderVersion { get; init; }

    [JsonPropertyName("javaVersion")]
    public required int JavaVersion { get; init; }

    [JsonPropertyName("recommendedRamMb")]
    public required int RecommendedRamMb { get; init; }
}