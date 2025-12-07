using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResQLink.Data.Entities;

[Table("ResourceDistributions")]
public class ResourceDistribution
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int DistributionId { get; set; }

    [ForeignKey(nameof(Allocation))]
    public int AllocationId { get; set; }
    public ResourceAllocation? Allocation { get; set; }

    [ForeignKey(nameof(Evacuee))]
    public int EvacueeId { get; set; }
    public Evacuee? Evacuee { get; set; }

    [ForeignKey(nameof(DistributedBy))]
    public int DistributedByUserId { get; set; }
    public User? DistributedBy { get; set; }

    public int DistributedQuantity { get; set; }
    public DateTime DistributedAt { get; set; } = DateTime.UtcNow;
}
