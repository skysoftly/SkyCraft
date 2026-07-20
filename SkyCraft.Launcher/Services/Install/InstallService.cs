using System.Threading.Tasks;
using SkyCraft.Launcher.Models.Build;
using SkyCraft.Launcher.Services.Install.Instance;

namespace SkyCraft.Launcher.Services.Install;

public class InstallService
{
    private readonly JavaService _java;
    private readonly AssetsService _assets;
    private readonly LibrariesService _libraries;
    private readonly InstanceService _instance;

    public InstallService(JavaService java, AssetsService assets, LibrariesService libraries, InstanceService instance)
    {
        _java = java;
        _assets = assets;
        _libraries = libraries;
        _instance = instance;
    }

    public async Task InstallAsync(BuildModel build)
    {
        await _java.EnsureAsync();
        await _assets.EnsureAsync();
        await _libraries.EnsureAsync();
        await _instance.EnsureAsync();
    }
}