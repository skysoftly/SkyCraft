using CommunityToolkit.Mvvm.ComponentModel;

namespace SkyCraft.Launcher.Models;

public partial class InstallContext : ObservableObject
{
    [ObservableProperty]
    private string _status = "";

    [ObservableProperty]
    private string? _file = "";

    [ObservableProperty]
    private double _progress;
    
    [ObservableProperty]
    private bool _finally;
}