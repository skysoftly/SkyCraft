using System;
using Microsoft.Extensions.DependencyInjection;
using SkyCraft.Launcher.Models;
using SkyCraft.Launcher.Services;
using SkyCraft.Launcher.Services.Build;
using SkyCraft.Launcher.Services.Install;
using SkyCraft.Launcher.Services.Install.Instance;
using SkyCraft.Launcher.Services.Navigation;
using SkyCraft.Launcher.Services.Settings;
using SkyCraft.Launcher.ViewModels;
using SkyCraft.Launcher.ViewModels.Controls;
using SkyCraft.Launcher.ViewModels.Overlay;
using SkyCraft.Launcher.ViewModels.Page;

namespace SkyCraft.Launcher;

public static class ServiceProvider
{
    public static IServiceProvider Build()
    {
        var services = new ServiceCollection();

        services.AddHttpClient();

        services.AddSingleton<SettingsService>();
        services.AddSingleton<NavigationService>();
        
        services.AddSingleton<ManifestService>();
        services.AddSingleton<BuildService>();
        services.AddSingleton<InstallService>();
        services.AddSingleton<JavaService>();
        services.AddSingleton<AssetsService>();
        services.AddSingleton<LibrariesService>();
        services.AddSingleton<InstanceService>();


        services.AddTransient<LoginPageViewModel>();
        services.AddTransient<HomePageViewModel>();
        
        services.AddSingleton<LoadingOverlayViewModel>();
        
        services.AddSingleton<TitleBarViewModel>();
        
        services.AddSingleton<MainWindowViewModel>();
        
        return services.BuildServiceProvider();
    }
}