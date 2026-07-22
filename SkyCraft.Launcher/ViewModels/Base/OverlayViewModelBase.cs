using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SkyCraft.Launcher.Services.Navigation;

namespace SkyCraft.Launcher.ViewModels.Base;

public abstract partial class OverlayViewModelBase : ViewModelBase
{
    protected readonly NavigationService Navigation;

    [ObservableProperty] private double _opacity = 0;
    [ObservableProperty] private string _transform = "scale(0, 0)";

    protected OverlayViewModelBase(NavigationService navigation)
    {
        Navigation = navigation;
    }


    public virtual async Task OnOpenedAsync()
    {
        await Task.Delay(100);
        Opacity = 1;
        Transform = "scale(1, 1)";
    }

    public virtual async Task OnClosedAsync()
    {
    }


    [RelayCommand]
    public virtual async Task CloseAsync()
    {
        Opacity = 0;
        Transform = "scale(0.5, 0.5)";
        await Task.Delay(200);
        await Navigation.CloseOverlayAsync();
    }
}