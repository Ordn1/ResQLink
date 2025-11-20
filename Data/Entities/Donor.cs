    using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResQLink.Data.Entities;

[Table("Donors")]
public class Donor
{
    [Key]
    public int DonorId { get; set; }

    [MaxLength(255)] [Required] public string Name { get; set; } = string.Empty;
    [MaxLength(255)] public string? Email { get; set; }
    public bool IsOrganization { get; set; }
    [MaxLength(30)] public string? ContactNumber { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Donation> Donations { get; set; } = new List<Donation>();
}
