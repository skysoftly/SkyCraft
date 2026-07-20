using System.Threading.Tasks;
using SkyCraft.Launcher.Models.Build;
using SkyCraft.Shared.Helpers;

namespace SkyCraft.Launcher.Services.Install;

public class AssetsService
{
    public async Task EnsureAsync()
    {
        Logger.Info("Checking assets.");
    }
}