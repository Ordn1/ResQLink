using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResQLink.Data.Entities;

[Table("AuditLogs")]
public class AuditLog
{
    [Key]
    public int AuditLogId { get; set; }

    [Required]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty; // Login, Logout, Create, Update, Delete, View, Assign, Unassign

    [Required]
    [MaxLength(100)]
    public string EntityType { get; set; } = string.Empty; // Volunteer, Shelter, Disaster, Donation, etc.

    public int? EntityId { get; set; } // ID of the affected entity

    public int? UserId { get; set; } // User who performed the action

    [MaxLength(50)]
    public string? UserType { get; set; } // Admin, Volunteer, Donor

    [MaxLength(200)]
    public string? UserName { get; set; } // Full name or email for easy reference

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    public string? OldValues { get; set; } // JSON of previous values for updates

    public string? NewValues { get; set; } // JSON of new values

    [MaxLength(1000)]
    public string? Description { get; set; } // Human-readable description

    [MaxLength(50)]
    public string Severity { get; set; } = "Info"; // Info, Warning, Error, Critical

    public bool IsSuccessful { get; set; } = true;

    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }

    [MaxLength(100)]
    public string? SessionId { get; set; }
}
