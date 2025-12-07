using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResQLink.Data.Entities;

[Table("Stocks")]
public class Stock
{
  [Key]
  public int StockId { get; set; }

  // Foreign keys
  public int RgId { get; set; }
  public ReliefGood ReliefGood { get; set; } = null!;

  public int? DisasterId { get; set; }
  public Disaster? Disaster { get; set; }

  public int? ShelterId { get; set; }
  public Shelter? Shelter { get; set; }

  // Core fields
  public int Quantity { get; set; }

  public int MaxCapacity { get; set; } = 1000;

  [MaxLength(255)]
  public string? Location { get; set; }

  [Column(TypeName = "decimal(14,2)")]
  public decimal UnitCost { get; set; } = 0m;

  public bool IsActive { get; set; } = true;

  public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

  [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
  public decimal? CapacityPercent { get; private set; }

  [MaxLength(20)]
  [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
  public string? Status { get; private set; }

  public ICollection<ResourceAllocation> Allocations { get; set; } = [];

  public string StatusClass
  {
    get
    {
      var cp = CapacityPercent ?? 0m;
      return cp switch
      {
        > 100 => "st-ok",
        > 50 => "st-high",
        > 0 => "st-medium",
        _ => "st-empty",
      };
    }
  }
}
