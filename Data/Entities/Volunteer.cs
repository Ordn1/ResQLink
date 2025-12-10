using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResQLink.Data.Entities;

[Table("Volunteers")]
public class Volunteer
{
    [Key]
    public int VolunteerId { get; set; }

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? PasswordHash { get; set; } // Add ? to make it nullable

    [MaxLength(30)]
    public string? ContactNumber { get; set; }

    [MaxLength(255)]
    public string? Address { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Active";

    [MaxLength(255)]
    public string? Skills { get; set; }

    [MaxLength(50)]
    public string? Availability { get; set; }

    public int? AssignedShelterId { get; set; }
    [ForeignKey(nameof(AssignedShelterId))]
    public Shelter? AssignedShelter { get; set; }

    public int? AssignedDisasterId { get; set; }
    [ForeignKey(nameof(AssignedDisasterId))]
    public Disaster? AssignedDisaster { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    
    // Security: Track failed login attempts
    public int FailedLoginAttempts { get; set; } = 0;
    
    // Security: Account lockout until this time (null if not locked)
    public DateTime? LockoutEnd { get; set; }
    
    // Security: Force password reset on next login
    public bool RequiresPasswordReset { get; set; } = false;
}