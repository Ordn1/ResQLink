using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResQLink.Data.Entities;

[Table("UserRoles")]
public class UserRole
{
    [Key]
    [Column("RoleId")]
    public int RoleId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("RoleName")]
    public string RoleName { get; set; } = string.Empty;

    [MaxLength(255)]
    [Column("description")]
    public string? Description { get; set; }

    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<User> Users { get; set; } = new List<User>();
}