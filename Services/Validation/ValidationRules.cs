using ResQLink.Data;
using Microsoft.EntityFrameworkCore;

namespace ResQLink.Services.Validation;

public interface IValidationRules
{
    Task<bool> StringLengthValidAsync(string? value, int maxLength, string fieldName, List<ValidationError> errors);
    Task<bool> RequiredStringAsync(string? value, string fieldName, List<ValidationError> errors);
    Task<bool> RangeValidAsync(int value, int min, int max, string fieldName, List<ValidationError> errors);
    Task<bool> ForeignKeyExistsAsync<T>(int? id, string entityName, List<ValidationError> errors) where T : class;
}

public class ValidationRules : IValidationRules
{
    private readonly AppDbContext _db;

    public ValidationRules(AppDbContext db)
    {
        _db = db;
    }

    public Task<bool> StringLengthValidAsync(string? value, int maxLength, string fieldName, List<ValidationError> errors)
    {
        if (value?.Length > maxLength)
        {
            errors.Add(new ValidationError(fieldName, $"{fieldName} cannot exceed {maxLength} characters.", ValidationErrorType.Length));
            return Task.FromResult(false);
        }
        return Task.FromResult(true);
    }

    public Task<bool> RequiredStringAsync(string? value, string fieldName, List<ValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new ValidationError(fieldName, $"{fieldName} is required.", ValidationErrorType.Required));
            return Task.FromResult(false);
        }
        return Task.FromResult(true);
    }

    public Task<bool> RangeValidAsync(int value, int min, int max, string fieldName, List<ValidationError> errors)
    {
        if (value < min || value > max)
        {
            errors.Add(new ValidationError(fieldName, $"{fieldName} must be between {min} and {max}.", ValidationErrorType.Range));
            return Task.FromResult(false);
        }
        return Task.FromResult(true);
    }

    public async Task<bool> ForeignKeyExistsAsync<T>(int? id, string entityName, List<ValidationError> errors) where T : class
    {
        if (!id.HasValue || id.Value <= 0)
            return true; // Allow null/zero for optional FKs

        var exists = await _db.Set<T>().AnyAsync(e => EF.Property<int>(e, $"{entityName}Id") == id.Value);
        if (!exists)
        {
            errors.Add(new ValidationError(entityName, $"Selected {entityName} does not exist.", ValidationErrorType.ForeignKey));
            return false;
        }
        return true;
    }
}