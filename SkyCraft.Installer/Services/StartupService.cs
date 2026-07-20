using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using SkyCraft.Installer.Helpers;
using SkyCraft.Installer.Models;
using SkyCraft.Shared.Helpers;

namespace SkyCraft.Installer.Services;

public class StartupService
{
    private readonly ManifestService _manifestService;
    private readonly DownloadService _downloadService;
    private readonly LauncherService _launcherService;

    public StartupService(ManifestService manifestService, DownloadService downloadService,
        LauncherService launcherService)
    {
        _manifestService = manifestService;
        _downloadService = downloadService;
        _launcherService = launcherService;
    }

    public async Task<bool> RunAsync(ProgressModel progress)
    {
        try
        {
            Logger.Info("Startup.");

            progress.Status = "Получение информации...";
            var manifest = await _manifestService.GetAsync();
            var launcherPath = PathHelper.GetLauncherPath();

            Directory.CreateDirectory(PathHelper.GetLauncherRoot());

            progress.Status = "Проверка обновлений...";

            if (!await HashHelper.VerifySha256Async(launcherPath, manifest.Sha256))
            {
                Logger.Info("Launcher update required.");

                progress.Status = "Обновление...";

                await _downloadService.DownloadAsync(manifest.Url, launcherPath, progress);

                if (!await HashHelper.VerifySha256Async(launcherPath, manifest.Sha256))
                    throw new Exception("Downloaded launcher hash verification failed.");

                Logger.Info("Launcher updated.");
            }
            else
            {
                Logger.Info("Launcher is up to date.");
            }

            progress.Status = "Запуск лаунчера...";

            await _launcherService.StartAsync(launcherPath);
            Logger.Info("Launcher started.");
            return true;
        }
        catch (Exception e)
        {
            progress.Status = "Ошибка загрузки";
            Logger.Error(e.Message);
            return false;
        }
    }
}