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
    //  РЕЗУЛЬТАТ ЛЁГКОЙ ПРОВЕРКИ
    // ============================================================

    private sealed class QuickCheckResult
    {
        public bool AllGood;
        public int Missing;      // файлов нет
        public int SizeMismatch; // размер не совпал
        public int Total;
    }

    // ============================================================
    //  УСТАНОВКА ФАЙЛОВ
    // ============================================================

    /// <summary>
    /// Обычная установка.
    /// - Если версия манифеста совпала и forceCheck=false → лёгкая проверка (наличие + размер).
    /// - Если чего-то не хватает → докачивает только недостающее (без полного SHA).
    /// - Если forceCheck=true → SHA256 каждого файла.
    /// </summary>
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

        string currentVersion = ReadManifestVersion(instancePath);
        bool versionMatch = !string.IsNullOrEmpty(currentVersion)
                            && currentVersion == manifest.Version;

        // ---------- Быстрый путь ----------
        if (!forceCheck && versionMatch)
        {
            installContext.Status = "Быстрая проверка файлов";

            var quick = QuickCheck(instancePath, manifest);

            Logger.Info(
                $"Quick check: total={quick.Total}, missing={quick.Missing}, " +
                $"sizeMismatch={quick.SizeMismatch}");

            if (quick.AllGood)
            {
                installContext.Status   = "Файлы актуальны";
                installContext.Progress = 100;
                Logger.Info("All files present, skipping install.");
                return;
            }

            Logger.Info("Some files missing/changed — running repair.");
        }
        else if (forceCheck)
        {
            Logger.Info("Force check requested — verifying every file by SHA256.");
        }
        else
        {
            Logger.Info($"Manifest version changed: '{currentVersion}' -> '{manifest.Version}'.");
        }

        // ---------- Полная установка / ремонт ----------
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

        await CleanDirectoryAsync(instancePath, allowedFiles, manifest.CleanDirectories);

        WriteManifestVersion(instancePath, manifest.Version);

        installContext.Status   = "Файлы установлены";
        installContext.Progress = 100;
    }

    // ============================================================
    //  ПРИНУДИТЕЛЬНАЯ ПРОВЕРКА
    // ============================================================

    /// <summary>
    /// Принудительно проверяет все файлы манифеста по SHA256.
    /// </summary>
    public async Task<bool> VerifyAndRepairAsync(BuildModel build, InstallContext installContext)
    {
        try
        {
            Logger.Info("Force verify files started");
            installContext.Status = "Проверка целостности файлов";

            await InstallAsync(build, installContext, forceCheck: true);

            installContext.Status   = "Проверка завершена";
            installContext.Progress = 100;
            installContext.Finally  = true;

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
    //  ЛЁГКАЯ ПРОВЕРКА (наличие + размер)
    // ============================================================

    private QuickCheckResult QuickCheck(string instancePath, FilesManifestModel manifest)
    {
        var result = new QuickCheckResult { Total = manifest.Files.Count };

        foreach (var file in manifest.Files)
        {
            string savePath = ResolveSavePath(instancePath, file, out _);

            // optional .disabled
            if (file.Optional && File.Exists(savePath + ".disabled"))
                continue;

            if (!File.Exists(savePath))
            {
                result.Missing++;
                continue;
            }

            // Если SHA нет — файл неприкосновенный, размер не проверяем
            if (file.Sha256 is null)
                continue;

            if (file.Size is > 0)
            {
                var info = new FileInfo(savePath);
                if (info.Length != file.Size.Value)
                    result.SizeMismatch++;
            }
        }

        result.AllGood = result.Missing == 0 && result.SizeMismatch == 0;
        return result;
    }

    // ============================================================
    //  ПУТИ
    // ============================================================

    /// <summary>
    /// Возвращает итоговый путь на диске для файла (учитывая optional-префикс).
    /// </summary>
    private static string ResolveSavePath(
        string instancePath,
        FileModel file,
        out string relativePath)
    {
        relativePath = file.Path;

        if (file.Optional)
        {
            string dir  = Path.GetDirectoryName(relativePath) ?? string.Empty;
            string name = Path.GetFileName(relativePath);
            relativePath = Path.Combine(dir, $"optional-{name}");
        }

        return Path.Combine(instancePath, relativePath);
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

        try { return File.ReadAllText(path).Trim(); }
        catch { return string.Empty; }
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

    private async Task<string> DownloadFile(
        BuildModel build,
        FileModel file,
        string instancePath,
        bool forceCheck)
    {
        string url = $"{build.Files.BaseUrl}{file.Path}";

        string savePath = ResolveSavePath(instancePath, file, out string relativePath);

        string? saveDir = Path.GetDirectoryName(savePath);
        if (!string.IsNullOrEmpty(saveDir))
            Directory.CreateDirectory(saveDir);

        // -------- Проверка существующего файла --------
        if (File.Exists(savePath))
        {
            // Если SHA нет — файл неприкосновенный: не проверяем, не заменяем.
            if (file.Sha256 is null)
                return relativePath;

            bool valid;

            if (forceCheck)
            {
                // Полная проверка: SHA256
                valid = await HashHelper.VerifySha256Async(savePath, file.Sha256);
            }
            else
            {
                // Лёгкая проверка: размер (если известен)
                valid = true;

                if (file.Size is > 0)
                {
                    var info = new FileInfo(savePath);
                    if (info.Length != file.Size.Value)
                        valid = false;
                }
            }

            if (valid)
                return relativePath;

            Logger.Info($"File invalid, redownloading: {relativePath}");
        }
        else if (file.Optional && File.Exists(savePath + ".disabled"))
        {
            // Отключённый optional-мод: не трогаем.
            if (file.Sha256 is null)
                return relativePath;

            if (!forceCheck ||
                await HashHelper.VerifySha256Async(savePath + ".disabled", file.Sha256))
            {
                return relativePath;
            }
        }

        // -------- Скачивание --------
        using var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        string tempPath = savePath + ".tmp";

        try
        {
            using (var stream = await response.Content.ReadAsStreamAsync())
            using (var fileStream = File.Create(tempPath))
            {
                await stream.CopyToAsync(fileStream);
            }

            // Проверка размера после загрузки
            if (file.Size is > 0)
            {
                var info = new FileInfo(tempPath);
                if (info.Length != file.Size.Value)
                {
                    File.Delete(tempPath);
                    throw new Exception(
                        $"Size mismatch after download: {relativePath} " +
                        $"(expected {file.Size}, got {info.Length})");
                }
            }

            // Проверка SHA после загрузки (если он известен)
            if (file.Sha256 is not null &&
                !await HashHelper.VerifySha256Async(tempPath, file.Sha256))
            {
                File.Delete(tempPath);
                throw new Exception($"SHA256 mismatch after download: {relativePath}");
            }

            if (File.Exists(savePath))
                File.Delete(savePath);

            File.Move(tempPath, savePath);
        }
        catch
        {
            try { if (File.Exists(tempPath)) File.Delete(tempPath); }
            catch { /* ignore */ }
            throw;
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