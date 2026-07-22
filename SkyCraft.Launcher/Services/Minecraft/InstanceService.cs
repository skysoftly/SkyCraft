using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SkyCraft.Launcher.Helpers;
using SkyCraft.Launcher.Models;
using SkyCraft.Launcher.Models.Build;
using SkyCraft.Launcher.Models.Files;
using SkyCraft.Launcher.Services.Settings;
using SkyCraft.Shared.Helpers;

namespace SkyCraft.Launcher.Services.Minecraft;

public class InstanceService
{
    private readonly SettingsService _settings;
    private readonly HttpClient _httpClient;

    public InstanceService(SettingsService settings, HttpClient httpClient)
    {
        _settings = settings;
        _httpClient = httpClient;
    }

    public async Task InstallAsync(BuildModel build, InstallContext installContext)
    {
        installContext.Status = "Проверка файлов";
        string instancePath = PathHelper.GetInstanceRoot(_settings.Settings.InstallDirectory, build.Info.Id);
        var manifest = await GetFilesManifestAsync(build.Files.Url);
        
        var allowedFiles = new List<string>();

        double i = 0;
        foreach (var file in manifest.Files)
        {
            installContext.File = file.Path;
            string relativePath = await DownloadFile(build, file, instancePath);
            i++;
            installContext.Progress = (i / manifest.Files.Count) * 100;
            allowedFiles.Add(relativePath.Replace('\\', '/'));
        }
        
        await CleanDirectoryAsync(instancePath, allowedFiles, manifest.CleanDirectories);
    }

    private async Task<FilesManifestModel> GetFilesManifestAsync(string url)
    {
        var json = await _httpClient.GetStringAsync(url);

        var manifest = JsonSerializer.Deserialize<FilesManifestModel>(json);

        if (manifest == null)
            throw new Exception("Failed to deserialize files.json.");

        Logger.Info($"Loaded {manifest.Files.Count} files.");

        return manifest;
    }

    private async Task<string> DownloadFile(BuildModel build, FileModel file, string instancePath)
    {
        string url = $"{build.Files.BaseUrl}{file.Path}";
        
        string relativePath = file.Path;

        string dir = Path.GetDirectoryName(relativePath)!;

        if (file.Optional)
        {
            string name = Path.GetFileName(relativePath);
            relativePath = Path.Combine(dir, $"optional-{name}");
        }

        string savePath = Path.Combine(instancePath, relativePath);
        string? saveDir = Path.GetDirectoryName(savePath);

        if (!string.IsNullOrEmpty(saveDir))
            Directory.CreateDirectory(saveDir);

        if (File.Exists(savePath))
        {
            if (file.Sha256 is null || await HashHelper.VerifySha256Async(savePath, file.Sha256))
            {
                return relativePath;
            }
        }
        else if (file.Optional && File.Exists(savePath + ".disabled"))
        {
            if (file.Sha256 is null || await HashHelper.VerifySha256Async(savePath + ".disabled", file.Sha256))
            {
                return relativePath;
            }
        }
        try
        {
            using var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            using var fileStream = File.Create(savePath);
            await stream.CopyToAsync(fileStream);
        }
        catch (Exception e)
        {
            Logger.Error(e.ToString());
        }
        
        return relativePath;
    }
    
    public async Task CleanDirectoryAsync(string instancePath, List<string> allowedFiles, List<string> cleanFolders)
    {
        
        if (cleanFolders.Count == 0)
            return;
    
        Logger.Info($"Clean folders: {string.Join(", ", cleanFolders)}");
    
        // Создаем HashSet для быстрого поиска разрешенных файлов
        var allowedSet = new HashSet<string>(allowedFiles, StringComparer.OrdinalIgnoreCase);
    
        foreach (var folder in cleanFolders)
        {
            string folderPath = Path.Combine(instancePath, folder);
            if (!Directory.Exists(folderPath))
                continue;
        
            Logger.Info($"Clean folder: {folder}");
        
            // Получаем все файлы рекурсивно
            var allFiles = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);
            var allDirs = Directory.GetDirectories(folderPath, "*", SearchOption.AllDirectories);
        
            int deletedCount = 0;
        
            foreach (var file in allFiles)
            {
                // Получаем относительный путь от папки instancePath
                string relativePath = Path.GetRelativePath(instancePath, file)
                    .Replace('\\', '/'); // Нормализуем разделители
            
                // Проверяем, есть ли файл в списке разрешенных
                if (!allowedSet.Contains(relativePath) && !Path.GetFileName(file).StartsWith("user_file-") && !Path.GetFileName(file).EndsWith(".disabled"))
                {
                    Logger.Info($"Delete: {relativePath}");
                    File.Delete(file);
                    deletedCount++;
                }
            }
        
            Logger.Info($"Clear {deletedCount} files in {folder}");
        }
    }
}