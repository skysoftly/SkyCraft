using CommunityToolkit.Mvvm.ComponentModel;

namespace SkyCraft.Installer.Models;

public partial class ProgressModel : ObservableObject
{
    [ObservableProperty] private string _status = string.Empty;
    [ObservableProperty] private double _progress;
    [ObservableProperty] private long _totalBytes;
    [ObservableProperty] private long _downloadedBytes;
}