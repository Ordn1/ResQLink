using Microsoft.EntityFrameworkCore;
using ResQLink.Data;
using ResQLink.Data.Entities;

namespace ResQLink.Services;

public class BudgetService(AppDbContext db)
{
    public Task<List<BarangayBudget>> GetAllAsync() =>
        db.BarangayBudgets
          .Include(b => b.Items)
          .Include(b => b.CreatedBy)
          .OrderByDescending(b => b.Year)
          .ThenBy(b => b.BarangayName)
          .AsNoTracking()
          .ToListAsync();

    public Task<BarangayBudget?> GetAsync(int id) =>
        db.BarangayBudgets
          .Include(b => b.Items)
          .Include(b => b.CreatedBy)
          .FirstOrDefaultAsync(b => b.BudgetId == id);

    public async Task<(BarangayBudget? budget, string? error)> CreateAsync(BarangayBudget input)
    {
        try
        {
            input.CreatedAt = DateTime.UtcNow;
            input.UpdatedAt = DateTime.UtcNow;
            db.BarangayBudgets.Add(input);
            await db.SaveChangesAsync();
            return (input, null);
        }
        catch (DbUpdateException ex) { return (null, ex.InnerException?.Message ?? ex.Message); }
        catch (Exception ex) { return (null, ex.Message); }
    }

    public async Task<(BarangayBudget? budget, string? error)> UpdateAsync(BarangayBudget input)
    {
        try
        {
            var existing = await db.BarangayBudgets.FindAsync(input.BudgetId);
            if (existing is null) return (null, "Budget not found.");

            existing.BarangayName = input.BarangayName.Trim();
            existing.Year = input.Year;
            existing.TotalAmount = input.TotalAmount;
            existing.Status = input.Status;
            existing.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return (existing, null);
        }
        catch (DbUpdateException ex) { return (null, ex.InnerException?.Message ?? ex.Message); }
        catch (Exception ex) { return (null, ex.Message); }
    }

    public async Task<(bool ok, string? error)> DeleteAsync(int id)
    {
        try
        {
            var budget = await db.BarangayBudgets.Include(b => b.Items).FirstOrDefaultAsync(b => b.BudgetId == id);
            if (budget is null) return (false, "Budget not found.");

            db.BarangayBudgetItems.RemoveRange(budget.Items);
            db.BarangayBudgets.Remove(budget);
            await db.SaveChangesAsync();
            return (true, null);
        }
        catch (DbUpdateException ex) { return (false, ex.InnerException?.Message ?? ex.Message); }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public async Task<(BarangayBudgetItem? item, string? error)> AddItemAsync(int budgetId, BarangayBudgetItem item)
    {
        try
        {
            var exists = await db.BarangayBudgets.AnyAsync(b => b.BudgetId == budgetId);
            if (!exists) return (null, "Budget not found.");

            item.BudgetId = budgetId;
            item.CreatedAt = DateTime.UtcNow;
            db.BarangayBudgetItems.Add(item);
            await db.SaveChangesAsync();
            return (item, null);
        }
        catch (DbUpdateException ex) { return (null, ex.InnerException?.Message ?? ex.Message); }
        catch (Exception ex) { return (null, ex.Message); }
    }

    public async Task<(BarangayBudgetItem? item, string? error)> UpdateItemAsync(BarangayBudgetItem item)
    {
        try
        {
            var existing = await db.BarangayBudgetItems.FindAsync(item.BudgetItemId);
            if (existing is null) return (null, "Item not found.");

            existing.Category = item.Category.Trim();
            existing.Description = item.Description.Trim();
            existing.Amount = item.Amount;
            existing.Notes = item.Notes?.Trim();

            await db.SaveChangesAsync();
            return (existing, null);
        }
        catch (DbUpdateException ex) { return (null, ex.InnerException?.Message ?? ex.Message); }
        catch (Exception ex) { return (null, ex.Message); }
    }

    public async Task<(bool ok, string? error)> DeleteItemAsync(int budgetItemId)
    {
        try
        {
            var existing = await db.BarangayBudgetItems.FindAsync(budgetItemId);
            if (existing is null) return (false, "Item not found.");

            db.BarangayBudgetItems.Remove(existing);
            await db.SaveChangesAsync();
            return (true, null);
        }
        catch (DbUpdateException ex) { return (false, ex.InnerException?.Message ?? ex.Message); }
        catch (Exception ex) { return (false, ex.Message); }
    }
}