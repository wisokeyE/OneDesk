namespace OneDesk.Models;

public partial class AppConfig : ObservableObject
{
    [ObservableProperty]
    private bool _followSystemTheme = true;
    [ObservableProperty]
    private string _theme = "Light";
    [ObservableProperty]
    private string _clientId = "";
    [ObservableProperty]
    private string? _tenantId;
    [ObservableProperty]
    private string _credentialFolderPath = "users";
    [ObservableProperty]
    private string? _activatedUserFileName;

    partial void OnFollowSystemThemeChanged(bool value) => RaiseConfigChanged();
    partial void OnThemeChanged(string value) => RaiseConfigChanged();
    partial void OnClientIdChanged(string value) => RaiseConfigChanged();
    partial void OnTenantIdChanged(string? value) => RaiseConfigChanged();
    partial void OnCredentialFolderPathChanged(string value) => RaiseConfigChanged();
    partial void OnActivatedUserFileNameChanged(string? value) => RaiseConfigChanged();

    public void CopyConfig(AppConfig config)
    {
        FollowSystemTheme = config.FollowSystemTheme;
        Theme = config.Theme;
        ClientId = config.ClientId;
        TenantId = config.TenantId;
        CredentialFolderPath = config.CredentialFolderPath;
        ActivatedUserFileName = config.ActivatedUserFileName;
    }

    // 用于通知外部“配置已修改”
    public event EventHandler? OnConfigChanged;

    private Timer? _debounceTimer;

    private void RaiseConfigChanged()
    {
        // 防抖动处理，避免频繁触发事件
        _debounceTimer?.Dispose();
        _debounceTimer = new Timer(_ => { OnConfigChanged?.Invoke(this, EventArgs.Empty); }, null, 500, 5000);
    }
}