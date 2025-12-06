using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResQLink.Data.Entities;

[Table("Disasters")]
public class Disaster : IValidatableObject
{
    [Key]
    public int DisasterId { get; set; }

    [MaxLength(255)] [Required] public string Title { get; set; } = string.Empty;
    [MaxLength(100)] [Required] public string DisasterType { get; set; } = string.Empty;
    [MaxLength(50)] [Required] public string Severity { get; set; } = string.Empty;
    [MaxLength(50)] [Required] public string Status { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Start Date")]
    public DateTime StartDate { get; set; }

    [Display(Name = "End Date")]
    public DateTime? EndDate { get; set; }

    [MaxLength(255)] [Required] public string Location { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Shelter> Shelters { get; set; } = new List<Shelter>();
    public ICollection<Evacuee> Evacuees { get; set; } = new List<Evacuee>();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Validate StartDate is not in the far future
        if (StartDate > DateTime.UtcNow.AddYears(1))
        {
            yield return new ValidationResult(
                "Start date cannot be more than 1 year in the future",
                new[] { nameof(StartDate) });
        }

        // Validate StartDate is not too far in the past (e.g., more than 50 years)
        if (StartDate < DateTime.UtcNow.AddYears(-50))
        {
            yield return new ValidationResult(
                "Start date seems unrealistic (more than 50 years ago)",
                new[] { nameof(StartDate) });
        }

        // Validate EndDate logic
        if (EndDate.HasValue)
        {
            if (EndDate.Value < StartDate)
            {
                yield return new ValidationResult(
                    "End date must be after start date",
                    new[] { nameof(EndDate) });
            }

            if (EndDate.Value > DateTime.UtcNow.AddYears(1))
            {
                yield return new ValidationResult(
                    "End date cannot be more than 1 year in the future",
                    new[] { nameof(EndDate) });
            }
        }

        // Validate Status and EndDate consistency
        if (Status.Equals("Closed", StringComparison.OrdinalIgnoreCase) && !EndDate.HasValue)
        {
            yield return new ValidationResult(
                "Closed disasters must have an end date",
                new[] { nameof(EndDate), nameof(Status) });
        }
    }
}
