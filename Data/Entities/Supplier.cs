using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResQLink.Data.Entities;

[Table("Suppliers")]
public class Supplier
{
    [Key]
    public int SupplierId { get; set; }

    [MaxLength(255)] [Required] public string SupplierName { get; set; } = string.Empty;
    [MaxLength(255)] public string? ContactPerson { get; set; }
    [MaxLength(255)] public string? Email { get; set; }
    [MaxLength(30)] public string? PhoneNumber { get; set; }
    [MaxLength(500)] public string? Address { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}   