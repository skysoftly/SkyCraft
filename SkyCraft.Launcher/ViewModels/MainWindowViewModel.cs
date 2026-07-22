using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using SkyCraft.Launcher.Helpers;
using SkyCraft.Launcher.Services;
using SkyCraft.Launcher.Services.Navigation;
using SkyCraft.Launcher.Services.Settings;
using SkyCraft.Launcher.ViewModels.Base;
using SkyCraft.Launcher.ViewModels.Controls;
using SkyCraft.Launcher.ViewModels.Overlay;
using SkyCraft.Launcher.ViewModels.Page;
using SkyCraft.Shared.Helpers;

namespace SkyCraft.Launcher.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly NavigationService _navigation;
    private readonly SettingsService _settings;

    [ObservableProperty]
    private PageViewModelBase? _currentPage;

    [ObservableProperty]
    private OverlayViewModelBase? _currentOverlay;
    
    public TitleBarViewModel TitleBar { get; }

    [ObservableProperty] private bool _overlayVisible;

    [ObservableProperty] private bool _isLoading;
    public MainWindowViewModel(TitleBarViewModel titleBar, NavigationService navigation, SettingsService settings)
    {
        TitleBar = titleBar;
        _navigation = navigation;
        _settings = settings;

        _navigation.StateChanged += Update;
    }

    private void Update()
    {
        CurrentPage = _navigation.CurrentPage;
        CurrentOverlay = _navigation.CurrentOverlay;

        OverlayVisible = CurrentOverlay is not null;
    }
    
    public async Task InitializeAsync()
    {
        await _settings.LoadAsync();

        
        
        if (!NicknameHelper.IsValid(_settings.Settings.Nickname))
        {
            _settings.Settings.Nickname = string.Empty;

            await _settings.SaveAsync();
            await _navigation.NavigateAsync<LoginPageViewModel>();
            return;
        }
        
        await _navigation.ShowOverlayAsync<InitializeViewModel>();
        
        await _navigation.NavigateAsync<HomePageViewModel>();
        
        await _navigation.CloseOverlayAsync();
    }
}