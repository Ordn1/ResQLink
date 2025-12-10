using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResQLink.Data.Entities;

[Table("BarangayBudgets")]
public class BarangayBudget : IArchivable
{
    [Key] public int BudgetId { get; set; }

    [Required, MaxLength(255)]
    public string BarangayName { get; set; } = string.Empty;

    [Range(2000, 9999)]
    public int Year { get; set; }

    [Column(TypeName = "decimal(14,2)")]
    public decimal TotalAmount { get; set; }

    [Required, MaxLength(30)]
    public string Status { get; set; } = "Draft"; // Draft, Approved, Closed

    public int CreatedByUserId { get; set; }
    public User? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Archive tracking (IArchivable)
    public bool IsArchived { get; set; } = false;
    public DateTime? ArchivedAt { get; set; }
    public int? ArchivedBy { get; set; }
    [MaxLength(500)] public string? ArchiveReason { get; set; }

    public ICollection<BarangayBudgetItem> Items { get; set; } = new List<BarangayBudgetItem>();
}