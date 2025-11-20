using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResQLink.Data.Entities;

[Table("Users")]
public class User
{
    [Key]
    [Column("user_id")]
    public int UserId { get; set; }

    // Alias for legacy code expecting a generic 'Id'
    [NotMapped]
    public int Id => UserId;

    [Required]
    [MaxLength(56)]
    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(56)]
    [Column("password")]
    public string Password { get; set; } = string.Empty;

    // Legacy hashing members (not present in current DB schema)
    [NotMapped]
    public string PasswordHash { get; set; } = string.Empty;

    [NotMapped]
    public string PasswordSalt { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column("role_id")]
    public int? RoleId { get; set; } // Nullable per schema (no NOT NULL constraint)

    [ForeignKey(nameof(RoleId))]
    public UserRole? Role { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Convenience UTC alias referenced elsewhere
    [NotMapped]
    public DateTime CreatedUtc => CreatedAt;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}