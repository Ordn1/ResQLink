using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResQLink.Data.Entities;

[Table("UserProfiles")]
public class UserProfile
{
    [Key]
    public int UserProfileId { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    [MaxLength(56)] public string? FirstName { get; set; }
    [MaxLength(56)] public string? LastName { get; set; }
    [MaxLength(56)] public string? MiddleName { get; set; }

    public DateTime? DateOfBirth { get; set; }

    [MaxLength(1)] public string? Gender { get; set; } // CK enforced in DB
    [MaxLength(20)] public string? MaritalStatus { get; set; }

    [MaxLength(30)] public string? ContactNumber { get; set; }
    [MaxLength(500)] public string? Address { get; set; }

    // Computed by DB (Age AS ...); represent read-only
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public int? Age { get; set; }
}
