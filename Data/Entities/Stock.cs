using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResQLink.Data.Entities;

[Table("Stocks")]
public class Stock
{
    public int StockId { get; set; }
    public int RgId { get; set; }
    public ReliefGood ReliefGood { get; set; } = null!;

    public int? DisasterId { get; set; }
    public Disaster? Disaster { get; set; }

    public int? ShelterId { get; set; }
    public Shelter? Shelter { get; set; }

    public int Quantity { get; set; }

    [MaxLength(255)] public string? Location { get; set; }

    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    public ICollection<ResourceAllocation> Allocations { get; set; } = new List<ResourceAllocation>();
}
