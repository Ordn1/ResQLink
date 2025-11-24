using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResQLink.Data.Entities;

[Table("Category_Types")]
public class CategoryType
{
    [Key]
    public short CategoryTypeId { get; set; }

    [MaxLength(50)]
    [Required]
    public string TypeName { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "char(1)")]
    public char TypeCode { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Category> Categories { get; set; } = [];
}
