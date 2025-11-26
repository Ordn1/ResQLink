using System.ComponentModel.DataAnnotations;

namespace ResQLink.Services.Validation;

public interface IValidationService
{
    ValidationResult ValidateModel<T>(T model) where T : class;
    Task<ValidationResult> ValidateModelAsync<T>(T model) where T : class;
    ValidationResult ValidateProperty<T>(T model, string propertyName, object? value) where T : class;
    bool IsValid<T>(T model) where T : class;
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<ValidationError> Errors { get; set; } = new();
    public string ErrorMessage => string.Join("; ", Errors.Select(e => e.Message));

    public static ValidationResult Success() => new() { IsValid = true };
    public static ValidationResult Failure(params ValidationError[] errors) => 
        new() { IsValid = false, Errors = errors.ToList() };
}

public record ValidationError(string PropertyName, string Message, ValidationErrorType Type = ValidationErrorType.BusinessRule);

public enum ValidationErrorType
{
    Required,
    Format,
    Range,
    Length,
    BusinessRule,
    Database,
    ForeignKey
}