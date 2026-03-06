using System.Collections.ObjectModel;
using Microsoft.Graph.Models;
using OneDesk.Models.Permissions;
using OneDesk.Services.Permissions;

namespace OneDesk.ViewModels.Windows;

public partial class PermissionManagerWindowViewModel(IPermissionService permissionService) : ObservableObject
{
    private string _driveId = string.Empty;
    private string _itemId = string.Empty;

    [ObservableProperty]
    private string _itemName = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private string _inviteEmail = string.Empty;

    [ObservableProperty]
    private ShareRole _selectedRole = ShareRole.write;

    [ObservableProperty]
    private PermissionItem? _selectedPermission;

    public ObservableCollection<PermissionItem> Permissions { get; } = [];

    public void LoadItem(DriveItem item)
    {
        _driveId = item.ParentReference?.DriveId ?? string.Empty;
        _itemId = item.Id ?? string.Empty;
        ItemName = item.Name ?? "未知";
        InviteEmail = string.Empty;
        StatusMessage = string.Empty;
        SelectedPermission = null;
        _ = LoadPermissionsAsync();
    }

    [RelayCommand]
    private async Task LoadPermissionsAsync()
    {
        if (string.IsNullOrEmpty(_driveId) || string.IsNullOrEmpty(_itemId)) return;

        IsLoading = true;
        StatusMessage = string.Empty;
        try
        {
            var perms = await permissionService.GetPermissionsAsync(_driveId, _itemId);
            Permissions.Clear();
            foreach (var perm in perms)
            {
                Permissions.Add(new PermissionItem(perm));
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"加载权限失败：{ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task InviteUserAsync()
    {
        if (string.IsNullOrWhiteSpace(InviteEmail))
        {
            StatusMessage = "请输入邮箱地址。";
            return;
        }

        IsLoading = true;
        StatusMessage = string.Empty;
        try
        {
            await permissionService.InviteUserAsync(_driveId, _itemId, InviteEmail.Trim(), [SelectedRole]);
            InviteEmail = string.Empty;
            StatusMessage = "已成功添加权限。";
            await LoadPermissionsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"添加权限失败：{ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DeletePermissionAsync(PermissionItem? item)
    {
        item ??= SelectedPermission;
        if (item is null) return;
        if (item.IsOwner)
        {
            StatusMessage = "无法删除所有者的权限。";
            return;
        }

        IsLoading = true;
        StatusMessage = string.Empty;
        try
        {
            await permissionService.DeletePermissionAsync(_driveId, _itemId, item.PermissionId);
            StatusMessage = "已成功删除权限。";
            await LoadPermissionsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"删除权限失败：{ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
