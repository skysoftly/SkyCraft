using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SkyCraft.Launcher.Models.Build;

public class BuildModel
{
    [JsonPropertyName("info")]
    public required BuildInfoModel Info { get; init; }

    [JsonPropertyName("assets")]
    public required BuildAssetsModel Assets { get; init; }

    [JsonPropertyName("minecraft")]
    public required MinecraftModel Minecraft { get; init; }

    [JsonPropertyName("servers")]
    public required List<ServerModel> Servers { get; init; }

    [JsonPropertyName("files")]
    public required BuildFilesModel Files { get; init; }
    
    [JsonPropertyName("mods")]
    public required List<ModModel> Mods { get; init; }
}