using System;
using System.IO;

namespace SkyCraft.Installer.Helpers;

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

    public static string GetLogsRoot()
    {
        return Path.Combine(GetAppDataRoot(),
            "Installer",
            "logs"
        );
    }

    public static string GetLauncherPath()
    {
        return Path.Combine(GetLauncherRoot(), "Launcher.exe");
    }
}