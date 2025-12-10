using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResQLink.Data.Entities;

[Table("Categories")]
public class Category : IArchivable
{
    [Key]
    public int CategoryId { get; set; }

    [MaxLength(100)] [Required] public string CategoryName { get; set; } = string.Empty;
    [MaxLength(255)] public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Archive tracking (IArchivable)
    public bool IsArchived { get; set; } = false;
    public DateTime? ArchivedAt { get; set; }
    public int? ArchivedBy { get; set; }
    [MaxLength(500)] public string? ArchiveReason { get; set; }

    public short? CategoryTypeId { get; set; }
    public CategoryType? CategoryType { get; set; }

    public ICollection<ReliefGoodCategory> ReliefGoods { get; set; } = [];
}
