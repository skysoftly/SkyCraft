using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using SkyCraft.Installer.Models;
using SkyCraft.Installer.Services;

namespace SkyCraft.Installer.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public ProgressModel Progress { get; } = new();
    
    private readonly StartupService _startupService;
 
    public MainWindowViewModel(StartupService startupService)
    {
        _startupService = startupService;
    }
    
    public async Task<bool> InitializeAsync()
    {
        return await _startupService.RunAsync(Progress);
    }
}