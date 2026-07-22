using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hardware.Info;
using SkyCraft.Helpers;
using SkyCraft.Launcher.Services.Navigation;
using SkyCraft.Launcher.Services.Settings;
using SkyCraft.Launcher.ViewModels.Base;
using SkyCraft.Shared.Helpers;

namespace SkyCraft.Launcher.ViewModels.Overlay;

public partial class SettingsOverlayViewModel : OverlayViewModelBase
{


    private readonly SettingsService _settings;
        
    [ObservableProperty] private string _selectedPath = string.Empty;
    [ObservableProperty] private bool _hideOnPlay;
    
    [ObservableProperty] private int _ramGb;
    
    [ObservableProperty] private int _maxRamGb = 50;
    //
    partial void OnRamGbChanged(int value)
    {
        UpdateSelectedOption();
    }
    //
    [ObservableProperty] private List<RamOption> _ramOptions = new();
    //
    public partial class RamOption : ObservableObject
    {
        public string Text { get; set; } = string.Empty;
        public int Value { get; set; }
        public ICommand? SelectCommand { get; set; }
    
        [ObservableProperty] private bool _isSelected;
    }
    //
    
    
    public SettingsOverlayViewModel(NavigationService navigation, SettingsService settings) :
        base(navigation)
    {
        _settings = settings;
    }
    //
    public override Task OnOpenedAsync()
    {
        var hardwareInfo = new HardwareInfo();
        hardwareInfo.RefreshMemoryStatus();
    
        // Получаем общий объем RAM в МБ
        ulong totalMemoryBytes = hardwareInfo.MemoryStatus.TotalPhysical;
        ulong totalMemoryMb = totalMemoryBytes / (1024 * 1024);
    
        // Округляем вниз до целых ГБ
        var maxGb = (int)(totalMemoryMb / 1024);
    
        // Ограничиваем максимум 12 ГБ
        if (maxGb > 12)
            maxGb = 12;
    
        // Очищаем и заполняем RamOptions
        RamOptions.Clear();
        for (int i = 0; i <= maxGb; i += 2)
        {
            var i1 = i;
            RamOptions.Add(new RamOption
            {
                Text = i == 0 ? "Авто" : $"{i} Гб",
                Value = i,
                SelectCommand = new RelayCommand(() => SelectRam(i1))
            });
        }
    
        MaxRamGb = (RamOptions.Count - 1) * 2;
    
        // Загружаем настройки (быстрые операции)
        RamGb = _settings.Settings.Ram / 1024;
        SelectedPath = _settings.Settings.InstallDirectory;
        HideOnPlay = _settings.Settings.HideOnPlay;
        //
        UpdateSelectedOption();
        //
        // // Вызываем базовую анимацию
        return base.OnOpenedAsync();
    }
    
    private void UpdateSelectedOption()
    {
        foreach (var option in RamOptions)
        {
            option.IsSelected = option.Value == RamGb;
        }
    }
    
    
    private void SelectRam(int i)
    {
        RamGb = i;
    }
    
    [RelayCommand]
    private async Task OpenFolder()
    {
        if (string.IsNullOrEmpty(SelectedPath))
            return;
    
        try
        {
            if (Directory.Exists(SelectedPath))
            {
                // Открываем папку в проводнике
                Process.Start(new ProcessStartInfo
                {
                    FileName = SelectedPath,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            else
            {
                Logger.Warning("The selected folder does not exist.");
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Ошибка открытия папки: {ex}");
        }
    }
    
    [RelayCommand]
    private async Task EditFolder()
    {
        var path = await FileHelper.OnBrowseFolderClickAsync();
        if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
        {
            SelectedPath = path;
        }
    }
    //
    //
    [RelayCommand]
    private async Task SaveAsync()
    {
        Logger.Info("Saving...");

        _settings.Settings.Ram = (RamGb * 1024);
        _settings.Settings.InstallDirectory = SelectedPath;
        _settings.Settings.HideOnPlay = HideOnPlay;
        
        await _settings.SaveAsync();
        CloseCommand.Execute(null);
    }
}