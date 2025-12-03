using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResQLink.Data.Entities;

[Table("BarangayBudgetItems")]
public class BarangayBudgetItem
{
    [Key] public int BudgetItemId { get; set; }

    public int BudgetId { get; set; }
    public BarangayBudget Budget { get; set; } = null!;

    [Required, MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string Description { get; set; } = string.Empty;

    [Column(TypeName = "decimal(14,2)")]
    public decimal Amount { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}