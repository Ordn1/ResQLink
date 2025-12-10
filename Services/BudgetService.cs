using Microsoft.EntityFrameworkCore;
using ResQLink.Data;
using ResQLink.Data.Entities;

namespace ResQLink.Services;

public class BudgetService
{
    private readonly AppDbContext _db;
    private readonly AuditService? _auditService;
    private readonly ArchiveService? _archiveService;
    private readonly AuthState? _authState;

    public BudgetService(AppDbContext db, AuditService? auditService = null, ArchiveService? archiveService = null, AuthState? authState = null)
    {
        _db = db;
        _auditService = auditService;
        _archiveService = archiveService;
        _authState = authState;
    }

    public Task<List<BarangayBudget>> GetAllAsync() =>
        _db.BarangayBudgets
          .Include(b => b.Items)
          .Include(b => b.CreatedBy)
          .OrderByDescending(b => b.Year)
          .ThenBy(b => b.BarangayName)
          .AsNoTracking()
          .ToListAsync();

    public Task<BarangayBudget?> GetAsync(int id) =>
        _db.BarangayBudgets
          .Include(b => b.Items)
          .Include(b => b.CreatedBy)
          .FirstOrDefaultAsync(b => b.BudgetId == id);

    public async Task<(BarangayBudget? budget, string? error)> CreateAsync(BarangayBudget input)
    {
        try
        {
            // Server-side validation
            if (input.TotalAmount < 0)
                return (null, "Total Amount cannot be negative.");
            if (input.Year < 2000)
                return (null, "Year must be 2000 or later.");
            if (string.IsNullOrWhiteSpace(input.BarangayName))
                return (null, "Barangay is required.");

            input.BarangayName = input.BarangayName.Trim();
            input.Status = string.IsNullOrWhiteSpace(input.Status) ? "Draft" : input.Status.Trim();

            // Check for duplicate Barangay + Year combination
            var duplicate = await _db.BarangayBudgets
                .AnyAsync(b => b.BarangayName.ToLower() == input.BarangayName.ToLower() && b.Year == input.Year);
            
            if (duplicate)
            {
                if (_auditService != null)
                {
                    await _auditService.LogAsync(
                        action: "BUDGET_CREATE",
                        entityType: "BarangayBudget",
                        entityId: null,
                        userId: _authState?.UserId,
                        userType: _authState?.CurrentRole,
                        userName: _authState?.CurrentUser?.Username,
                        description: $"Failed to create budget for {input.BarangayName} ({input.Year}): Duplicate",
                        severity: "Warning",
                        isSuccessful: false,
                        errorMessage: $"A budget for {input.BarangayName} in year {input.Year} already exists"
                    );
                }
                return (null, $"A budget for {input.BarangayName} in year {input.Year} already exists");
            }

            input.CreatedAt = DateTime.UtcNow;
            input.UpdatedAt = DateTime.UtcNow;
            _db.BarangayBudgets.Add(input);
            await _db.SaveChangesAsync();

            // 🔥 Log budget creation
            if (_auditService != null)
            {
                await _auditService.LogBudgetAllocationAsync(
                    budgetId: input.BudgetId,
                    barangayName: input.BarangayName,
                    year: input.Year,
                    totalAmount: input.TotalAmount,
                    status: input.Status,
                    userId: _authState?.UserId,
                    userName: _authState?.CurrentUser?.Username
                );
            }

            return (input, null);
        }
        catch (DbUpdateException ex)
        {
            // 🔥 Log budget creation error
            if (_auditService != null)
            {
                await _auditService.LogAsync(
                    action: "BUDGET_CREATE",
                    entityType: "BarangayBudget",
                    entityId: null,
                    userId: _authState?.UserId,
                    userType: _authState?.CurrentRole,
                    userName: _authState?.CurrentUser?.Username,
                    description: $"Failed to create budget for {input.BarangayName} ({input.Year})",
                    severity: "Error",
                    isSuccessful: false,
                    errorMessage: ex.InnerException?.Message ?? ex.Message
                );
            }
            return (null, ex.InnerException?.Message ?? ex.Message);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    public async Task<(BarangayBudget? budget, string? error)> UpdateAsync(BarangayBudget input)
    {
        try
        {
            // Server-side validation
            if (input.TotalAmount < 0)
                return (null, "Total Amount cannot be negative.");
            if (input.Year < 2000)
                return (null, "Year must be 2000 or later.");
            if (string.IsNullOrWhiteSpace(input.BarangayName))
                return (null, "Barangay is required.");

            var existing = await _db.BarangayBudgets.FindAsync(input.BudgetId);
            if (existing is null) return (null, "Budget not found.");

            // Check for duplicate Barangay + Year combination (excluding current budget)
            var duplicate = await _db.BarangayBudgets
                .AnyAsync(b => b.BudgetId != input.BudgetId && 
                             b.BarangayName.ToLower() == input.BarangayName.Trim().ToLower() && 
                             b.Year == input.Year);
            
            if (duplicate)
            {
                if (_auditService != null)
                {
                    await _auditService.LogAsync(
                        action: "BUDGET_UPDATE",
                        entityType: "BarangayBudget",
                        entityId: input.BudgetId,
                        userId: _authState?.UserId,
                        userType: _authState?.CurrentRole,
                        userName: _authState?.CurrentUser?.Username,
                        description: $"Failed to update budget #{input.BudgetId}: Duplicate Barangay+Year",
                        severity: "Warning",
                        isSuccessful: false,
                        errorMessage: $"A budget for {input.BarangayName.Trim()} in year {input.Year} already exists"
                    );
                }
                return (null, $"A budget for {input.BarangayName.Trim()} in year {input.Year} already exists");
            }

            // 🔥 Capture old values for audit
            var oldValues = new
            {
                existing.BarangayName,
                existing.Year,
                existing.TotalAmount,
                existing.Status
            };

            var previousAmount = existing.TotalAmount;

            existing.BarangayName = input.BarangayName.Trim();
            existing.Year = input.Year;
            existing.TotalAmount = input.TotalAmount;
            existing.Status = input.Status;
            existing.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            // 🔥 Log budget update
            if (_auditService != null)
            {
                await _auditService.LogBudgetAllocationAsync(
                    budgetId: input.BudgetId,
                    barangayName: input.BarangayName,
                    year: input.Year,
                    totalAmount: input.TotalAmount,
                    status: input.Status,
                    userId: _authState?.UserId,
                    userName: _authState?.CurrentUser?.Username,
                    previousAmount: previousAmount
                );
            }

            return (existing, null);
        }
        catch (DbUpdateException ex)
        {
            // 🔥 Log budget update error
            if (_auditService != null)
            {
                await _auditService.LogAsync(
                    action: "BUDGET_UPDATE",
                    entityType: "BarangayBudget",
                    entityId: input.BudgetId,
                    userId: _authState?.UserId,
                    userType: _authState?.CurrentRole,
                    userName: _authState?.CurrentUser?.Username,
                    description: $"Failed to update budget #{input.BudgetId}",
                    severity: "Error",
                    isSuccessful: false,
                    errorMessage: ex.InnerException?.Message ?? ex.Message
                );
            }
            return (null, ex.InnerException?.Message ?? ex.Message);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    public async Task<(bool ok, string? error)> DeleteAsync(int id)
    {
        try
        {
            var budget = await _db.BarangayBudgets.Include(b => b.Items).FirstOrDefaultAsync(b => b.BudgetId == id);
            if (budget is null)
            {
                if (_auditService != null)
                {
                    await _auditService.LogAsync(
                        action: "ARCHIVE",
                        entityType: "BarangayBudget",
                        entityId: id,
                        userId: _authState?.UserId,
                    userType: _authState?.CurrentRole,
                    userName: _authState?.CurrentUser?.Username,
                    description: $"Failed to archive budget #{id}: Not found",
                    severity: "Warning",
                    isSuccessful: false,
                    errorMessage: "Budget not found"
                );
                }
                return (false, "Budget not found.");
            }

            // Use centralized ArchiveService
            if (_archiveService != null)
            {
                var itemCount = budget.Items.Count;
                var totalSpent = budget.Items.Sum(i => i.Amount);
                var reason = itemCount > 0
                    ? $"Archived with {itemCount} budget items (Total: ₱{totalSpent:N2})"
                    : "Archived by user";

                var result = await _archiveService.ArchiveAsync<BarangayBudget>(
                    id,
                    reason,
                    $"{budget.BarangayName} ({budget.Year})");

                if (!result.success)
                    return (false, result.error);
            }

            return (true, null);
        }
        catch (DbUpdateException ex)
        {
            if (_auditService != null)
            {
                await _auditService.LogAsync(
                    action: "ARCHIVE",
                    entityType: "BarangayBudget",
                    entityId: id,
                    userId: _authState?.UserId,
                    userType: _authState?.CurrentRole,
                    userName: _authState?.CurrentUser?.Username,
                description: $"Failed to archive budget #{id}: Database error",
                severity: "Error",
                isSuccessful: false,
                errorMessage: ex.InnerException?.Message ?? ex.Message
            );
            }
            return (false, ex.InnerException?.Message ?? ex.Message);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(BarangayBudgetItem? item, string? error)> AddItemAsync(int budgetId, BarangayBudgetItem item)
    {
        try
        {
            // Server-side validation
            if (item.Amount < 0)
                return (null, "Amount cannot be negative.");
            if (string.IsNullOrWhiteSpace(item.Category) || string.IsNullOrWhiteSpace(item.Description))
                return (null, "Category and Description are required.");

            var budget = await _db.BarangayBudgets.FindAsync(budgetId);
            if (budget is null) return (null, "Budget not found.");

            var currentSpent = await _db.BarangayBudgetItems
                .Where(i => i.BudgetId == budgetId)
                .SumAsync(i => i.Amount);
            
            var remainingBudget = budget.TotalAmount - currentSpent - item.Amount;

            item.BudgetId = budgetId;
            item.CreatedAt = DateTime.UtcNow;
            _db.BarangayBudgetItems.Add(item);
            await _db.SaveChangesAsync();

            // 🔥 Log budget expenditure
            if (_auditService != null)
            {
                await _auditService.LogBudgetExpenditureAsync(
                    budgetItemId: item.BudgetItemId,
                    budgetId: budgetId,
                    barangayName: budget.BarangayName,
                    category: item.Category,
                    description: item.Description,
                    amount: item.Amount,
                    remainingBudget: remainingBudget,
                    userId: _authState?.UserId,
                    userName: _authState?.CurrentUser?.Username
                );
            }

            return (item, null);
        }
        catch (DbUpdateException ex)
        {
            return (null, ex.InnerException?.Message ?? ex.Message);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    public async Task<(BarangayBudgetItem? item, string? error)> UpdateItemAsync(BarangayBudgetItem item)
    {
        try
        {
            // Server-side validation
            if (item.Amount < 0)
                return (null, "Amount cannot be negative.");
            if (string.IsNullOrWhiteSpace(item.Category) || string.IsNullOrWhiteSpace(item.Description))
                return (null, "Category and Description are required.");

            var existing = await _db.BarangayBudgetItems.FindAsync(item.BudgetItemId);
            if (existing is null) return (null, "Item not found.");

            var budget = await _db.BarangayBudgets.FindAsync(existing.BudgetId);
            if (budget is null) return (null, "Budget not found.");

            // 🔥 Capture old values
            var oldValues = new
            {
                existing.Category,
                existing.Description,
                existing.Amount,
                existing.Notes
            };

            existing.Category = item.Category.Trim();
            existing.Description = item.Description.Trim();
            existing.Amount = item.Amount;
            existing.Notes = item.Notes?.Trim();

            await _db.SaveChangesAsync();

            var currentSpent = await _db.BarangayBudgetItems
                .Where(i => i.BudgetId == existing.BudgetId)
                .SumAsync(i => i.Amount);
            
            var remainingBudget = budget.TotalAmount - currentSpent;

            // 🔥 Log budget item update
            if (_auditService != null)
            {
                await _auditService.LogAsync(
                    action: "BUDGET_ITEM_UPDATE",
                    entityType: "BarangayBudgetItem",
                    entityId: item.BudgetItemId,
                    userId: _authState?.UserId,
                    userType: _authState?.CurrentRole,
                    userName: _authState?.CurrentUser?.Username,
                    oldValues: oldValues,
                    newValues: new
                    {
                        item.Category,
                        item.Description,
                        item.Amount,
                        item.Notes
                    },
                    description: $"Budget item updated for {budget.BarangayName}: {item.Category} - {item.Description} (₱{item.Amount:N2})",
                    severity: "Info",
                    isSuccessful: true
                );
            }

            return (existing, null);
        }
        catch (DbUpdateException ex)
        {
            return (null, ex.InnerException?.Message ?? ex.Message);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    public async Task<(bool ok, string? error)> DeleteItemAsync(int budgetItemId)
    {
        try
        {
            var existing = await _db.BarangayBudgetItems.FindAsync(budgetItemId);
            if (existing is null) return (false, "Item not found.");

            var budget = await _db.BarangayBudgets.FindAsync(existing.BudgetId);
            if (budget is null) return (false, "Budget not found.");

            // 🔥 Capture item details
            var itemDetails = new
            {
                existing.BudgetItemId,
                existing.Category,
                existing.Description,
                existing.Amount,
                existing.Notes
            };

            _db.BarangayBudgetItems.Remove(existing);
            await _db.SaveChangesAsync();

            // 🔥 Log budget item deletion
            if (_auditService != null)
            {
                await _auditService.LogAsync(
                    action: "BUDGET_ITEM_DELETE",
                    entityType: "BarangayBudgetItem",
                    entityId: budgetItemId,
                    userId: _authState?.UserId,
                    userType: _authState?.CurrentRole,
                    userName: _authState?.CurrentUser?.Username,
                    oldValues: itemDetails,
                    description: $"Budget item deleted from {budget.BarangayName}: {existing.Category} - {existing.Description} (₱{existing.Amount:N2})",
                    severity: "Warning",
                    isSuccessful: true
                );
            }

            return (true, null);
        }
        catch (DbUpdateException ex)
        {
            return (false, ex.InnerException?.Message ?? ex.Message);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}