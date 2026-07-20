using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using SkyCraft.Launcher.Services.Navigation;

namespace SkyCraft.Launcher.ViewModels.Base;

public abstract partial class OverlayViewModelBase : ViewModelBase
{
    protected readonly NavigationService Navigation;

    protected OverlayViewModelBase(NavigationService navigation)
    {
        Navigation = navigation;
    }

    
    public virtual Task OnOpenedAsync()
    {
        return Task.CompletedTask;
    }

    public virtual Task OnClosedAsync()
    {
        return Task.CompletedTask;
    }
    
    
    [RelayCommand]
    public virtual async Task CloseAsync()
    {
        await Navigation.CloseOverlayAsync();
    }
}