using System.Threading.Tasks;
using SkyCraft.Launcher.Models.Build;
using SkyCraft.Shared.Helpers;

namespace SkyCraft.Launcher.Services.Install.Instance;

public class InstanceService
{
    public async Task EnsureAsync()
    {
        Logger.Info("Checking instance.");
    }
}