using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using SkyCraft.Launcher.Models.Build;
using SkyCraft.Shared.Helpers;

namespace SkyCraft.Launcher.Services.Build;

public class BuildService
{
    private readonly HttpClient _httpClient;
    private readonly ManifestService _manifestService;

    public BuildService(HttpClient httpClient, ManifestService manifestService)
    {
        _httpClient = httpClient;
        _manifestService = manifestService;
    }

    public async Task<BuildModel> GetAsync(string url)
    {
        var json = await _httpClient.GetStringAsync(url);

        var build = JsonSerializer.Deserialize<BuildModel>(json);
        
        if (build == null)
            throw new Exception("Failed to deserialize build.json.");

        Logger.Info($"Loaded build {build.Info.Name}.");

        return build;
    }

    public async Task<List<BuildModel>> GetAllAsync()
    {
        var builds = new List<BuildModel>();
        var manifest = await _manifestService.GetAsync();

        foreach (var entry in manifest.Builds)
        {
            Logger.Info($"Downloading build: {entry.Id}.");
            var build = await GetAsync(entry.Url);
            
            builds.Add(build);
        }
        
        return builds;
    }
}