using Microsoft.EntityFrameworkCore;
using ResQLink.Data;
using ResQLink.Data.Entities;
using System.Text.Json;

namespace ResQLink.Services;

public class AuditService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public AuditService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    #region Core Logging Methods

    public async Task LogAsync(
        string action,
        string entityType,
        int? entityId = null,
        int? userId = null,
        string? userType = null,
        string? userName = null,
        object? oldValues = null,
        object? newValues = null,
        string? description = null,
        string severity = "Info",
        bool isSuccessful = true,
        string? errorMessage = null)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            string? deviceInfo = GetDeviceInfo();
            string? sessionId = GetSessionId();

            var auditLog = new AuditLog
            {
                Timestamp = DateTime.UtcNow,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                UserId = userId,
                UserType = userType,
                UserName = userName,
                IpAddress = "MAUI-App",
                UserAgent = deviceInfo,
                OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
                NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
                Description = description,
                Severity = severity,
                IsSuccessful = isSuccessful,
                ErrorMessage = errorMessage,
                SessionId = sessionId
            };

            context.AuditLogs.Add(auditLog);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AUDIT LOG ERROR] {DateTime.UtcNow}: Failed to log audit entry. Action: {action}, Entity: {entityType}, Error: {ex.Message}");
        }
    }

    #endregion

    #region Transaction Logging Methods

    /// <summary>
    /// Log inventory stock-in transaction
    /// </summary>
    public async Task LogStockInAsync(
        int stockId,
        string itemName,
        int quantity,
        string unit,
        decimal unitCost,
        decimal totalCost,
        string? supplier,
        int? userId,
        string? userName,
        int? barangayBudgetId = null)
    {
        var transactionDetails = new
        {
            StockId = stockId,
            ItemName = itemName,
            Quantity = quantity,
            Unit = unit,
            UnitCost = unitCost,
            TotalCost = totalCost,
            Supplier = supplier,
            BarangayBudgetId = barangayBudgetId,
            TransactionType = "Stock-In"
        };

        await LogAsync(
            action: "STOCK_IN",
            entityType: "Stock",
            entityId: stockId,
            userId: userId,
            userType: "Inventory Manager",
            userName: userName,
            newValues: transactionDetails,
            description: $"Stock received: {quantity} {unit} of {itemName} at ₱{unitCost:N2}/unit (Total: ₱{totalCost:N2})" +
                        (supplier != null ? $" from {supplier}" : ""),
            severity: "Info",
            isSuccessful: true
        );
    }

    /// <summary>
    /// Log inventory stock-out transaction
    /// </summary>
    public async Task LogStockOutAsync(
        int stockId,
        string itemName,
        int quantity,
        string unit,
        int remainingBalance,
        string destination,
        string purpose,
        int? userId,
        string? userName)
    {
        var transactionDetails = new
        {
            StockId = stockId,
            ItemName = itemName,
            QuantityReleased = quantity,
            Unit = unit,
            RemainingBalance = remainingBalance,
            Destination = destination,
            Purpose = purpose,
            TransactionType = "Stock-Out"
        };

        await LogAsync(
            action: "STOCK_OUT",
            entityType: "Stock",
            entityId: stockId,
            userId: userId,
            userType: "Inventory Manager",
            userName: userName,
            newValues: transactionDetails,
            description: $"Stock released: {quantity} {unit} of {itemName} to {destination} - {purpose}. Remaining: {remainingBalance} {unit}",
            severity: quantity > remainingBalance ? "Warning" : "Info",
            isSuccessful: true
        );
    }

    /// <summary>
    /// Log procurement request transaction
    /// </summary>
    public async Task LogProcurementRequestAsync(
        int requestId,
        string status,
        decimal totalAmount,
        int itemCount,
        int? barangayBudgetId,
        string? barangayName,
        int? userId,
        string? userName,
        string? notes = null)
    {
        var transactionDetails = new
        {
            RequestId = requestId,
            Status = status,
            TotalAmount = totalAmount,
            ItemCount = itemCount,
            BarangayBudgetId = barangayBudgetId,
            BarangayName = barangayName,
            Notes = notes,
            TransactionType = "Procurement Request"
        };

        await LogAsync(
            action: "PROCUREMENT_REQUEST",
            entityType: "ProcurementRequest",
            entityId: requestId,
            userId: userId,
            userType: "Finance Manager",
            userName: userName,
            newValues: transactionDetails,
            description: $"Procurement request #{requestId} for {barangayName}: ₱{totalAmount:N2} ({itemCount} items) - Status: {status}",
            severity: status == "Approved" ? "Info" : "Warning",
            isSuccessful: true
        );
    }

    /// <summary>
    /// Log budget allocation transaction
    /// </summary>
    public async Task LogBudgetAllocationAsync(
        int budgetId,
        string barangayName,
        int year,
        decimal totalAmount,
        string status,
        int? userId,
        string? userName,
        decimal? previousAmount = null)
    {
        var oldValues = previousAmount.HasValue ? new { TotalAmount = previousAmount.Value } : null;
        var newValues = new
        {
            BudgetId = budgetId,
            BarangayName = barangayName,
            Year = year,
            TotalAmount = totalAmount,
            Status = status,
            TransactionType = "Budget Allocation"
        };

        await LogAsync(
            action: previousAmount.HasValue ? "BUDGET_UPDATE" : "BUDGET_CREATE",
            entityType: "BarangayBudget",
            entityId: budgetId,
            userId: userId,
            userType: "Finance Manager",
            userName: userName,
            oldValues: oldValues,
            newValues: newValues,
            description: previousAmount.HasValue
                ? $"Budget updated for {barangayName} ({year}): ₱{previousAmount:N2} → ₱{totalAmount:N2}"
                : $"Budget created for {barangayName} ({year}): ₱{totalAmount:N2}",
            severity: "Info",
            isSuccessful: true
        );
    }

    /// <summary>
    /// Log budget expenditure transaction
    /// </summary>
    public async Task LogBudgetExpenditureAsync(
        int budgetItemId,
        int budgetId,
        string barangayName,
        string category,
        string description,
        decimal amount,
        decimal remainingBudget,
        int? userId,
        string? userName)
    {
        var transactionDetails = new
        {
            BudgetItemId = budgetItemId,
            BudgetId = budgetId,
            BarangayName = barangayName,
            Category = category,
            Description = description,
            Amount = amount,
            RemainingBudget = remainingBudget,
            TransactionType = "Budget Expenditure"
        };

        await LogAsync(
            action: "BUDGET_EXPENDITURE",
            entityType: "BarangayBudgetItem",
            entityId: budgetItemId,
            userId: userId,
            userType: "Finance Manager",
            userName: userName,
            newValues: transactionDetails,
            description: $"Expenditure recorded for {barangayName}: ₱{amount:N2} - {category} - {description}. Remaining budget: ₱{remainingBudget:N2}",
            severity: remainingBudget < 1000 ? "Warning" : "Info",
            isSuccessful: true
        );
    }

    /// <summary>
    /// Log donation transaction
    /// </summary>
    public async Task LogDonationAsync(
        int donationId,
        string donorName,
        decimal? monetaryAmount,
        string? itemDescription,
        string type,
        int? userId,
        string? userName)
    {
        var transactionDetails = new
        {
            DonationId = donationId,
            DonorName = donorName,
            MonetaryAmount = monetaryAmount,
            ItemDescription = itemDescription,
            DonationType = type,
            TransactionType = "Donation Received"
        };

        var description = type == "Monetary"
            ? $"Donation received from {donorName}: ₱{monetaryAmount:N2}"
            : $"In-kind donation received from {donorName}: {itemDescription}";

        await LogAsync(
            action: "DONATION_RECEIVED",
            entityType: "Donation",
            entityId: donationId,
            userId: userId,
            userType: "Finance Manager",
            userName: userName,
            newValues: transactionDetails,
            description: description,
            severity: "Info",
            isSuccessful: true
        );
    }

    #endregion

    #region Query Methods

    public async Task<List<AuditLog>> GetLogsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? action = null,
        string? entityType = null,
        int? userId = null,
        string? severity = null,
        int limit = 100)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var query = context.AuditLogs.AsNoTracking().AsQueryable();

        if (startDate.HasValue)
            query = query.Where(a => a.Timestamp >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(a => a.Timestamp <= endDate.Value);

        if (!string.IsNullOrEmpty(action))
            query = query.Where(a => a.Action == action);

        if (!string.IsNullOrEmpty(entityType))
            query = query.Where(a => a.EntityType == entityType);

        if (userId.HasValue)
            query = query.Where(a => a.UserId == userId.Value);

        if (!string.IsNullOrEmpty(severity))
            query = query.Where(a => a.Severity == severity);

        return await query
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<AuditLog>> GetTransactionLogsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? transactionType = null,
        int limit = 100)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var transactionActions = new[]
        {
            "STOCK_IN", "STOCK_OUT", "PROCUREMENT_REQUEST",
            "BUDGET_CREATE", "BUDGET_UPDATE", "BUDGET_EXPENDITURE",
            "DONATION_RECEIVED"
        };

        var query = context.AuditLogs
            .AsNoTracking()
            .Where(a => transactionActions.Contains(a.Action));

        if (startDate.HasValue)
            query = query.Where(a => a.Timestamp >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(a => a.Timestamp <= endDate.Value);

        if (!string.IsNullOrEmpty(transactionType))
            query = query.Where(a => a.Action == transactionType);

        return await query
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<AuditLog>> GetEntityHistoryAsync(string entityType, int entityId, int limit = 50)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.AuditLogs
            .AsNoTracking()
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<AuditLog>> GetUserActivityAsync(int userId, int limit = 100)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.AuditLogs
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .ToListAsync();
    }

    /// <summary>
    /// Get financial transaction summary
    /// </summary>
    public async Task<FinancialTransactionSummary> GetFinancialSummaryAsync(DateTime startDate, DateTime endDate)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var transactionLogs = await context.AuditLogs
            .AsNoTracking()
            .Where(a => a.Timestamp >= startDate && a.Timestamp <= endDate &&
                       (a.Action == "STOCK_IN" || a.Action == "PROCUREMENT_REQUEST" ||
                        a.Action == "BUDGET_EXPENDITURE" || a.Action == "DONATION_RECEIVED"))
            .ToListAsync();

        var summary = new FinancialTransactionSummary
        {
            TotalStockInTransactions = transactionLogs.Count(l => l.Action == "STOCK_IN"),
            TotalProcurementRequests = transactionLogs.Count(l => l.Action == "PROCUREMENT_REQUEST"),
            TotalExpenditures = transactionLogs.Count(l => l.Action == "BUDGET_EXPENDITURE"),
            TotalDonations = transactionLogs.Count(l => l.Action == "DONATION_RECEIVED"),
            PeriodStart = startDate,
            PeriodEnd = endDate
        };

        return summary;
    }

    #endregion

    #region Helper Methods

    private string GetDeviceInfo()
    {
        try
        {
            var deviceInfo = $"{DeviceInfo.Current.Platform} {DeviceInfo.Current.VersionString} | " +
                           $"{DeviceInfo.Current.Manufacturer} {DeviceInfo.Current.Model} | " +
                           $"App v{AppInfo.Current.VersionString}";
            return deviceInfo;
        }
        catch
        {
            return "Unknown Device";
        }
    }

    private string GetSessionId()
    {
        try
        {
            var sessionKey = "AuditSessionId";
            if (!Preferences.ContainsKey(sessionKey))
            {
                var newSessionId = Guid.NewGuid().ToString();
                Preferences.Set(sessionKey, newSessionId);
                return newSessionId;
            }
            return Preferences.Get(sessionKey, Guid.NewGuid().ToString());
        }
        catch
        {
            return Guid.NewGuid().ToString();
        }
    }

    public void ClearSession()
    {
        try
        {
            Preferences.Remove("AuditSessionId");
        }
        catch
        {
            // Ignore errors
        }
    }

    #endregion
}

public class FinancialTransactionSummary
{
    public int TotalStockInTransactions { get; set; }
    public int TotalProcurementRequests { get; set; }
    public int TotalExpenditures { get; set; }
    public int TotalDonations { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
}