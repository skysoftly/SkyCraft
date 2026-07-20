using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SkyCraft.Launcher.Services;
using SkyCraft.Launcher.Services.Navigation;
using SkyCraft.Launcher.Services.Settings;
using SkyCraft.Launcher.ViewModels.Base;
using SkyCraft.Launcher.ViewModels.Page;

namespace SkyCraft.Launcher.ViewModels.Controls;

public partial class TitleBarViewModel : ViewModelBase
{
    private readonly NavigationService _navigation;
    private readonly SettingsService _settings;
    
    public string Nickname => _settings.Settings.Nickname;
    
    [ObservableProperty]
    private bool _showUserPanel;

    public TitleBarViewModel(NavigationService navigation, SettingsService settings)
    {
        _navigation = navigation;
        _settings = settings;
        
        _navigation.StateChanged += Update;
    }

    private void Update()
    {
        ShowUserPanel = _navigation is { CurrentPage: HomePageViewModel, CurrentOverlay: null };
        
        OnPropertyChanged(nameof(Nickname));
    }
    [RelayCommand]
    private async Task LogoutAsync()
    {
        _settings.Settings.Nickname = string.Empty;

        await _settings.SaveAsync();
        
        await _navigation.NavigateAsync<LoginPageViewModel>();
    }
}