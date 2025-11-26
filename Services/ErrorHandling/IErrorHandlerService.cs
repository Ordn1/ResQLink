using Microsoft.EntityFrameworkCore;

namespace ResQLink.Services.ErrorHandling;

public interface IErrorHandlerService
{
    ErrorResult HandleException(Exception ex);
    ErrorResult HandleDbUpdateException(DbUpdateException ex);
    string GetUserFriendlyMessage(Exception ex);
    Task LogErrorAsync(Exception ex, string? context = null);
}

public class ErrorResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? DetailedMessage { get; set; }
    public ErrorCategory Category { get; set; }
    public Exception? OriginalException { get; set; }

    public static ErrorResult Success() => new() { IsSuccess = true, Message = "Operation completed successfully" };
    public static ErrorResult Failure(string message, ErrorCategory category = ErrorCategory.General, Exception? ex = null)
        => new() { IsSuccess = false, Message = message, Category = category, OriginalException = ex };
}

public enum ErrorCategory
{
    General,
    Database,
    Validation,
    NotFound,
    Unauthorized,
    Network,
    Constraint
}