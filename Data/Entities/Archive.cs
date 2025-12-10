using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResQLink.Data.Entities;

/// <summary>
/// Centralized archive table for storing soft-deleted records from any entity
/// </summary>
public class Archive
{
    [Key]
    public int ArchiveId { get; set; }

    /// <summary>
    /// Type of entity that was archived (e.g., "ReliefGood", "Category", "Disaster")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Primary key value of the archived entity
    /// </summary>
    [Required]
    public int EntityId { get; set; }

    /// <summary>
    /// JSON serialized data of the archived entity
    /// </summary>
    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public string ArchivedData { get; set; } = string.Empty;

    /// <summary>
    /// Date and time when the record was archived
    /// </summary>
    [Required]
    public DateTime ArchivedAt { get; set; }

    /// <summary>
    /// User who archived the record
    /// </summary>
    [Required]
    public int ArchivedBy { get; set; }

    /// <summary>
    /// Navigation property to the user who archived
    /// </summary>
    [ForeignKey(nameof(ArchivedBy))]
    public User? ArchivedByUser { get; set; }

    /// <summary>
    /// Reason for archiving
    /// </summary>
    [MaxLength(500)]
    public string? ArchiveReason { get; set; }

    /// <summary>
    /// Optional: Original entity name for display purposes
    /// </summary>
    [MaxLength(200)]
    public string? EntityName { get; set; }
}
