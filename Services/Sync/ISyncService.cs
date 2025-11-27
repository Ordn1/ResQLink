using System;
using System.Threading;
using System.Threading.Tasks;

namespace ResQLink.Services.Sync;

public interface ISyncService
{
    Task<bool> CheckOnlineAsync(CancellationToken ct = default);
    Task<(bool ok, string? error)> SyncNowAsync(CancellationToken ct = default);
    Task<(bool ok, string? error)> PushAsync(CancellationToken ct = default);
    Task<(bool ok, string? error)> PullAsync(CancellationToken ct = default);

    void StartAutoSync(TimeSpan interval);
    void StopAutoSync();

    TimeSpan? CurrentInterval { get; }
}
