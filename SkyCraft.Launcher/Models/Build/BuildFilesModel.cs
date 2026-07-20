using System.Text.Json.Serialization;

namespace SkyCraft.Launcher.Models.Build;

public class BuildFilesModel
{
    [JsonPropertyName("url")]
    public required string Url { get; init; }

    [JsonPropertyName("baseUrl")]
    public required string BaseUrl { get; init; }
}