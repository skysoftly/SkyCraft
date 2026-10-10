using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.Installer.Forge;
using CmlLib.Core.Installer.Forge.Versions;
using CmlLib.Core.Installer.NeoForge;
using CmlLib.Core.Installer.NeoForge.Installers;
using CmlLib.Core.Installer.NeoForge.Versions;
using CmlLib.Core.Installers;
using CmlLib.Core.Java;
using CmlLib.Core.ModLoaders.FabricMC;
using CmlLib.Core.ProcessBuilder;
using SkyCraft.Launcher.Helpers;
using SkyCraft.Launcher.Models;
using SkyCraft.Launcher.Models.Build;
using SkyCraft.Launcher.Services.Settings;
using SkyCraft.Launcher.ViewModels.Overlay;
using SkyCraft.Shared.Helpers;

namespace SkyCraft.Launcher.Services.Minecraft;

public class MinecraftService
{
    public sealed class MinecraftContext
    {
        public required MinecraftLauncher Launcher { get; init; }
        public required string Version { get; set; }
    }

    private readonly SettingsService _settings;
    private readonly HttpClient _httpClient;

    public MinecraftService(SettingsService settings, HttpClient httpClient)
    {
        _settings = settings;
        _httpClient = httpClient;
    }

    // ============================================================
    //  МАРКЕР УСТАНОВКИ
    // ============================================================

    private static string GetMarkerPath(string basePath, string buildId)
        => Path.Combine(PathHelper.GetInstanceRoot(basePath, buildId), ".installed");

    private static void WriteMarker(string basePath, string buildId, string versionName)
    {
        try
        {
            var dir = PathHelper.GetInstanceRoot(basePath, buildId);
            Directory.CreateDirectory(dir);

            File.WriteAllText(
                GetMarkerPath(basePath, buildId),
                $"{versionName}\n{DateTime.UtcNow:O}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to write install marker: {ex}");
        }
    }

    private static bool MarkerExists(string basePath, string buildId, out string versionName)
    {
        versionName = null;

        var marker = GetMarkerPath(basePath, buildId);
        if (!File.Exists(marker))
            return false;

        try
        {
            var lines = File.ReadAllLines(marker);
            versionName = lines.Length > 0 ? lines[0] : null;
            return !string.IsNullOrEmpty(versionName);
        }
        catch
        {
            return false;
        }
    }

    private static void DeleteMarker(string basePath, string buildId)
    {
        try
        {
            var marker = GetMarkerPath(basePath, buildId);
            if (File.Exists(marker))
                File.Delete(marker);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to delete install marker: {ex}");
        }
    }

    // ============================================================
    //  ПРОВЕРКА УСТАНОВКИ
    // ============================================================

    private bool IsInstalled(string basePath, BuildModel build, out string versionName)
    {
        versionName = build.Minecraft.Version;

        // 1) Быстрая проверка по маркеру
        if (MarkerExists(basePath, build.Info.Id, out var markedVersion))
        {
            // Дополнительно убедимся, что JSON этой версии реально существует
            var instanceRoot = PathHelper.GetInstanceRoot(basePath, build.Info.Id);
            var versionJson = Path.Combine(instanceRoot, "versions", markedVersion,
                                           markedVersion + ".json");

            if (File.Exists(versionJson))
            {
                versionName = markedVersion;
                return true;
            }

            // Маркер есть, а файла нет — установка повреждена
            DeleteMarker(basePath, build.Info.Id);
        }

        // 2) Fallback-проверка без маркера (на случай старых установок)
        var instanceRootFallback = PathHelper.GetInstanceRoot(basePath, build.Info.Id);
        if (!Directory.Exists(instanceRootFallback))
            return false;

        var versionsDir = Path.Combine(instanceRootFallback, "versions");
        if (!Directory.Exists(versionsDir))
            return false;

        var baseVersionJson = Path.Combine(versionsDir, build.Minecraft.Version,
                                            build.Minecraft.Version + ".json");
        if (!File.Exists(baseVersionJson))
            return false;

        var loader = build.Minecraft.Loader?.ToLower();
        if (!string.IsNullOrEmpty(loader) && loader != "vanilla")
        {
            if (string.IsNullOrEmpty(build.Minecraft.LoaderVersion))
                return false;

            var loaderFolder = Directory.GetDirectories(versionsDir)
                .FirstOrDefault(d => Path.GetFileName(d)
                    .Contains(build.Minecraft.LoaderVersion, StringComparison.OrdinalIgnoreCase));

            if (loaderFolder == null)
                return false;

            versionName = Path.GetFileName(loaderFolder);
        }

        // Дозапишем маркер, раз всё валидно
        WriteMarker(basePath, build.Info.Id, versionName);
        return true;
    }

    // ============================================================
    //  СОЗДАНИЕ ЛАУНЧЕРА
    // ============================================================

    private MinecraftLauncher CreateLauncher(
        string basePath,
        string buildId,
        InstallContext installContext = null)
    {
        var path = new MinecraftPath(PathHelper.GetInstanceRoot(basePath, buildId));
        path.Assets  = PathHelper.GetAssetsRoot(basePath);
        path.Runtime = PathHelper.GetJavaRoot(basePath);

        var parameters = MinecraftLauncherParameters.CreateDefault(path);
        parameters.JavaPathResolver = new MinecraftJavaPathResolver(path);

        var launcher = new MinecraftLauncher(parameters);

        if (installContext != null)
        {
            launcher.FileProgressChanged += (_, e) =>
            {
                installContext.File = e.Name;
            };

            launcher.ByteProgressChanged += (_, e) =>
            {
                if (e.TotalBytes > 0)
                {
                    installContext.Progress =
                        (double)e.ProgressedBytes / e.TotalBytes * 100;
                }
            };
        }

        return launcher;
    }

    // ============================================================
    //  УСТАНОВКА ЛОАДЕРА
    // ============================================================

    private async Task<string> InstallLoaderAsync(
        MinecraftLauncher launcher,
        BuildModel build,
        InstallContext installContext,
        string versionName)
    {
        var fileProgress = new SyncProgress<InstallerProgressChangedEventArgs>(e =>
        {
            installContext.File = e.Name;

            installContext.Progress =
                e.TotalTasks == 0
                    ? 0
                    : (double)e.ProgressedTasks / e.TotalTasks * 100;
        });

        var byteProgress = new SyncProgress<ByteProgress>(e =>
        {
            if (e.TotalBytes > 0)
            {
                installContext.Progress =
                    (double)e.ProgressedBytes / e.TotalBytes * 100;
            }
        });

        var installerOutput = new SyncProgress<string>(_ => { });

        switch (build.Minecraft.Loader?.ToLower())
        {
            case "neoforge":
                Logger.Info("Install Neoforge");
                installContext.Status = "Проверка и установка Neoforge";

                var neoforge = new NeoForgeInstaller(launcher);

                return await neoforge.Install(
                    versionName,
                    build.Minecraft.LoaderVersion,
                    new NeoForgeInstallOptions
                    {
                        FileProgress    = fileProgress,
                        ByteProgress    = byteProgress,
                        InstallerOutput = installerOutput,
                    });

            case "forge":
                Logger.Info("Install Forge");
                installContext.Status = "Проверка и установка Forge";

                var forge = new ForgeInstaller(launcher);

                return await forge.Install(
                    versionName,
                    build.Minecraft.LoaderVersion,
                    new ForgeInstallOptions
                    {
                        FileProgress    = fileProgress,
                        ByteProgress    = byteProgress,
                        InstallerOutput = installerOutput,
                    });

            case "fabric":
                Logger.Info("Install Fabric");
                installContext.Status = "Проверка и установка Fabric";

                var fabric = new FabricInstaller(_httpClient);

                return await fabric.Install(
                    versionName,
                    build.Minecraft.LoaderVersion,
                    launcher.MinecraftPath);

            default:
                return versionName;
        }
    }

    // ============================================================
    //  ОСНОВНАЯ УСТАНОВКА
    // ============================================================

    /// <summary>
    /// Устанавливает Minecraft и лоадер.
    /// Если уже установлено — возвращает готовый контекст, ничего не скачивая.
    /// </summary>
    /// <param name="forceReinstall">Принудительно переустановить, игнорируя маркер.</param>
    public async Task<MinecraftContext> InstallAsync(
        BuildModel build,
        InstallContext installContext,
        bool forceReinstall = false)
    {
        var basePath = _settings.Settings.InstallDirectory;

        // ---- БЫСТРЫЙ ПУТЬ: уже установлено ----
        if (!forceReinstall && IsInstalled(basePath, build, out var installedVersion))
        {
            Logger.Info($"Minecraft already installed, skipping: {installedVersion}");

            installContext.Status   = "Уже установлено";
            installContext.Progress = 100;

            var existingLauncher = CreateLauncher(basePath, build.Info.Id);

            return new MinecraftContext
            {
                Launcher = existingLauncher,
                Version  = installedVersion,
            };
        }

        // ---- ПОЛНАЯ УСТАНОВКА ----
        if (forceReinstall)
        {
            Logger.Info("Force reinstall requested");
            DeleteMarker(basePath, build.Info.Id);
        }

        Logger.Info("Install Minecraft");
        installContext.Status   = "Проверка и установка Minecraft";
        installContext.Progress = 0;

        var launcher = CreateLauncher(basePath, build.Info.Id, installContext);

        var versionName = build.Minecraft.Version;

        await launcher.InstallAsync(build.Minecraft.Version);

        versionName = await InstallLoaderAsync(launcher, build, installContext, versionName);

        // Помечаем установку завершённой
        WriteMarker(basePath, build.Info.Id, versionName);

        installContext.Status   = "Установлено";
        installContext.Progress = 100;

        return new MinecraftContext
        {
            Launcher = launcher,
            Version  = versionName,
        };
    }

    // ============================================================
    //  ПРОВЕРКА И ДОКАЧКА ФАЙЛОВ
    // ============================================================

    /// <summary>
    /// Проверяет целостность файлов клиента/лоадера и докачивает недостающие.
    /// CmlLib пропускает файлы с корректным хешем, поэтому это именно "repair".
    /// </summary>
    /// <returns>true, если всё в порядке (или было исправлено).</returns>
    public async Task<bool> VerifyAndRepairAsync(BuildModel build, InstallContext installContext)
    {
        var basePath = _settings.Settings.InstallDirectory;

        try
        {
            Logger.Info("Verify & repair started");

            installContext.Status   = "Проверка файлов Minecraft";
            installContext.Progress = 0;
            installContext.File     = string.Empty;

            // Сбрасываем маркер — если что-то битое, лучше пересоздать
            DeleteMarker(basePath, build.Info.Id);

            var launcher = CreateLauncher(basePath, build.Info.Id, installContext);

            // 1. Базовая версия
            await launcher.InstallAsync(build.Minecraft.Version);

            // 2. Лоадер
            var versionName = await InstallLoaderAsync(
                launcher, build, installContext, build.Minecraft.Version);

            // 3. Записываем маркер
            WriteMarker(basePath, build.Info.Id, versionName);

            installContext.Status   = "Проверка завершена";
            installContext.Progress = 100;

            Logger.Info("Verify & repair completed successfully");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Verify & repair failed: {ex}");

            installContext.Status = "Ошибка проверки: " + ex.Message;
            return false;
        }
    }

    // ============================================================
    //  ЗАПУСК
    // ============================================================

    public async Task LaunchAsync(BuildModel build, MinecraftContext context)
    {
        int ram = _settings.Settings.Ram;
        if (ram <= 0)
        {
            ram = 4096;
        }

        Logger.Info("Starting Minecraft");

        var launchOption = new MLaunchOption
        {
            MaximumRamMb = ram,
            Session      = MSession.CreateOfflineSession(_settings.Settings.Nickname),
        };

        var process = await context.Launcher.CreateProcessAsync(context.Version, launchOption);

        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.UseShellExecute = true;

        process.Start();

        Logger.Info("Minecraft opened");
    }
}