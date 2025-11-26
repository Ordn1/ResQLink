using Microsoft.EntityFrameworkCore;
using ResQLink.Data;
using System.ComponentModel.DataAnnotations;

namespace ResQLink.Services.Validation;

public class ValidationService : IValidationService
{
    private readonly AppDbContext _db;

    public ValidationService(AppDbContext db)
    {
        _db = db;
    }

    public ValidationResult ValidateModel<T>(T model) where T : class
    {
        var context = new ValidationContext(model);
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        
        if (Validator.TryValidateObject(model, context, results, validateAllProperties: true))
        {
            return ValidationResult.Success();
        }

        var errors = results.Select(r => new ValidationError(
            r.MemberNames.FirstOrDefault() ?? string.Empty,
            r.ErrorMessage ?? "Validation failed",
            DetermineErrorType(r.ErrorMessage)
        )).ToList();

        return ValidationResult.Failure(errors.ToArray());
    }

    public async Task<ValidationResult> ValidateModelAsync<T>(T model) where T : class
    {
        // Start with data annotations validation
        var result = ValidateModel(model);
        if (!result.IsValid)
            return result;

        // Add custom async validations here
        return result;
    }

    public ValidationResult ValidateProperty<T>(T model, string propertyName, object? value) where T : class
    {
        var context = new ValidationContext(model) { MemberName = propertyName };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        
        if (Validator.TryValidateProperty(value, context, results))
        {
            return ValidationResult.Success();
        }

        var errors = results.Select(r => new ValidationError(
            propertyName,
            r.ErrorMessage ?? "Validation failed",
            DetermineErrorType(r.ErrorMessage)
        )).ToList();

        return ValidationResult.Failure(errors.ToArray());
    }

    public bool IsValid<T>(T model) where T : class
    {
        return ValidateModel(model).IsValid;
    }

    private ValidationErrorType DetermineErrorType(string? message)
    {
        if (message == null) return ValidationErrorType.BusinessRule;
        
        if (message.Contains("required", StringComparison.OrdinalIgnoreCase))
            return ValidationErrorType.Required;
        if (message.Contains("length", StringComparison.OrdinalIgnoreCase))
            return ValidationErrorType.Length;
        if (message.Contains("range", StringComparison.OrdinalIgnoreCase))
            return ValidationErrorType.Range;
        if (message.Contains("format", StringComparison.OrdinalIgnoreCase))
            return ValidationErrorType.Format;
        
        return ValidationErrorType.BusinessRule;
    }
}