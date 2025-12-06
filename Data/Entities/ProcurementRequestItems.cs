using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResQLink.Data.Entities;

[Table("ProcurementRequestItems")]
public class ProcurementRequestItem
{
    [Key] public int RequestItemId { get; set; }

    public int RequestId { get; set; }
    public ProcurementRequest Request { get; set; } = null!;

    [Required, MaxLength(255)]
    public string ItemName { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Unit { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Column(TypeName = "decimal(14,2)")]
    public decimal UnitPrice { get; set; }

    [NotMapped]
    public decimal LineTotal => UnitPrice * Quantity;
}