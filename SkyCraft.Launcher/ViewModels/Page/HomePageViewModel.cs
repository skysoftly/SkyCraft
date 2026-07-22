using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using SkyCraft.Launcher.Models.Build;
using SkyCraft.Launcher.Services;
using SkyCraft.Launcher.Services.Build;
using SkyCraft.Launcher.Services.Minecraft;
using SkyCraft.Launcher.Services.Navigation;
using SkyCraft.Launcher.Services.Settings;
using SkyCraft.Launcher.ViewModels.Base;
using SkyCraft.Launcher.ViewModels.Controls;
using SkyCraft.Shared.Helpers;

namespace SkyCraft.Launcher.ViewModels.Page;

public partial class HomePageViewModel : PageViewModelBase
{
    private readonly SettingsService _settings;
    private readonly NavigationService _navigation;
    private readonly BuildService _buildService;
    private readonly MinecraftService _minecraftService;
    private readonly InstanceService _instanceService;
    private readonly ImageService _imageService;

    public ObservableCollection<BuildCardViewModel> Builds { get; } = [];

    [ObservableProperty] private BuildCardViewModel? _selectedBuild;


    public HomePageViewModel(BuildService buildService, NavigationService navigation, InstanceService instanceService,
        MinecraftService minecraftService, SettingsService settings, ImageService imageService)
    {
        _buildService = buildService;
        _navigation = navigation;
        _instanceService = instanceService;
        _minecraftService = minecraftService;
        _settings = settings;
        _imageService = imageService;
    }

    public override async Task OnNavigatedToAsync()
    {
        var builds = await _buildService.GetAllAsync();

        foreach (var build in builds)
        {
            var buildCard = new BuildCardViewModel(build, _navigation, _minecraftService, _instanceService, _settings,
                _imageService);
            Builds.Add(buildCard);
            await buildCard.UpdateBuildCard();
        }

        SelectedBuild = Builds.FirstOrDefault(a =>
                            a.Build.Info.Id == _settings.Settings.SelectedBuild)
                        ?? Builds.FirstOrDefault();
    }
}