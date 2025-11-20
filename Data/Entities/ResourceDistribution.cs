using System.ComponentModel.DataAnnotations.Schema;

namespace ResQLink.Data.Entities;

[Table("ResourceDistributions")]
public class ResourceDistribution
{
    public int DistributionId { get; set; }

    public int AllocationId { get; set; }
    public ResourceAllocation Allocation { get; set; } = null!;

    public int EvacueeId { get; set; }
    public Evacuee Evacuee { get; set; } = null!;

    public int DistributedByUserId { get; set; }
    public User DistributedBy { get; set; } = null!;

    public int DistributedQuantity { get; set; }
    public DateTime DistributedAt { get; set; } = DateTime.UtcNow;
}
