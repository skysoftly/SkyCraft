using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using SkyCraft.Launcher.Models.Manifest;
using SkyCraft.Launcher.Services.Settings;
using SkyCraft.Shared.Helpers;

namespace SkyCraft.Launcher.Services.Build;

public class ManifestService
{
    private const string ManifestUrl = "https://storage.yandexcloud.net/skycraft/game/manifest.json";
    
    private readonly HttpClient _httpClient;

    public ManifestService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ManifestModel> GetAsync()
    {
        Logger.Info("Downloading manifest.");

        var json = await _httpClient.GetStringAsync(ManifestUrl);
        
        var manifest = JsonSerializer.Deserialize<ManifestModel>(json);
        
        if (manifest == null)
            throw new Exception("Failed to deserialize manifest.json.");
        
        Logger.Info($"Loaded {manifest.Builds.Count} build(s).");
        
        return manifest;
    }
}