using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResQLink.Data.Entities;

[Table("Relief_Goods")]
public class ReliefGood
{
    [Key]
    public int RgId { get; set; }

    [MaxLength(255)] [Required] public string Name { get; set; } = string.Empty;
    [MaxLength(50)] [Required] public string Unit { get; set; } = string.Empty;
    [MaxLength(255)] public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ReliefGoodCategory> Categories { get; set; } = new List<ReliefGoodCategory>();
    public ICollection<Stock> Stocks { get; set; } = new List<Stock>();
}
