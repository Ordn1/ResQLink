using System.ComponentModel.DataAnnotations.Schema;

namespace ResQLink.Data.Entities;

[Table("AuditLogs")]
public class AuditLog
{
    public int LogId { get; set; }
    public int? UserId { get; set; }
    public User? User { get; set; }

    public string Action { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    public string? Meta { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
