namespace ResQLink.Models;

public class Volunteer
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string EmergencyContactName { get; set; } = string.Empty;
    public string EmergencyContactPhone { get; set; } = string.Empty;
    public string Skills { get; set; } = string.Empty;
    public string AvailabilitySchedule { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending, Active, Inactive
    public DateTime RegistrationDate { get; set; } = DateTime.Now;
    public string? AssignedRole { get; set; }
    public int? AssignedShelterId { get; set; }
    public string? Notes { get; set; }
}