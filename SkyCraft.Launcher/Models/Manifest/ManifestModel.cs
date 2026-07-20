using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SkyCraft.Launcher.Models.Manifest;

public class ManifestModel
{
    [JsonPropertyName("builds")]
    public required List<ManifestBuildModel> Builds { get; init; }
}