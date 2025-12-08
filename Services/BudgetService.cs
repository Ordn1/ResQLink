using Microsoft.EntityFrameworkCore;
using ResQLink.Data;
using ResQLink.Data.Entities;

namespace ResQLink.Services;

public class BudgetService
{
    private readonly AppDbContext _db;
    private readonly AuditService? _auditService;
    private readonly AuthState? _authState;

    public BudgetService(AppDbContext db, AuditService? auditService = null, AuthState? authState = null)
    {
        _db = db;
        _auditService = auditService;
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
            var existing = await _db.BarangayBudgets.FindAsync(input.BudgetId);
            if (existing is null) return (null, "Budget not found.");

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
            if (budget is null) return (false, "Budget not found.");

            // 🔥 Capture budget details before deletion
            var budgetDetails = new
            {
                budget.BudgetId,
                budget.BarangayName,
                budget.Year,
                budget.TotalAmount,
                budget.Status,
                ItemCount = budget.Items.Count,
                TotalSpent = budget.Items.Sum(i => i.Amount)
            };

            _db.BarangayBudgetItems.RemoveRange(budget.Items);
            _db.BarangayBudgets.Remove(budget);
            await _db.SaveChangesAsync();

            // 🔥 Log budget deletion
            if (_auditService != null)
            {
                await _auditService.LogAsync(
                    action: "BUDGET_DELETE",
                    entityType: "BarangayBudget",
                    entityId: id,
                    userId: _authState?.UserId,
                    userType: _authState?.CurrentRole,
                    userName: _authState?.CurrentUser?.Username,
                    oldValues: budgetDetails,
                    description: $"Budget deleted: {budget.BarangayName} ({budget.Year}) - ₱{budget.TotalAmount:N2}",
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

    public async Task<(BarangayBudgetItem? item, string? error)> AddItemAsync(int budgetId, BarangayBudgetItem item)
    {
        try
        {
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