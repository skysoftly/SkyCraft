using System.Text.Json.Serialization;

namespace SkyCraft.Launcher.Models.Build;

public class ServerModel
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("host")]
    public required string Host { get; init; }

    [JsonPropertyName("port")]
    public required int Port { get; init; }
}