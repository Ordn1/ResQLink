using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResQLink.Data.Entities;

[Table("UserRoles")]
public class UserRole
{
    [Key]
    [Column("role_id")]
    public int RoleId { get; set; }

    [Required]
    [Column("role_name")]
    public string RoleName { get; set; } = string.Empty;
}