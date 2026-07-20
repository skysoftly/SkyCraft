using System.Threading.Tasks;

namespace SkyCraft.Launcher.ViewModels.Base;

public abstract class PageViewModelBase : ViewModelBase
{
    public virtual Task OnNavigatedToAsync()
    {
        return Task.CompletedTask;
    }

    public virtual Task OnNavigatedFromAsync()
    {
        return Task.CompletedTask;
    }
}