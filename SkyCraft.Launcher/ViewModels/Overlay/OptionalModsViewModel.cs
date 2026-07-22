using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SkyCraft.Launcher.Helpers;
using SkyCraft.Launcher.Models.Build;
using SkyCraft.Launcher.Services.Navigation;
using SkyCraft.Launcher.Services.Settings;
using SkyCraft.Launcher.ViewModels.Base;
using SkyCraft.Shared.Helpers;

namespace SkyCraft.Launcher.ViewModels.Overlay;

public partial class OptionalModsViewModel : OverlayViewModelBase
{
    private readonly SettingsService _settings;

    [ObservableProperty] private ObservableCollection<OptionalModEntry> _optionalFiles = new();
    [ObservableProperty] private int _modsCount;

    [ObservableProperty] private bool _modsLoaded;

    public OptionalModsViewModel(NavigationService navigation, SettingsService settings) : base(navigation)
    {
        _settings = settings;
    }

    public async Task UpdateMods(BuildModel build)
    {
        OptionalFiles.Clear();
        var modsPath = Path.Combine(PathHelper.GetInstanceRoot(_settings.Settings.InstallDirectory, build.Info.Id),
            "mods");

        if (Directory.Exists(modsPath))
        {
            int i = 0;
            foreach (string file in Directory.EnumerateFiles(modsPath, "optional-*.jar*", SearchOption.AllDirectories))
            {
                string fileName = Path.GetFileName(file);
                i++;

                string result = Regex.Match(fileName.Replace("optional-", ""), @"^[a-zA-Z-]+").Value.Trim('-');

                result = char.ToUpper(result[0]) + result.Substring(1);

                OptionalFiles.Add(new OptionalModEntry()
                {
                    IsEnabled = !file.EndsWith(".disabled"),
                    Name = result,
                    Path = file,
                });
            }

            if (i > 0)
                ModsLoaded = true;

            ModsCount = i;
        }
    }


    [RelayCommand]
    private async Task SaveAsync()
    {
        Logger.Info("Saving...");

        foreach (var file in OptionalFiles)
        {
            string originalPath = file.Path;
            string enabledPath = file.Path.EndsWith(".disabled")
                ? file.Path.Replace(".disabled", "")
                : file.Path;
            string disabledPath = enabledPath + ".disabled";

            if (file.IsEnabled && File.Exists(disabledPath))
            {
                // Включаем мод - переименовываем .disabled -> .jar
                File.Move(disabledPath, enabledPath);
                Logger.Info($"Enabled: {Path.GetFileName(enabledPath)}");
            }
            else if (!file.IsEnabled && File.Exists(enabledPath))
            {
                // Отключаем мод - переименовываем .jar -> .jar.disabled
                File.Move(enabledPath, disabledPath);
                Logger.Info($"Disabled: {Path.GetFileName(enabledPath)}");
            }
        }

        CloseCommand.Execute(null);
    }

}

public class OptionalModEntry
{
    public string Path { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public string Name { get; set; } = string.Empty;
}