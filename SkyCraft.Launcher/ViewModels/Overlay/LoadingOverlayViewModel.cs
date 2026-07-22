using System;
using System.Threading.Tasks;
using SkyCraft.Launcher.Models;
using SkyCraft.Launcher.Models.Build;
using SkyCraft.Launcher.Services.Navigation;
using SkyCraft.Launcher.ViewModels.Base;

namespace SkyCraft.Launcher.ViewModels.Overlay;

public class LoadingOverlayViewModel : OverlayViewModelBase
{
    public InstallContext InstallContext { get; } = new();

    public LoadingOverlayViewModel(NavigationService navigation) : base(navigation)
    {
    }
}