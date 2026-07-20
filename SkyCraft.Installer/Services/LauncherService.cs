using System.Diagnostics;
using System.Threading.Tasks;
using SkyCraft.Shared.Helpers;

namespace SkyCraft.Installer.Services;

public class LauncherService
{
    public Task StartAsync(string launcherPath)
    {
        Logger.Info($"Starting launcher: {launcherPath}");
        
        Process.Start(new ProcessStartInfo
        {
            FileName = launcherPath,
            UseShellExecute = true
        });

        return Task.CompletedTask;
    }
}