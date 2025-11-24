using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResQLink.Data.Entities;

[Table("Evacuees")]
public class Evacuee
{
    [Key]
    public int EvacueeId { get; set; }

    public int DisasterId { get; set; }
    public Disaster Disaster { get; set; } = null!;

    public int? ShelterId { get; set; }
    public Shelter? Shelter { get; set; }

    [MaxLength(100)] [Required] public string FirstName { get; set; } = string.Empty;
    [MaxLength(100)] [Required] public string LastName { get; set; } = string.Empty;

    [MaxLength(50)] [Required] public string Status { get; set; } = string.Empty;

    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    public ICollection<ResourceDistribution> Distributions { get; set; } = new List<ResourceDistribution>();
}
