using Microsoft.Graph.Models;

namespace OneDesk.ViewModels.Windows;

public partial class FileDetailsWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _fileName = string.Empty;

    [ObservableProperty]
    private string _fileSize = string.Empty;

    [ObservableProperty]
    private string _mimeType = string.Empty;

    [ObservableProperty]
    private string _createdTime = string.Empty;

    [ObservableProperty]
    private string _modifiedTime = string.Empty;

    [ObservableProperty]
    private string _quickXorHash = string.Empty;

    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _webUrl = string.Empty;

    [ObservableProperty]
    private bool _isWebUrlEnabled;

    [ObservableProperty]
    private string _createdBy = string.Empty;

    [ObservableProperty]
    private bool _isFolder;

    public void LoadFileDetails(DriveItem item)
    {
        FileName = item.Name ?? "未知";
        IsFolder = item.Folder != null;
        FileSize = FormatFileSize(item.Size ?? 0);
        MimeType = IsFolder ? "文件夹" : (item.File?.MimeType ?? "未知");
        CreatedTime = item.CreatedDateTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "未知";
        ModifiedTime = item.LastModifiedDateTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "未知";
        QuickXorHash = item.File?.Hashes?.QuickXorHash ?? "无";
        Id = item.Id ?? "未知";
        WebUrl = item.WebUrl ?? "无";
        IsWebUrlEnabled = !string.IsNullOrEmpty(item.WebUrl);
        CreatedBy = item.CreatedBy?.User?.DisplayName ?? "未知";
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        double len = bytes;
        var order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
