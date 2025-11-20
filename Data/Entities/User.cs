using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResQLink.Data.Entities;

[Table("Users")]
public class User
{
    [Key]
    public int UserId { get; set; }

    // Convenience alias
    [NotMapped]
    public int Id => UserId;

    [Required]
    [MaxLength(56)]
    public string Username { get; set; } = string.Empty;

    // New schema stores hashed password only
    [Required]
    [MaxLength(256)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    // NOT NULL per schema
    public int RoleId { get; set; }
    public UserRole? Role { get; set; }

    public bool IsActive { get; set; } = true;

    // Defaults set in DB (SYSUTCDATETIME())
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}