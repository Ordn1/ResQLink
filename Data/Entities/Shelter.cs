using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResQLink.Data.Entities;

[Table("Shelters")]
public class Shelter
{
    [Key]
    public int ShelterId { get; set; }

    public int? DisasterId { get; set; }
    public Disaster? Disaster { get; set; }

    [MaxLength(255)] [Required] public string Name { get; set; } = string.Empty;

    public int Capacity { get; set; }
    public int CurrentOccupancy { get; set; }

    [MaxLength(255)] public string? Location { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Evacuee> Evacuees { get; set; } = new List<Evacuee>();
}
