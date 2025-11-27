namespace ResQLink.Services.Sync;

public class SyncSettings
{
    // Enable/disable remote sync
    public bool RemoteEnabled { get; set; } = true;

    // Remote API base URL (optional if using HTTP-based sync)
    public string? RemoteBaseUrl { get; set; } = null;

    // Auto-sync interval minutes (0 = off)
    public int IntervalMinutes { get; set; } = 5;

    // Remote connection string for direct SQL sync (Windows only)
    public string? RemoteConnectionString { get; set; } = null;
}