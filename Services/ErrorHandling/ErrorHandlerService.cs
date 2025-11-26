using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ResQLink.Services.ErrorHandling;

public class ErrorHandlerService : IErrorHandlerService
{
    private readonly ILogger<ErrorHandlerService> _logger;

    public ErrorHandlerService(ILogger<ErrorHandlerService> logger)
    {
        _logger = logger;
    }

    public ErrorResult HandleException(Exception ex)
    {
        _logger.LogError(ex, "Exception occurred: {Message}", ex.Message);

        return ex switch
        {
            DbUpdateException dbEx => HandleDbUpdateException(dbEx),
            ArgumentNullException => ErrorResult.Failure("Required data is missing", ErrorCategory.Validation, ex),
            ArgumentException => ErrorResult.Failure(ex.Message, ErrorCategory.Validation, ex),
            InvalidOperationException => ErrorResult.Failure(ex.Message, ErrorCategory.General, ex),
            UnauthorizedAccessException => ErrorResult.Failure("You don't have permission to perform this action", ErrorCategory.Unauthorized, ex),
            _ => ErrorResult.Failure($"An unexpected error occurred: {ex.Message}", ErrorCategory.General, ex)
        };
    }

    public ErrorResult HandleDbUpdateException(DbUpdateException ex)
    {
        if (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx)
        {
            return sqlEx.Number switch
            {
                547 => ErrorResult.Failure(
                    "Cannot perform this operation because it would violate data relationships. Please check related records.",
                    ErrorCategory.Constraint, ex),
                
                2601 or 2627 => ErrorResult.Failure(
                    "A record with this information already exists. Please use unique values.",
                    ErrorCategory.Constraint, ex),
                
                515 => ErrorResult.Failure(
                    "Required information is missing. Please fill in all required fields.",
                    ErrorCategory.Validation, ex),
                
                _ => ErrorResult.Failure(
                    $"Database error occurred: {sqlEx.Message}",
                    ErrorCategory.Database, ex)
            };
        }

        return ErrorResult.Failure(
            $"Database update failed: {ex.InnerException?.Message ?? ex.Message}",
            ErrorCategory.Database, ex);
    }

    public string GetUserFriendlyMessage(Exception ex)
    {
        var result = HandleException(ex);
        return result.Message;
    }

    public async Task LogErrorAsync(Exception ex, string? context = null)
    {
        var contextInfo = !string.IsNullOrEmpty(context) ? $" [Context: {context}]" : "";
        _logger.LogError(ex, "Error logged{Context}: {Message}", contextInfo, ex.Message);
        
        // Here you could add database logging or external logging service
        await Task.CompletedTask;
    }
}