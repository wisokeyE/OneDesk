using OneDesk.Models;

namespace OneDesk.Services.Configuration;

public interface IConfigService
{
    AppConfig Config { get; }

    void WatchConfig();

    Task LoadAsync();
    Task SaveAsync();
}