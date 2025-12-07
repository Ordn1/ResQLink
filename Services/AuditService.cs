using Microsoft.EntityFrameworkCore;
using ResQLink.Data;
using ResQLink.Data.Entities;
using System.Text.Json;

namespace ResQLink.Services;

public class AuditService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public AuditService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task LogAsync(
        string action,
        string entityType,
        int? entityId = null,
        int? userId = null,
        string? userType = null,
        string? userName = null,
        object? oldValues = null,
        object? newValues = null,
        string? description = null,
        string severity = "Info",
        bool isSuccessful = true,
        string? errorMessage = null)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            // For MAUI apps, we can capture device info instead of HTTP context
            string? deviceInfo = GetDeviceInfo();
            string? sessionId = GetSessionId();

            var auditLog = new AuditLog
            {
                Timestamp = DateTime.UtcNow,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                UserId = userId,
                UserType = userType,
                UserName = userName,
                IpAddress = "MAUI-App", // MAUI apps don't have IP addresses like web apps
                UserAgent = deviceInfo,
                OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
                NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
                Description = description,
                Severity = severity,
                IsSuccessful = isSuccessful,
                ErrorMessage = errorMessage,
                SessionId = sessionId
            };

            context.AuditLogs.Add(auditLog);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Log to console/file as fallback - don't throw to avoid breaking main operations
            Console.WriteLine($"[AUDIT LOG ERROR] {DateTime.UtcNow}: Failed to log audit entry. Action: {action}, Entity: {entityType}, Error: {ex.Message}");
        }
    }

    private string GetDeviceInfo()
    {
        try
        {
            var deviceInfo = $"{DeviceInfo.Current.Platform} {DeviceInfo.Current.VersionString} | " +
                           $"{DeviceInfo.Current.Manufacturer} {DeviceInfo.Current.Model} | " +
                           $"App v{AppInfo.Current.VersionString}";
            return deviceInfo;
        }
        catch
        {
            return "Unknown Device";
        }
    }

    private string GetSessionId()
    {
        try
        {
            // Generate a session ID based on app instance
            // You could store this in Preferences for the session lifetime
            var sessionKey = "AuditSessionId";
            if (!Preferences.ContainsKey(sessionKey))
            {
                var newSessionId = Guid.NewGuid().ToString();
                Preferences.Set(sessionKey, newSessionId);
                return newSessionId;
            }
            return Preferences.Get(sessionKey, Guid.NewGuid().ToString());
        }
        catch
        {
            return Guid.NewGuid().ToString();
        }
    }

    public async Task<List<AuditLog>> GetLogsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? action = null,
        string? entityType = null,
        int? userId = null,
        string? severity = null,
        int limit = 100)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var query = context.AuditLogs.AsNoTracking().AsQueryable();

        if (startDate.HasValue)
            query = query.Where(a => a.Timestamp >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(a => a.Timestamp <= endDate.Value);

        if (!string.IsNullOrEmpty(action))
            query = query.Where(a => a.Action == action);

        if (!string.IsNullOrEmpty(entityType))
            query = query.Where(a => a.EntityType == entityType);

        if (userId.HasValue)
            query = query.Where(a => a.UserId == userId.Value);

        if (!string.IsNullOrEmpty(severity))
            query = query.Where(a => a.Severity == severity);

        return await query
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<AuditLog>> GetEntityHistoryAsync(string entityType, int entityId, int limit = 50)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.AuditLogs
            .AsNoTracking()
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<AuditLog>> GetUserActivityAsync(int userId, int limit = 100)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.AuditLogs
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .ToListAsync();
    }

    /// <summary>
    /// Clear the current session ID to start a new session
    /// </summary>
    public void ClearSession()
    {
        try
        {
            Preferences.Remove("AuditSessionId");
        }
        catch
        {
            // Ignore errors
        }
    }
}