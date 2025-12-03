using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResQLink.Data.Entities;

[Table("ProcurementRequests")]
public class ProcurementRequest
{
    [Key] public int RequestId { get; set; }

    [Required, MaxLength(255)]
    public string BarangayName { get; set; } = string.Empty;

    public int? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public int RequestedByUserId { get; set; }
    public User? RequestedBy { get; set; }

    [Required, MaxLength(30)]
    public string Status { get; set; } = "Draft"; // Draft, Submitted, Approved, Rejected, Ordered

    public DateTime RequestDate { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "decimal(14,2)")]
    public decimal TotalAmount { get; set; }

    public ICollection<ProcurementRequestItem> Items { get; set; } = new List<ProcurementRequestItem>();
}