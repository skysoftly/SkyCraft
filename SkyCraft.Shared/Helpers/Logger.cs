namespace SkyCraft.Shared.Helpers;

public static class Logger
{
    private static string? _logPath;
    private const int MaxLogFiles = 9;

    public static void Init(string logPath)
    {
        _logPath = logPath; 
        var dir = Path.GetDirectoryName(_logPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
            CleanOldLogs(dir);
        }
    }
    
    public static void Info(string message) => Write("INFO", message);
    public static void Warning(string message) => Write("WARNING", message);
    public static void Error(string message) => Write("ERROR", message);

    private static void Write(string level, string message)
    {
        if (_logPath is null) return;
        try
        {
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";
            File.AppendAllText(_logPath, line + Environment.NewLine);
            Console.WriteLine(line);
        }
        catch
        {
            // ignored
        }
    }

    private static void CleanOldLogs(string dir)
    {
        try
        {
            var files = Directory.GetFiles(dir, "*.log")
                .Select(path => new FileInfo(path))
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .Skip(MaxLogFiles);

            foreach (var file in files)
            {
                file.Delete();
            }
        }
        catch
        {
            // ignored
        }
    }

}