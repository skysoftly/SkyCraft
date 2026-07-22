using System;
using Microsoft.Extensions.DependencyInjection;
using SkyCraft.Installer.Services;
using SkyCraft.Installer.ViewModels;

namespace SkyCraft.Installer;

public static class ServiceProvider
{
    public static IServiceProvider Build()
    {
        var services = new ServiceCollection();

        services.AddHttpClient();

        services.AddSingleton<StartupService>();
        services.AddSingleton<ManifestService>();
        services.AddSingleton<DownloadService>();
        services.AddSingleton<LauncherService>();
        
        services.AddSingleton<TitleBarViewModel>();

        services.AddSingleton<MainWindowViewModel>();

        return services.BuildServiceProvider();
    }
}