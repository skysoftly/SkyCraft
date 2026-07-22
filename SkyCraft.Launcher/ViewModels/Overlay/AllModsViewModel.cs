using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using SkyCraft.Launcher.Models.Build;
using SkyCraft.Launcher.Services.Navigation;
using SkyCraft.Launcher.ViewModels.Base;

namespace SkyCraft.Launcher.ViewModels.Overlay;

public partial class AllModsViewModel : OverlayViewModelBase
{
    [ObservableProperty] private List<ModModel> _displayMods = new();
    [ObservableProperty] private int _modsCount;
    //
    public AllModsViewModel(NavigationService navigation) : base(navigation)
    {
    }
    
    public async Task UpdateMods(BuildModel build)
    {
        DisplayMods = build.Mods;
        ModsCount = DisplayMods.Count;
    }

}