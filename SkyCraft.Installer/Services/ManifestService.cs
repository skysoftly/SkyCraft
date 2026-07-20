using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using SkyCraft.Installer.Models;
using SkyCraft.Shared.Helpers;

namespace SkyCraft.Installer.Services;

public class ManifestService
{
    private readonly HttpClient _httpClient;

    private const string ManifectUrl = "https://storage.yandexcloud.net/skycraft/launcher/launcher.json";
    
    public ManifestService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<LauncherManifest> GetAsync()
    {
        Logger.Info("Downloading launcher manifest.");
        var json = await _httpClient.GetStringAsync(ManifectUrl);
        var manifest = JsonSerializer.Deserialize<LauncherManifest>(json);

        if (manifest == null)
            throw new Exception("Failed to deserialize launcher.json");
        
        Logger.Info("Manifest loaded.");
        
        
        return manifest;
    }
}