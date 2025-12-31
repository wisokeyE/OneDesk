using OneDesk.Services.Auth;
using OneDesk.Services.Tasks;

namespace OneDesk.ViewModels.Pages;

/// <summary>
/// 任务管理页面 ViewModel
/// </summary>
public partial class TaskManagerViewModel : ObservableObject
{
    [ObservableProperty]
    private IUserInfoManager _userInfoManager;

    [ObservableProperty]
    private ITaskScheduler _taskScheduler;

    public TaskManagerViewModel(IUserInfoManager userInfoManager, ITaskScheduler taskScheduler)
    {
        _userInfoManager = userInfoManager;
        _taskScheduler = taskScheduler;
    }
}
