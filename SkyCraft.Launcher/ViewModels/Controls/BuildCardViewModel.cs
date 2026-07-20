using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SkyCraft.Launcher.Models.Build;
using SkyCraft.Launcher.Services.Install;
using SkyCraft.Launcher.Services.Navigation;
using SkyCraft.Launcher.ViewModels.Base;
using SkyCraft.Launcher.ViewModels.Overlay;

namespace SkyCraft.Launcher.ViewModels.Controls;

public partial class BuildCardViewModel : ViewModelBase
{
    private readonly NavigationService _navigation;
    private readonly InstallService _installService;
    public BuildModel Build { get;}
    [ObservableProperty] private int _online;

    public BuildCardViewModel(BuildModel build, NavigationService navigation, InstallService installService)
    {
        Build = build;
        _navigation = navigation;
        _installService = installService;
    }

    [RelayCommand]
    private async Task PlayAsync()
    {
        var overlay = await _navigation.ShowOverlayAsync<LoadingOverlayViewModel>();
        
        try
        {
            // overlay.Status = "Подготовка...";

            await _installService.InstallAsync(Build);
        }
        finally
        {
            await _navigation.CloseOverlayAsync();
        }
    }
}