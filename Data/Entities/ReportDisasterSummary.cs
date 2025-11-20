using System.ComponentModel.DataAnnotations.Schema;

namespace ResQLink.Data.Entities;

[Table("ReportDisasterSummary")]
public class ReportDisasterSummary
{
    public int ReportId { get; set; }
    public int DisasterId { get; set; }
    public Disaster Disaster { get; set; } = null!;

    public int TotalEvacuees { get; set; }
    public decimal TotalDonations { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
