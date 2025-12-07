using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResQLink.Data.Entities;

[Table("UserRoles")]
public class UserRole
{
    [Key]
    public int RoleId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string RoleName { get; set; } = string.Empty;
    
    [MaxLength(255)]
    public string? Description { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public ICollection<User> Users { get; set; } = new List<User>();
}