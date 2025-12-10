namespace ResQLink.Services.Notifications;

/// <summary>
/// Notification types for disaster management events
/// </summary>
public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error,
    Critical
}

/// <summary>
/// Notification message model
/// </summary>
public class NotificationMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.Info;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? ActionUrl { get; set; }
    public string? ActionLabel { get; set; }
    public bool IsRead { get; set; }
    public int? UserId { get; set; }
    public string? Category { get; set; } // e.g., "Disaster", "Inventory", "Budget"
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Interface for notification service
/// </summary>
public interface INotificationService
{
    event EventHandler<NotificationMessage>? NotificationReceived;
    
    Task SendNotificationAsync(NotificationMessage notification);
    Task SendToUserAsync(int userId, NotificationMessage notification);
    Task SendToRoleAsync(string role, NotificationMessage notification);
    Task<List<NotificationMessage>> GetUserNotificationsAsync(int userId, int limit = 50);
    Task MarkAsReadAsync(string notificationId, int userId);
    Task MarkAllAsReadAsync(int userId);
    Task<int> GetUnreadCountAsync(int userId);
}
