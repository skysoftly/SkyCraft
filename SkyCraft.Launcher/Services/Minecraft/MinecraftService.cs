using System;
using System.Net.Http;
using System.Threading.Tasks;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.Installer.Forge;
using CmlLib.Core.Installer.NeoForge;
using CmlLib.Core.Installer.NeoForge.Installers;
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


    public async Task<MinecraftContext> InstallAsync(BuildModel build, InstallContext installContext)
    {
        Logger.Info("Install Minecraft");
        installContext.Status = "Проверка и установка Minecraft";
        var basePath = _settings.Settings.InstallDirectory;

        var path = new MinecraftPath(PathHelper.GetInstanceRoot(basePath, build.Info.Id));

        path.Assets = PathHelper.GetAssetsRoot(basePath);
        path.Runtime = PathHelper.GetJavaRoot(basePath);

        var parametrs = MinecraftLauncherParameters.CreateDefault(path);

        parametrs.JavaPathResolver = new MinecraftJavaPathResolver(path);
        
        var launcher = new MinecraftLauncher(parametrs);
        
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

        var installerOutput = new SyncProgress<string>(message => { });

        await launcher.InstallAsync(build.Minecraft.Version);

        var versionName = build.Minecraft.Version;

        switch (build.Minecraft.Loader.ToLower())
        {
            case "neoforge":
                Logger.Info("Install Neoforge");
                installContext.Status = "Проверка и установка Neoforge";
                var neoforge = new NeoForgeInstaller(launcher);

                versionName = await neoforge.Install(versionName, build.Minecraft.LoaderVersion,
                    new NeoForgeInstallOptions
                    {
                        FileProgress = fileProgress,
                        ByteProgress = byteProgress,
                        InstallerOutput = installerOutput,
                    });

                break;
            case "forge":
                Logger.Info("Install Forge");
                installContext.Status = "Проверка и установка Forge";
                var forge = new ForgeInstaller(launcher);

                versionName = await forge.Install(versionName, build.Minecraft.LoaderVersion, new ForgeInstallOptions
                {
                    FileProgress = fileProgress,
                    ByteProgress = byteProgress,
                    InstallerOutput = installerOutput,
                });
                break;
            case "fabric":
                Logger.Info("Install Fabric");
                installContext.Status = "Проверка и установка Fabric";
                var fabric = new FabricInstaller(_httpClient);

                versionName = await fabric.Install(versionName, build.Minecraft.LoaderVersion, path);
                break;
            default:
                break;
        }

        return new MinecraftContext()
        {
            Launcher = launcher,
            Version = versionName,
        };
    }

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
            Session = MSession.CreateOfflineSession(_settings.Settings.Nickname)
        };

        var process = await context.Launcher.CreateProcessAsync(context.Version, launchOption);

        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.UseShellExecute = true; 
        
        process.Start();
        Logger.Info("Minecraft opened");
    }
}