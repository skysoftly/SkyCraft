using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using SkyCraft.Launcher.Models.Build;
using SkyCraft.Launcher.Services.Build;
using SkyCraft.Launcher.Services.Install;
using SkyCraft.Launcher.Services.Navigation;
using SkyCraft.Launcher.ViewModels.Base;
using SkyCraft.Launcher.ViewModels.Controls;
using SkyCraft.Shared.Helpers;

namespace SkyCraft.Launcher.ViewModels.Page;

public partial class HomePageViewModel : PageViewModelBase
{
    private readonly NavigationService _navigation;
    private readonly InstallService _installService;
    private readonly BuildService _buildService;

    public ObservableCollection<BuildCardViewModel> Builds { get; } = [];

    [ObservableProperty] private BuildCardViewModel? _selectedBuild;


    public HomePageViewModel(BuildService buildService, NavigationService navigation, InstallService installService)
    {
        _buildService = buildService;
        _navigation = navigation;
        _installService = installService;
    }

    public override async Task OnNavigatedToAsync()
    {
        var builds = await _buildService.GetAllAsync();

        foreach (var build in builds)
        {
            Builds.Add(new BuildCardViewModel(build, _navigation, _installService));
        }
        
        SelectedBuild = Builds.FirstOrDefault();
    }
}