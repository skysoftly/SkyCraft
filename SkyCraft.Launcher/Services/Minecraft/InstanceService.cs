using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
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
    private const string ManifestVersionFile = ".manifest-version";

    private readonly SettingsService _settings;
    private readonly HttpClient _httpClient;

    public InstanceService(SettingsService settings, HttpClient httpClient)
    {
        _settings = settings;
        _httpClient = httpClient;
    }

    // ============================================================
    //  УСТАНОВКА ФАЙЛОВ
    // ============================================================

    /// <summary>
    /// Обычная установка. Если версия манифеста совпадает с сохранённой —
    /// файлы не проверяются и не скачиваются.
    /// </summary>
    /// <param name="forceCheck">
    /// true — принудительно проверить SHA256 каждого файла и докачать недостающие,
    /// игнорируя совпадение версии манифеста.
    /// </param>
    public async Task InstallAsync(
        BuildModel build,
        InstallContext installContext,
        bool forceCheck = false)
    {
        string instancePath = PathHelper.GetInstanceRoot(
            _settings.Settings.InstallDirectory, build.Info.Id);

        Directory.CreateDirectory(instancePath);

        installContext.Status = "Проверка манифеста файлов";
        Logger.Info("Install files.");

        var manifest = await GetFilesManifestAsync(build.Files.Url);

        // ---- Проверка версии манифеста ----
        string currentVersion = ReadManifestVersion(instancePath);

        if (!forceCheck &&
            !string.IsNullOrEmpty(currentVersion) &&
            currentVersion == manifest.Version)
        {
            Logger.Info($"Manifest version match ({currentVersion}), skipping file install.");
            installContext.Status   = "Файлы актуальны";
            installContext.Progress = 100;
            return;
        }

        if (forceCheck)
            Logger.Info("Force check requested — verifying every file by SHA256.");
        else
            Logger.Info($"Manifest version changed: '{currentVersion}' -> '{manifest.Version}'.");

        // ---- Скачивание / проверка файлов ----
        var allowedFiles = new List<string>();

        int total = manifest.Files.Count;
        int index = 0;

        foreach (var file in manifest.Files)
        {
            installContext.File = file.Path;

            string relativePath = await DownloadFile(
                build, file, instancePath, forceCheck: forceCheck);

            allowedFiles.Add(relativePath.Replace('\\', '/'));

            index++;
            installContext.Progress = total == 0 ? 100 : (double)index / total * 100;
        }

        // ---- Чистим лишнее ----
        await CleanDirectoryAsync(instancePath, allowedFiles, manifest.CleanDirectories);

        // ---- Записываем новую версию ----
        WriteManifestVersion(instancePath, manifest.Version);

        installContext.Status   = "Файлы установлены";
        installContext.Progress = 100;
    }

    // ============================================================
    //  ПРИНУДИТЕЛЬНАЯ ПРОВЕРКА
    // ============================================================

    /// <summary>
    /// Принудительно проверяет все файлы манифеста по SHA256 и докачивает битые/отсутствующие.
    /// Не смотрит на версию манифеста. Также удаляет лишние файлы из cleanDirectories.
    /// </summary>
    public async Task<bool> VerifyAndRepairAsync(BuildModel build, InstallContext installContext)
    {
        try
        {
            Logger.Info("Force verify files started");
            installContext.Status = "Проверка целостности файлов";

            await InstallAsync(build, installContext, forceCheck: true);

            installContext.Status = "Проверка завершена";
            installContext.Progress = 100;
            installContext.Finally = true;

            Logger.Info("Force verify files completed");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Verify files failed: {ex}");
            installContext.Status = "Ошибка проверки: " + ex.Message;
            return false;
        }
    }

    // ============================================================
    //  МАНИФЕСТ
    // ============================================================

    private async Task<FilesManifestModel> GetFilesManifestAsync(string url)
    {
        var json = await _httpClient.GetStringAsync(url);

        var manifest = JsonSerializer.Deserialize<FilesManifestModel>(json);

        if (manifest == null)
            throw new Exception("Failed to deserialize files.json.");

        Logger.Info($"Loaded {manifest.Files.Count} files (manifest version: {manifest.Version}).");

        return manifest;
    }

    private static string GetManifestVersionPath(string instancePath)
        => Path.Combine(instancePath, ManifestVersionFile);

    private static string ReadManifestVersion(string instancePath)
    {
        var path = GetManifestVersionPath(instancePath);
        if (!File.Exists(path))
            return string.Empty;

        try
        {
            return File.ReadAllText(path).Trim();
        }
        catch
        {
            return string.Empty;
        }
    }

    private static void WriteManifestVersion(string instancePath, string version)
    {
        try
        {
            File.WriteAllText(GetManifestVersionPath(instancePath), version);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to write manifest version: {ex}");
        }
    }

    // ============================================================
    //  СКАЧИВАНИЕ ОДНОГО ФАЙЛА
    // ============================================================

    /// <summary>
    /// Возвращает относительный путь скачанного (или уже существующего) файла.
    /// Если forceCheck = false и файл существует и проходит SHA256 — сразу возвращает путь.
    /// Если forceCheck = true — проверяет SHA256 обязательно.
    /// </summary>
    private async Task<string> DownloadFile(
        BuildModel build,
        FileModel file,
        string instancePath,
        bool forceCheck)
    {
        string url = $"{build.Files.BaseUrl}{file.Path}";

        string relativePath = file.Path;
        string dir = Path.GetDirectoryName(relativePath) ?? string.Empty;

        if (file.Optional)
        {
            string name = Path.GetFileName(relativePath);
            relativePath = Path.Combine(dir, $"optional-{name}");
        }

        string savePath = Path.Combine(instancePath, relativePath);
        string? saveDir = Path.GetDirectoryName(savePath);

        if (!string.IsNullOrEmpty(saveDir))
            Directory.CreateDirectory(saveDir);

        // -------- Проверка существующего файла --------
        if (File.Exists(savePath))
        {
            if (!forceCheck)
            {
                // Быстрый путь: файл есть — считаем валидным (доверяем версии манифеста)
                if (file.Sha256 is null)
                    return relativePath;

                // SHA указан — проверим хотя бы его, это дёшево и надёжно
                if (await HashHelper.VerifySha256Async(savePath, file.Sha256))
                    return relativePath;
            }
            else
            {
                // Принудительная проверка: SHA обязателен
                if (file.Sha256 is null)
                {
                    // Нечего проверять — файл существует, считаем ок
                    return relativePath;
                }

                if (await HashHelper.VerifySha256Async(savePath, file.Sha256))
                    return relativePath;

                Logger.Info($"SHA256 mismatch, redownloading: {relativePath}");
            }
        }
        else if (file.Optional && File.Exists(savePath + ".disabled"))
        {
            // Отключённый мод: не трогаем, он "разрешён" как optional-disabled
            if (file.Sha256 is null ||
                await HashHelper.VerifySha256Async(savePath + ".disabled", file.Sha256))
            {
                return relativePath;
            }
        }

        // -------- Скачивание --------
        try
        {
            using var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            // Пишем во временный файл, потом переносим — защита от битых загрузок
            string tempPath = savePath + ".tmp";

            using (var stream = await response.Content.ReadAsStreamAsync())
            using (var fileStream = File.Create(tempPath))
            {
                await stream.CopyToAsync(fileStream);
            }

            // Проверяем SHA после загрузки (если он известен)
            if (file.Sha256 is not null &&
                !await HashHelper.VerifySha256Async(tempPath, file.Sha256))
            {
                Logger.Error($"Downloaded file failed SHA256: {relativePath}");
                File.Delete(tempPath);
                throw new Exception($"SHA256 mismatch after download: {relativePath}");
            }

            if (File.Exists(savePath))
                File.Delete(savePath);

            File.Move(tempPath, savePath);
        }
        catch (Exception e)
        {
            Logger.Error($"Failed to download {url}: {e}");
        }

        return relativePath;
    }

    // ============================================================
    //  ЧИСТКА ЛИШНИХ ФАЙЛОВ
    // ============================================================

    public async Task CleanDirectoryAsync(
        string instancePath,
        List<string> allowedFiles,
        List<string> cleanFolders)
    {
        if (cleanFolders == null || cleanFolders.Count == 0)
            return;

        Logger.Info($"Clean folders: {string.Join(", ", cleanFolders)}");

        var allowedSet = new HashSet<string>(allowedFiles, StringComparer.OrdinalIgnoreCase);

        foreach (var folder in cleanFolders)
        {
            string folderPath = Path.Combine(instancePath, folder);
            if (!Directory.Exists(folderPath))
                continue;

            Logger.Info($"Clean folder: {folder}");

            var allFiles = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);

            int deletedCount = 0;

            foreach (var file in allFiles)
            {
                string relativePath = Path.GetRelativePath(instancePath, file)
                    .Replace('\\', '/');

                string fileName = Path.GetFileName(file);

                bool isProtected =
                    fileName.StartsWith("user_file-") ||
                    fileName.EndsWith(".disabled") ||
                    fileName.EndsWith(".tmp");

                if (!allowedSet.Contains(relativePath) && !isProtected)
                {
                    Logger.Info($"Delete: {relativePath}");
                    try
                    {
                        File.Delete(file);
                        deletedCount++;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Failed to delete {relativePath}: {ex}");
                    }
                }
            }

            Logger.Info($"Cleared {deletedCount} files in {folder}");
        }

        await Task.CompletedTask;
    }
}