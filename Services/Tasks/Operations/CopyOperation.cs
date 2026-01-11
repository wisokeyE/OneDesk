using System.Net;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph.Drives.Item.Items.Item.Copy;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions;
using Newtonsoft.Json;
using OneDesk.Helpers;
using OneDesk.Models.Tasks;
using Application = System.Windows.Application;

namespace OneDesk.Services.Tasks.Operations;

/// <summary>
/// 复制操作类（单例模式）
/// </summary>
public class CopyOperation(IServiceProvider serviceProvider) : ITaskOperation
{
    private static readonly Dictionary<string, object> EmptyDictionary = [];

    private ITaskScheduler TaskScheduler => field ??= serviceProvider.GetRequiredService<ITaskScheduler>();
    private HttpClient MonitorHttpClient => field ??= serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("MonitorCopy");

    /// <summary>
    /// 操作名称
    /// </summary>
    public string OperationName => "复制";

    /// <summary>
    /// 执行复制操作
    /// </summary>
    /// <param name="taskInfo">任务信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task ExecuteAsync(TaskInfo taskInfo, CancellationToken cancellationToken)
    {
        if (taskInfo.SourceItem.Id == null)
        {
            throw new InvalidOperationException("源项或源项 ID 不能为空");
        }

        if (taskInfo.DestinationItem?.Id == null)
        {
            throw new InvalidOperationException("目标项或目标项 ID 不能为空");
        }

        var sourceDriveId = CommonUtils.GetDriveId(taskInfo.SourceItem);
        var destinationDriveId = CommonUtils.GetDriveId(taskInfo.DestinationItem);

        // 判断源是文件还是文件夹
        if (taskInfo.SourceItem.Folder != null)
        {
            // 源是文件夹，需要递归复制
            await CopyFolderAsync(taskInfo, sourceDriveId, destinationDriveId, cancellationToken);
        }
        else
        {
            // 源是文件，直接复制
            await CopyFileAsync(taskInfo, sourceDriveId, destinationDriveId, cancellationToken);
        }
    }

    /// <summary>
    /// 复制文件
    /// </summary>
    private async Task CopyFileAsync(TaskInfo taskInfo, string sourceDriveId, string destinationDriveId, CancellationToken cancellationToken)
    {
        // 构造目标父文件夹引用
        var parentReference = new ItemReference
        {
            DriveId = destinationDriveId,
            Id = taskInfo.DestinationItem!.Id
        };

        // 执行复制操作
        var nativeResponseHandler = new NativeResponseHandler();

        var client = taskInfo.UserInfo.Client;
        await client.Drives[sourceDriveId]
            .Items[taskInfo.SourceItem.Id!]
            .Copy
            .PostAsync(new CopyPostRequestBody
            {
                ParentReference = parentReference,
                Name = taskInfo.SourceItem.Name,
                AdditionalData = CommonUtils.GetValueOrDefault(taskInfo.ExtraData, "AdditionalData", EmptyDictionary)
            }, requestConfiguration =>
            {
                requestConfiguration.Options.Add(new ResponseHandlerOption
                {
                    ResponseHandler = nativeResponseHandler
                });
            }, cancellationToken: cancellationToken);
        var responseMessage = nativeResponseHandler.Value as HttpResponseMessage;

        // 检查响应是否成功
        if (responseMessage is not { IsSuccessStatusCode: true })
        {
            var statusCode = responseMessage?.StatusCode.ToString() ?? "Unknown";
            throw new InvalidOperationException($"复制操作失败，HTTP 状态码: {statusCode}");
        }

        switch (responseMessage.StatusCode)
        {
            case HttpStatusCode.Accepted:
                {
                    // 复制操作异步进行，需轮询检查状态
                    var monitorUrl = responseMessage.Headers.Location ?? throw new InvalidOperationException("复制操作返回 202 Accepted，但未提供监控 URL");
                    await MonitorCopy(taskInfo, monitorUrl, cancellationToken);
                    break;
                }
            case HttpStatusCode.Created:
                // 复制操作同步完成
                break;
            default:
                throw new InvalidOperationException($"复制操作异常，HTTP 状态码: {responseMessage.StatusCode}");
        }
    }

    /// <summary>
    /// 复制文件夹
    /// </summary>
    private async Task CopyFolderAsync(TaskInfo taskInfo, string sourceDriveId, string destinationDriveId, CancellationToken cancellationToken)
    {
        // 检查目标下是否存在同名文件夹
        var client = taskInfo.UserInfo.Client;
        var children = await client.Drives[destinationDriveId]
            .Items[taskInfo.DestinationItem!.Id!]
            .Children
            .GetAsync(cancellationToken: cancellationToken);

        var existingFolder = children?.Value?.FirstOrDefault(item =>
            item.Name == taskInfo.SourceItem.Name && item.Folder != null);

        DriveItem targetFolder;

        if (existingFolder == null)
        {
            // 目标下不存在同名文件夹，创建新文件夹
            var newFolder = new DriveItem
            {
                Name = taskInfo.SourceItem.Name,
                Folder = new Folder()
            };

            targetFolder = await client.Drives[destinationDriveId]
                .Items[taskInfo.DestinationItem.Id!]
                .Children
                .PostAsync(newFolder, cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("创建目标文件夹失败");
        }
        else
        {
            // 目标下已存在同名文件夹，使用现有文件夹
            targetFolder = existingFolder;
        }

        // 获取源文件夹的所有子项
        var sourceChildren = await client.Drives[sourceDriveId]
            .Items[taskInfo.SourceItem.Id!]
            .Children
            .GetAsync(cancellationToken: cancellationToken);

        if (sourceChildren?.Value is not { Count: not 0 })
        {
            // 源文件夹为空，无需复制子项
            return;
        }

        // 遍历源文件夹的所有子项，为每个子项创建复制任务
        foreach (var childItem in sourceChildren.Value)
        {
            var childTaskInfo = new TaskInfo(taskInfo.UserInfo, this, childItem, targetFolder, taskInfo.ExtraData);
            await TaskScheduler.AddTaskAsync(childTaskInfo);
        }
    }

    private async Task MonitorCopy(TaskInfo taskInfo, Uri monitorUrl, CancellationToken cancellationToken)
    {
        var result = await MonitorHttpClient.GetAsync(monitorUrl, cancellationToken);

        if (!result.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"监控复制任务失败，HTTP 状态码: {result.StatusCode}");
        }

        var content = await result.Content.ReadAsStringAsync(cancellationToken);
        var dict = JsonConvert.DeserializeObject<IDictionary<string, object>>(content);
        var status = CommonUtils.GetValueOrDefault(dict, "status", "completed");
        await UpdateProgress(taskInfo, CommonUtils.GetValueOrDefault(dict, "percentageComplete", 0.0));

        // 轮询检查任务状态
        while (status is "notStarted" or "inProgress" or "waiting")
        {
            await Task.Delay(200, cancellationToken);

            result = await MonitorHttpClient.GetAsync(monitorUrl, cancellationToken);
            if (!result.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"监控复制任务失败，HTTP 状态码: {result.StatusCode}");
            }

            content = await result.Content.ReadAsStringAsync(cancellationToken);
            dict = JsonConvert.DeserializeObject<IDictionary<string, object>>(content);
            status = CommonUtils.GetValueOrDefault(dict, "status", "completed");
            await UpdateProgress(taskInfo, CommonUtils.GetValueOrDefault(dict, "percentageComplete", 0.0));
        }

        if (status == "failed")
        {
            var errorMessage = CommonUtils.GetValueOrDefault(dict, "error", "未知错误");
            throw new InvalidOperationException($"复制任务失败: {errorMessage}");
        }

        if (status != "completed")
        {
            throw new InvalidOperationException($"复制任务结束，但状态异常: {status}");
        }
    }

    private static async Task UpdateProgress(TaskInfo taskInfo, double progress)
    {
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            taskInfo.Progress = progress;
        });
    }
}
