using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using OneDesk.Models;

namespace OneDesk.Services.Configuration;

public class ConfigService : IConfigService, IDisposable
{
    private readonly string _configFilePath = Path.Combine(AppContext.BaseDirectory, "appConfig.json");
    private readonly SemaphoreSlim _saveLock = new(1, 1);
    private readonly JsonSerializerSettings _jsonSettings = new()
    {
        Formatting = Formatting.Indented,
        Converters = { new StringEnumConverter() }
    };

    public AppConfig Config { get; private set; } = new();

    public void WatchConfig()
    {
        Config.OnConfigChanged += async (s, e) => { await SaveAsync(); };
    }

    public async Task LoadAsync()
    {
        if (!File.Exists(_configFilePath))
        {
            // 首次运行，创建默认配置
            await SaveAsync();
            return;
        }

        var json = await File.ReadAllTextAsync(_configFilePath);
        var loaded = JsonConvert.DeserializeObject<AppConfig>(json, _jsonSettings);
        if (loaded != null)
        {
            Config.CopyConfig(loaded);
        }
    }

    public async Task SaveAsync()
    {
        await _saveLock.WaitAsync();
        try
        {
            var json = JsonConvert.SerializeObject(Config, _jsonSettings);
            await File.WriteAllTextAsync(_configFilePath, json);
        }
        finally
        {
            _saveLock.Release();
        }
    }

    public void Dispose()
    {
        _saveLock.Dispose();
        GC.SuppressFinalize(this);
    }
}