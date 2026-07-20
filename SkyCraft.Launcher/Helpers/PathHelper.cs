using System;
using System.IO;

namespace SkyCraft.Launcher.Helpers;

public static class PathHelper
{
    
    public static string GetAppDataRoot()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SkyCraft"
        );
    }

    public static string GetLauncherRoot()
    {
        return Path.Combine(GetAppDataRoot(), "launcher");
    }

    public static string GetDefaultInstallDirectory()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "SkyCraft"
        );
    }
    
    public static string GetLogsRoot()
    {
        return Path.Combine(GetLauncherRoot(), "logs");
    }

    public static string GetSettingsPath()
    {
        return Path.Combine(GetLauncherRoot(), "settings.json");
    }
}