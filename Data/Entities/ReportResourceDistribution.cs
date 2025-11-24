using System.ComponentModel.DataAnnotations.Schema;

namespace ResQLink.Data.Entities;

[Table("ReportResourceDistribution")]
public class ReportResourceDistribution
{
    public int ReportId { get; set; }
    public int DisasterId { get; set; }
    public Disaster Disaster { get; set; } = null!;

    public int ShelterId { get; set; }
    public Shelter Shelter { get; set; } = null!;

    public string DistributedItems { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
