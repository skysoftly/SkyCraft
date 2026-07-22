using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SkyCraft.Helpers;
using SkyCraft.Launcher.Helpers;
using SkyCraft.Launcher.Models.Build;
using SkyCraft.Launcher.Services.Navigation;
using SkyCraft.Launcher.Services.Settings;
using SkyCraft.Launcher.ViewModels.Base;
using SkyCraft.Shared.Helpers;

namespace SkyCraft.Launcher.ViewModels.Overlay;

public partial class MyModsViewModel : OverlayViewModelBase
{
    private readonly SettingsService _settings;

    [ObservableProperty] private ObservableCollection<MyModEntry> _myMods = new();
    [ObservableProperty] private int _modsCount;

    [ObservableProperty] private bool _modsLoaded;

    private BuildModel _build;

    public MyModsViewModel(NavigationService navigation, SettingsService settings) : base(navigation)
    {
        _settings = settings;
    }

    public async Task UpdateMods(BuildModel build)
    {
        _build = build;
        MyMods.Clear();
        var modsPath = Path.Combine(PathHelper.GetInstanceRoot(_settings.Settings.InstallDirectory, build.Info.Id),
            "mods");

        if (Directory.Exists(modsPath))
        {
            int i = 0;
            foreach (string file in Directory.EnumerateFiles(modsPath, "user_file-*.jar*", SearchOption.AllDirectories))
            {
                string fileName = Path.GetFileName(file);
                i++;

                MyMods.Add(new MyModEntry()
                {
                    IsEnabled = !file.EndsWith(".disabled"),
                    Name = GetModDisplayName(fileName),
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

        foreach (var file in MyMods)
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


    [RelayCommand]
    private async Task AddMod()
    {
        string instancePath = PathHelper.GetInstanceRoot(_settings.Settings.InstallDirectory, _build.Info.Id);

        string modsPath = Path.Combine(instancePath, "mods");
        Directory.CreateDirectory(modsPath);

        var mods = await FileHelper.OnBrowseModsClickAsync();


        foreach (var mod in mods)
        {
            string fileName = "user_file-" + Path.GetFileName(mod);

            string destPath = Path.Combine(modsPath, fileName);

            if (File.Exists(destPath) || File.Exists(destPath + ".disabled"))
                continue;


            File.Copy(mod, destPath);


            MyMods.Add(new MyModEntry()
            {
                IsEnabled = true,
                Name = GetModDisplayName(fileName),
                Path = destPath,
            });
        }

        ModsCount = MyMods.Count;
    }

    [RelayCommand]
    private void RemoveMod(MyModEntry mod)
    {
        if (File.Exists(mod.Path))
            File.Delete(mod.Path);
        

        MyMods.Remove(mod);

        ModsCount = MyMods.Count;
    }

    public static string GetModDisplayName(string fileName)
    {
        string result = Regex.Match(fileName.Replace("user_file-", ""), @"^[a-zA-Z-]+").Value.Trim('-');

        result = char.ToUpper(result[0]) + result.Substring(1);

        return result;
    }
}

public class MyModEntry
{
    public string Path { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public string Name { get; set; } = string.Empty;
}