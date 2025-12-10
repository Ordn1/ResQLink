using Microsoft.EntityFrameworkCore;
using ResQLink.Data;
using System.Collections.Concurrent;

namespace ResQLink.Services.Notifications;

/// <summary>
/// In-memory notification service for real-time alerts
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ConcurrentDictionary<int, List<NotificationMessage>> _userNotifications = new();
    
    public event EventHandler<NotificationMessage>? NotificationReceived;

    public NotificationService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task SendNotificationAsync(NotificationMessage notification)
    {
        // Broadcast to all connected users
        NotificationReceived?.Invoke(this, notification);
        
        // Store in memory if user-specific
        if (notification.UserId.HasValue)
        {
            var userId = notification.UserId.Value;
            _userNotifications.AddOrUpdate(
                userId,
                new List<NotificationMessage> { notification },
                (key, existing) =>
                {
                    existing.Insert(0, notification);
                    // Keep only last 100 notifications per user
                    return existing.Take(100).ToList();
                });
        }

        await Task.CompletedTask;
    }

    public async Task SendToUserAsync(int userId, NotificationMessage notification)
    {
        notification.UserId = userId;
        await SendNotificationAsync(notification);
    }

    public async Task SendToRoleAsync(string role, NotificationMessage notification)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var userIds = await context.Users
            .Include(u => u.Role)
            .Where(u => u.Role != null && u.Role.RoleName == role && u.IsActive)
            .Select(u => u.UserId)
            .ToListAsync();

        foreach (var userId in userIds)
        {
            var userNotification = new NotificationMessage
            {
                Title = notification.Title,
                Message = notification.Message,
                Type = notification.Type,
                Timestamp = notification.Timestamp,
                ActionUrl = notification.ActionUrl,
                ActionLabel = notification.ActionLabel,
                Category = notification.Category,
                Metadata = notification.Metadata,
                UserId = userId
            };
            
            await SendNotificationAsync(userNotification);
        }
    }

    public Task<List<NotificationMessage>> GetUserNotificationsAsync(int userId, int limit = 50)
    {
        if (_userNotifications.TryGetValue(userId, out var notifications))
        {
            return Task.FromResult(notifications.Take(limit).ToList());
        }
        
        return Task.FromResult(new List<NotificationMessage>());
    }

    public Task MarkAsReadAsync(string notificationId, int userId)
    {
        if (_userNotifications.TryGetValue(userId, out var notifications))
        {
            var notification = notifications.FirstOrDefault(n => n.Id == notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
            }
        }
        
        return Task.CompletedTask;
    }

    public Task MarkAllAsReadAsync(int userId)
    {
        if (_userNotifications.TryGetValue(userId, out var notifications))
        {
            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }
        }
        
        return Task.CompletedTask;
    }

    public Task<int> GetUnreadCountAsync(int userId)
    {
        if (_userNotifications.TryGetValue(userId, out var notifications))
        {
            return Task.FromResult(notifications.Count(n => !n.IsRead));
        }
        
        return Task.FromResult(0);
    }
}
