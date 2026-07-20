using SkyCraft.Shared.Helpers;

namespace SkyCraft.Shared;

public static class AppInitializer
{
    private static bool _initialized = false;

    public static void Initialize(string logsRoot)
    {
        if (_initialized) return;

        var logPath = Path.Combine(
            logsRoot,
            $"log-{DateTime.Now:yyyyMMdd-HHmmss}.log"
        );

        Logger.Init(logPath);

        Logger.Info("Started");
        
        _initialized = true;
    }
}