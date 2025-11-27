using Microsoft.Maui.Storage;
using System.Runtime.Versioning;

namespace ResQLink.Services.Sync;

public class PreferencesSyncSettingsStorage : ISyncSettingsStorage
{
    private const string Prefix = "sync_";

    // Preferences is available across MAUI platforms; suppress analyzer for shared component
    [SupportedOSPlatform("android")] // hint to analyzer; MAUI provides cross-platform implementation
    [SupportedOSPlatform("ios")]
    [SupportedOSPlatform("maccatalyst")]
    [SupportedOSPlatform("windows")]
    public void LoadInto(SyncSettings target)
    {
        target.RemoteEnabled = Preferences.Get(Prefix + nameof(SyncSettings.RemoteEnabled), target.RemoteEnabled);
        target.RemoteBaseUrl = Preferences.Get(Prefix + nameof(SyncSettings.RemoteBaseUrl), target.RemoteBaseUrl ?? string.Empty);
        target.IntervalMinutes = Preferences.Get(Prefix + nameof(SyncSettings.IntervalMinutes), target.IntervalMinutes);
        if (string.IsNullOrWhiteSpace(target.RemoteBaseUrl)) target.RemoteBaseUrl = null;
        // Do not load RemoteConnectionString from preferences for security (keep code-only)
    }

    [SupportedOSPlatform("android")]
    [SupportedOSPlatform("ios")]
    [SupportedOSPlatform("maccatalyst")]
    [SupportedOSPlatform("windows")]
    public void Save(SyncSettings source)
    {
        Preferences.Set(Prefix + nameof(SyncSettings.RemoteEnabled), source.RemoteEnabled);
        Preferences.Set(Prefix + nameof(SyncSettings.RemoteBaseUrl), source.RemoteBaseUrl ?? string.Empty);
        Preferences.Set(Prefix + nameof(SyncSettings.IntervalMinutes), source.IntervalMinutes);
        // Do not save RemoteConnectionString
    }
}
