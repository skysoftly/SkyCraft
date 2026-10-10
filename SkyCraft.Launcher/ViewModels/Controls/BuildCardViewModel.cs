using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CmlLib.Core;
using CmlLib.Core.Java;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SkyCraft.Launcher.Helpers;
using SkyCraft.Launcher.Models.Build;
using SkyCraft.Launcher.Services;
using SkyCraft.Launcher.Services.Minecraft;
using SkyCraft.Launcher.Services.Navigation;
using SkyCraft.Launcher.Services.Settings;
using SkyCraft.Launcher.ViewModels.Base;
using SkyCraft.Launcher.ViewModels.Overlay;

namespace SkyCraft.Launcher.ViewModels.Controls;

public partial class BuildCardViewModel : ViewModelBase
{
    private readonly SettingsService _settings;
    private readonly NavigationService _navigation;
    private readonly MinecraftService _minecraftService;
    private readonly InstanceService _instanceService;
    private readonly ImageService _imageService;
    private readonly ServerService _serverService;

    public BuildModel Build { get; }

    [ObservableProperty] private Bitmap? _icon;
    [ObservableProperty] private Bitmap? _banner;
    [ObservableProperty] private int _online;

    public BuildCardViewModel(
        BuildModel build,
        NavigationService navigation,
        MinecraftService minecraftService,
        InstanceService instanceService,
        SettingsService settings,
        ImageService imageService,
        ServerService serverService)
    {
        Build = build;
        _navigation = navigation;
        _minecraftService = minecraftService;
        _instanceService = instanceService;
        _settings = settings;
        _imageService = imageService;
        _serverService = serverService;
    }

    public async Task UpdateBuildCard()
    {
        Icon   = await _imageService.LoadAsync(Build.Assets.BaseUrl + Build.Assets.Icon);
        Banner = await _imageService.LoadAsync(Build.Assets.BaseUrl + Build.Assets.Banner);

        Online = 0;

        foreach (var server in Build.Servers)
        {
            Online += await _serverService.GetOnlineAsync(server.Host, server.Port);
        }
    }

    // ============================================================
    //  ИГРАТЬ
    // ============================================================

    [RelayCommand]
    private async Task PlayAsync()
    {
        var overlay = await _navigation.ShowOverlayAsync<LoadingOverlayViewModel>();
        var installContext = overlay.InstallContext;

        try
        {
            // 1. Minecraft + лоадер (с быстрым пропуском, если уже установлено)
            var ctx = await _minecraftService.InstallAsync(Build, installContext);

            // 2. Файлы сборки (mods, configs и т.п.)
            //    Без forceCheck: если версия манифеста совпадает — пропустит
            await _instanceService.InstallAsync(Build, installContext);

            installContext.Status  = "Игра запущена!";
            installContext.Finally = true;

            // 3. Запуск
            await _minecraftService.LaunchAsync(Build, ctx);

            _settings.Settings.SelectedBuild = Build.Info.Id;
            await _settings.SaveAsync();

            if (_settings.Settings.HideOnPlay)
            {
                App.MainWindow.Close();
            }
        }
        finally
        {
            // overlay скроется сам через Finally = true
        }
    }

    // ============================================================
    //  ПРОВЕРИТЬ ФАЙЛЫ (принудительно)
    // ============================================================

    [RelayCommand]
    private async Task CheckAsync()
    {
        var overlay = await _navigation.ShowOverlayAsync<LoadingOverlayViewModel>();
        var installContext = overlay.InstallContext;

        try
        {
            // 1. Принудительная проверка Minecraft + лоадер
            bool mcOk = await _minecraftService.VerifyAndRepairAsync(Build, installContext);

            // 2. Принудительная проверка файлов сборки
            bool filesOk = await _instanceService.VerifyAndRepairAsync(Build, installContext);

            installContext.Status  = (mcOk && filesOk)
                ? "Проверка завершена успешно"
                : "Проверка завершена с ошибками";
            installContext.Finally = true;
        }
        finally
        {
        }
    }

    // ============================================================
    //  МОДЫ
    // ============================================================

    [RelayCommand]
    private async Task ShowAllModsAsync()
    {
        var vm = await _navigation.ShowOverlayAsync<AllModsViewModel>();
        await vm.UpdateMods(Build);
    }

    [RelayCommand]
    private async Task ShowMyModsAsync()
    {
        var vm = await _navigation.ShowOverlayAsync<MyModsViewModel>();
        await vm.UpdateMods(Build);
    }

    [RelayCommand]
    private async Task ShowOptionalModsAsync()
    {
        var vm = await _navigation.ShowOverlayAsync<OptionalModsViewModel>();
        await vm.UpdateMods(Build);
    }
}