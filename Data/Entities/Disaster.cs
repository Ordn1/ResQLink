using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResQLink.Data.Entities;

[Table("Disasters")]
public class Disaster
{
    [Key]
    public int DisasterId { get; set; }

    [MaxLength(255)] [Required] public string Title { get; set; } = string.Empty;
    [MaxLength(100)] [Required] public string DisasterType { get; set; } = string.Empty;
    [MaxLength(50)] [Required] public string Severity { get; set; } = string.Empty;
    [MaxLength(50)] [Required] public string Status { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    [MaxLength(255)] [Required] public string Location { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Shelter> Shelters { get; set; } = new List<Shelter>();
    public ICollection<Evacuee> Evacuees { get; set; } = new List<Evacuee>();
}
