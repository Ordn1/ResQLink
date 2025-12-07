using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResQLink.Data.Entities;

[Table("ResourceAllocations")]
public class ResourceAllocation
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int AllocationId { get; set; }
    
    public int StockId { get; set; }
    public Stock Stock { get; set; } = null!;

    public int ShelterId { get; set; }
    public Shelter Shelter { get; set; } = null!;

    public int AllocatedByUserId { get; set; }
    public User AllocatedBy { get; set; } = null!;

    public int AllocatedQuantity { get; set; }
    public DateTime AllocatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ResourceDistribution> Distributions { get; set; } = new List<ResourceDistribution>();
}
