using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using SkyCraft.Launcher.ViewModels.Base;
using SkyCraft.Shared.Helpers;

namespace SkyCraft.Launcher.Services.Navigation;

public class NavigationService
{
    private readonly IServiceProvider _services;

    public PageViewModelBase? CurrentPage { get; private set; }
    public OverlayViewModelBase? CurrentOverlay { get; private set; }

    public event Action? StateChanged;

    public NavigationService(IServiceProvider services)
    {
        _services = services;
    }
    
    public async Task<T> NavigateAsync<T>()
        where T : PageViewModelBase
    {
        Logger.Info($"Navigated to {GetName(typeof(T))}");

        if (CurrentPage != null)
        {
            await CurrentPage.OnNavigatedFromAsync();
        }

        var page = _services.GetRequiredService<T>();

        CurrentPage = page;

        await page.OnNavigatedToAsync();

        StateChanged?.Invoke();

        return page;
    }
    public async Task<T> ShowOverlayAsync<T>()
        where T : OverlayViewModelBase
    {
        if (CurrentOverlay != null)
            await CloseOverlayAsync();

        Logger.Info($"Opening overlay: {GetName(typeof(T))}");

        var overlay = _services.GetRequiredService<T>();

        CurrentOverlay = overlay;

        StateChanged?.Invoke();

        await overlay.OnOpenedAsync();

        return overlay;
    }

    public async Task CloseOverlayAsync()
    {
        if (CurrentOverlay == null)
            return;

        Logger.Info($"Closing overlay: {GetName(CurrentOverlay.GetType())}");

        await CurrentOverlay.OnClosedAsync();

        CurrentOverlay = null;

        StateChanged?.Invoke();
    }
    
    private static string GetName(Type type)
    {
        return type.Name.Replace("ViewModel", "");
    }
}