using System.IO;
using System.Reflection;
using System.Text;
using Azure.Core;
using Azure.Identity;
using OneDesk.Helpers;
using Wpf.Ui.Controls;
using Application = System.Windows.Application;
using MessageBox = Wpf.Ui.Controls.MessageBox;

namespace OneDesk.Services.Auth;

public class FileBackDeviceCodeCredential(DeviceCodeCredentialOptions options, string filePath) : DeviceCodeCredential(DealCacheOptions(options, filePath))
{
    private string _serializeRecord = "";

    private AuthenticationRecord? Record => typeof(DeviceCodeCredential)
        .GetProperty("Record", BindingFlags.Instance | BindingFlags.NonPublic)?
        .GetValue(this) as AuthenticationRecord;

    private static DeviceCodeCredentialOptions DealCacheOptions(DeviceCodeCredentialOptions options, string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return options;
        options.TokenCachePersistenceOptions ??= new TokenCachePersistenceOptions();
        options.DeviceCodeCallback ??= async (info, ct) => await DefaultDeviceCodeCallback(options, info, ct);
        if (options.AuthenticationRecord is not null || !File.Exists(filePath)) return options;
        try
        {
            using var authRecordStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            options.AuthenticationRecord = AuthenticationRecord.Deserialize(authRecordStream);
        }
        catch (Exception)
        {
            // ignored
        }

        return options;
    }

    private void SaveRecord(AuthenticationRecord? record)
    {
        if (string.IsNullOrWhiteSpace(filePath) || record is null) return;
        options.AuthenticationRecord = record;
        // 将 AuthenticationRecord 先序列化成字符串，对比是否有变化，避免重复写文件
        using var memStream = new MemoryStream();
        record.Serialize(memStream);
        var serialized = Encoding.UTF8.GetString(memStream.ToArray());
        if (serialized == _serializeRecord) return;
        _serializeRecord = serialized;
        var directory = Path.GetDirectoryName(Path.GetFullPath(filePath));
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        using var authRecordStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        record.Serialize(authRecordStream);
    }

    public override AuthenticationRecord Authenticate(TokenRequestContext requestContext, CancellationToken cancellationToken = default)
    {
        var record = base.Authenticate(requestContext, cancellationToken);
        SaveRecord(record);
        return record;
    }

    public override async Task<AuthenticationRecord> AuthenticateAsync(TokenRequestContext requestContext, CancellationToken cancellationToken = default)
    {
        var record = await base.AuthenticateAsync(requestContext, cancellationToken);
        SaveRecord(record);
        return record;
    }

    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken = default)
    {
        var accessToken = base.GetToken(requestContext, cancellationToken);
        SaveRecord(Record);
        return accessToken;
    }

    public override async ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken = default)
    {
        var accessToken = await base.GetTokenAsync(requestContext, cancellationToken);
        SaveRecord(Record);
        return accessToken;
    }

    private static async Task DefaultDeviceCodeCallback(DeviceCodeCredentialOptions options, DeviceCodeInfo info, CancellationToken ct)
    {
        var userName = options.AuthenticationRecord?.Username;
        // 若被取消，尽快返回
        if (ct.IsCancellationRequested) return;

        var who = !string.IsNullOrWhiteSpace(userName) ? $"将为用户：{userName} 登录。\n" : "这是一个新的登录，请登录一个新用户。\n";

        await CommonUtils.ShowMessageBoxAsync("登录提示", $"{who}请在浏览器打开: {info.VerificationUri}\n并输入代码: {info.UserCode}\n（有效期至: {info.ExpiresOn}）", ct);
    }

}