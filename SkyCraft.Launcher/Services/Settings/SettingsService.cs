using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using SkyCraft.Launcher.Helpers;
using SkyCraft.Launcher.Models;
using SkyCraft.Shared.Helpers;

namespace SkyCraft.Launcher.Services.Settings;

public class SettingsService
{
    public SettingsModel Settings { get; private set; } = new();
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };
    
    public async Task LoadAsync()
    {
        Directory.CreateDirectory(PathHelper.GetHideRoot());

        if (!File.Exists(PathHelper.GetSettingsPath()))
        {
            Logger.Info("Settings file not found. Creating default settings.");

            Settings = CreateDefault();

            await SaveAsync();

            return;
        }

        try
        {
            await using var stream = File.OpenRead(PathHelper.GetSettingsPath());

            var settings = await JsonSerializer.DeserializeAsync<SettingsModel>(stream);

            if (settings == null)
                throw new InvalidDataException("Invalid settings file.");

            Settings = settings;

            Logger.Info("Settings loaded.");
        }
        catch
        {
            Logger.Warning("Settings file is invalid. Recreating.");

            Settings = CreateDefault();

            await SaveAsync();
        }
    }

    public async Task SaveAsync()
    {
        Directory.CreateDirectory(PathHelper.GetHideRoot());

        await using var stream = File.Create(PathHelper.GetSettingsPath());

        await JsonSerializer.SerializeAsync(
            stream,
            Settings,
            JsonOptions);

        Logger.Info("Settings saved.");
    }
    
    
    private static SettingsModel CreateDefault()
    {
        return new SettingsModel
        {
            Nickname = string.Empty,
            InstallDirectory = PathHelper.GetDefaultInstallDirectory(),
            Ram = 4096,
            HideOnPlay = false,
            SelectedBuild = string.Empty
        };
    }
}