namespace ResQLink.Data.Entities;

public class User
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Role { get; set; } = "Staff"; // Admin | Staff | Volunteer

    public string PasswordHash { get; set; } = string.Empty;

    public string PasswordSalt { get; set; } = string.Empty;

    public string? Email { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedUtc { get; set; }
}