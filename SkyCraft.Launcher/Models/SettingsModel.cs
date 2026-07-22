namespace SkyCraft.Launcher.Models;

public class SettingsModel
{
    public string Nickname { get; set; } = "";
    public string InstallDirectory { get; set; } = "";
    public int Ram { get; set; }
    public bool HideOnPlay { get; set; }
    public string? SelectedBuild { get; set; }
}