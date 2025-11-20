using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResQLink.Data.Entities;

[Table("Donations")]
public class Donation
{
    [Key] public int DonationId { get; set; }

    public int DonorId { get; set; }
    public Donor Donor { get; set; } = null!;

    public int RecordedByUserId { get; set; }
    public User RecordedBy { get; set; } = null!;

    public int? DisasterId { get; set; }
    public Disaster? Disaster { get; set; }

    [Column(TypeName="decimal(12,2)")] public decimal Amount { get; set; }

    [MaxLength(100)] [Required] public string DonationType { get; set; } = string.Empty;
    [MaxLength(50)] [Required] public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
