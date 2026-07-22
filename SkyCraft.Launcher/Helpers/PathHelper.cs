using System;
using System.IO;

namespace SkyCraft.Launcher.Helpers;

public static class PathHelper
{
    // AppData
    public static string GetHideRoot()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".skycraft"
        );
    }

    public static string GetLogsRoot()
    {
        return Path.Combine(GetHideRoot(), "logs");
    }

    public static string GetSettingsPath()
    {
        return Path.Combine(GetHideRoot(), "settings.json");
    }
    
    
    // Game
    public static string GetDefaultInstallDirectory()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "SkyCraft"
        );
    }
    
    public static string GetAssetsRoot(string installDirectory)
    {
        return Path.Combine(installDirectory, "assets");
    }
    public static string GetJavaRoot(string installDirectory)
    {
        return Path.Combine(installDirectory, "java");
    }
    public static string GetInstancesRoot(string installDirectory)
    {
        return Path.Combine(installDirectory, "instances");
    }
    
    public static string GetInstanceRoot(string installDirectory, string id)
    {
        return Path.Combine(GetInstancesRoot(installDirectory), id);
    }
}