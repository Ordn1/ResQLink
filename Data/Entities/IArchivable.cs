namespace ResQLink.Data.Entities;

/// <summary>
/// Interface for entities that support soft delete/archiving
/// </summary>
public interface IArchivable
{
    /// <summary>
    /// Indicates if the record is archived (soft deleted)
    /// </summary>
    bool IsArchived { get; set; }
    
    /// <summary>
    /// Date and time when the record was archived
    /// </summary>
    DateTime? ArchivedAt { get; set; }
    
    /// <summary>
    /// User who archived the record
    /// </summary>
    int? ArchivedBy { get; set; }
    
    /// <summary>
    /// Reason for archiving
    /// </summary>
    string? ArchiveReason { get; set; }
}
