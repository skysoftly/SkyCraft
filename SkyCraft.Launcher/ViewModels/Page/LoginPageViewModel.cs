using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SkyCraft.Launcher.Helpers;
using SkyCraft.Launcher.Services;
using SkyCraft.Launcher.Services.Navigation;
using SkyCraft.Launcher.Services.Settings;
using SkyCraft.Launcher.ViewModels.Base;
using SkyCraft.Shared.Helpers;

namespace SkyCraft.Launcher.ViewModels.Page;

public partial class LoginPageViewModel : PageViewModelBase
{
    private readonly SettingsService _settings;
    private readonly NavigationService _navigation;
    
    [ObservableProperty] private string _nickname = string.Empty;
    [ObservableProperty] private string _error = string.Empty;

    public LoginPageViewModel(SettingsService settings, NavigationService navigation)
    {
        _settings = settings;
        _navigation = navigation;
        
    }

    [ObservableProperty] private bool _canLogin;

    partial void OnNicknameChanged(string value)
    {
        Error = string.Empty;
        

        if (Nickname.Length < 3)
        {
            Error = "Минимальная длинна 3";
            CanLogin = false;
            return;
        }
        if (!NicknameHelper.IsValid(Nickname))
        {
            Error = "Используйте только A-Z, 0-9 и _";
            CanLogin = false;
            return;
        }

        CanLogin = true;
    }


    [RelayCommand]
    private async Task LoginAsync()
    {
        if (!CanLogin) return;
        
        Error = string.Empty;

        if (!NicknameHelper.IsValid(Nickname))
        {
            Logger.Info("Invalid Nickname");
            return;
        }

        Logger.Info("Logging in");
        _settings.Settings.Nickname = Nickname;
        
        await _settings.SaveAsync();
        
        await _navigation.NavigateAsync<HomePageViewModel>();
    }
}