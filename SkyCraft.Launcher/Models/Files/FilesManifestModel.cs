using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SkyCraft.Launcher.Models.Files;

public class FilesManifestModel
{
    [JsonPropertyName("cleanDirectories")]
    public required List<string> CleanDirectories { get; init; }

    [JsonPropertyName("files")]
    public required List<FileModel> Files { get; init; }
}