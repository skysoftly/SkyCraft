using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using SkyCraft.Launcher.ViewModels;
using SkyCraft.Launcher.Views;

namespace SkyCraft.Launcher;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    public static Window MainWindow { get; set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        Services = ServiceProvider.Build();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var vm = Services.GetRequiredService<MainWindowViewModel>();

            MainWindow = new MainWindow
            {
                DataContext = vm
            };
            desktop.MainWindow = MainWindow;

            await vm.InitializeAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }
}