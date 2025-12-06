using Microsoft.EntityFrameworkCore;
using ResQLink.Data;
using ResQLink.Data.Entities;

namespace ResQLink.Services;

public class InventoryService(AppDbContext db)
{
    public async Task<List<ReliefGood>> GetAllAsync()
    {
        return await db.ReliefGoods
            .Include(r => r.Categories)
                .ThenInclude(rc => rc.Category)
                    .ThenInclude(c => c.CategoryType)
            .Include(r => r.Stocks)
            .OrderBy(r => r.Name)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<ReliefGood?> GetByIdAsync(int id)
    {
        return await db.ReliefGoods
            .Include(r => r.Categories)
                .ThenInclude(rc => rc.Category)
                    .ThenInclude(c => c.CategoryType)
            .Include(r => r.Stocks)
            .FirstOrDefaultAsync(r => r.RgId == id);
    }

    public async Task<(ReliefGood? entity, string? error)> CreateAsync(
        ReliefGood input,
        IEnumerable<int> categoryIds,
        int quantity = 0,
        decimal unitCost = 0m,
        int? barangayBudgetId = null)
    {
        using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            input.CreatedAt = DateTime.UtcNow;
            db.ReliefGoods.Add(input);
            await db.SaveChangesAsync();

            // add pivot rows
            foreach (var cid in categoryIds.Distinct())
            {
                if (await db.Categories.AnyAsync(c => c.CategoryId == cid))
                    db.ReliefGoodCategories.Add(new ReliefGoodCategory { RgId = input.RgId, CategoryId = cid });
            }
            await db.SaveChangesAsync();

            // Create initial stock with unit cost if quantity > 0
            if (quantity > 0)
            {
                var totalCost = quantity * unitCost;

                // Deduct from barangay budget if specified
                if (barangayBudgetId.HasValue && unitCost > 0)
                {
                    var budget = await db.BarangayBudgets.FindAsync(barangayBudgetId.Value);
                    if (budget == null)
                    {
                        await tx.RollbackAsync();
                        return (null, "Barangay budget not found.");
                    }

                    // FIX: Accept both "Approved" and "Draft" status (matching Finance page)
                    if (budget.Status != "Approved" && budget.Status != "Draft")
                    {
                        await tx.RollbackAsync();
                        return (null, $"Barangay budget is not active. Current status: {budget.Status}");
                    }

                    // Check if budget has sufficient funds
                    var currentSpent = await db.BarangayBudgetItems
                        .Where(i => i.BudgetId == barangayBudgetId.Value)
                        .SumAsync(i => i.Amount);

                    var available = budget.TotalAmount - currentSpent;
                    if (totalCost > available)
                    {
                        await tx.RollbackAsync();
                        return (null, $"Insufficient budget. Available: ₱{available:N2}, Required: ₱{totalCost:N2}");
                    }

                    // Create budget item entry with better categorization
                    var budgetItem = new BarangayBudgetItem
                    {
                        BudgetId = barangayBudgetId.Value,
                        Category = "Inventory Purchase", // This is correct
                        Description = $"Purchase of {quantity} {input.Unit} of {input.Name}",
                        Amount = totalCost,
                        Notes = $"Unit Cost: ₱{unitCost:N2} | Item Type: {(input.RequiresExpiration ? "Perishable/Medical" : "Non-Perishable")}",
                        CreatedAt = DateTime.UtcNow
                    };
                    db.BarangayBudgetItems.Add(budgetItem);
                    
                    // Add debug logging
                    System.Diagnostics.Debug.WriteLine($"Budget transaction recorded:");
                    System.Diagnostics.Debug.WriteLine($"  Budget ID: {barangayBudgetId.Value}");
                    System.Diagnostics.Debug.WriteLine($"  Item: {input.Name}");
                    System.Diagnostics.Debug.WriteLine($"  Amount: ₱{totalCost:N2}");
                    System.Diagnostics.Debug.WriteLine($"  Previous Spent: ₱{currentSpent:N2}");
                    System.Diagnostics.Debug.WriteLine($"  New Total Spent: ₱{(currentSpent + totalCost):N2}");
                }

                var stock = new Stock
                {
                    RgId = input.RgId,
                    Quantity = quantity,
                    MaxCapacity = 1000,
                    UnitCost = unitCost,
                    IsActive = true,
                    LastUpdated = DateTime.UtcNow
                };
                db.Stocks.Add(stock);
                await db.SaveChangesAsync();
            }

            await tx.CommitAsync();
            return (await GetByIdAsync(input.RgId), null);
        }
        catch (DbUpdateException ex)
        {
            await tx.RollbackAsync();
            return (null, ex.InnerException?.Message ?? ex.Message);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return (null, ex.Message);
        }
    }

    public async Task<(ReliefGood? entity, string? error)> UpdateAsync(int id, ReliefGood input, IEnumerable<int> categoryIds)
    {
        using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            var existing = await db.ReliefGoods.Include(r => r.Categories).FirstOrDefaultAsync(r => r.RgId == id);
            if (existing == null) return (null, "Item not found");

            existing.Name = input.Name;
            existing.Unit = input.Unit;
            existing.Description = input.Description;
            existing.IsActive = input.IsActive;

            // sync categories
            var currentIds = existing.Categories.Select(c => c.CategoryId).ToHashSet();
            var newIds = categoryIds.Distinct().ToHashSet();

            // remove
            foreach (var rc in existing.Categories.Where(c => !newIds.Contains(c.CategoryId)).ToList())
                db.ReliefGoodCategories.Remove(rc);
            // add
            foreach (var addId in newIds.Where(id2 => !currentIds.Contains(id2)))
                if (await db.Categories.AnyAsync(c => c.CategoryId == addId))
                    db.ReliefGoodCategories.Add(new ReliefGoodCategory { RgId = existing.RgId, CategoryId = addId });

            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return (await GetByIdAsync(existing.RgId), null);
        }
        catch (DbUpdateException ex)
        {
            await tx.RollbackAsync();
            return (null, ex.InnerException?.Message ?? ex.Message);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return (null, ex.Message);
        }
    }

    /// <summary>
    /// Add stock with cost and deduct from barangay budget
    /// </summary>
    public async Task<(Stock? stock, string? error)> AddStockWithCostAsync(
        int rgId,
        int quantity,
        decimal unitCost,
        int? barangayBudgetId = null,
        int maxCapacity = 1000,
        string? location = null)
    {
        using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            if (quantity <= 0)
                return (null, "Quantity must be greater than zero.");

            if (unitCost < 0)
                return (null, "Unit cost cannot be negative.");

            var reliefGood = await db.ReliefGoods.FindAsync(rgId);
            if (reliefGood == null)
                return (null, "Relief good not found.");

            var totalCost = quantity * unitCost;

            // Deduct from barangay budget if specified
            if (barangayBudgetId.HasValue && unitCost > 0)
            {
                var budget = await db.BarangayBudgets.FindAsync(barangayBudgetId.Value);
                if (budget == null)
                {
                    await tx.RollbackAsync();
                    return (null, "Barangay budget not found.");
                }

                // FIX: Accept both "Approved" and "Draft" status
                if (budget.Status != "Approved" && budget.Status != "Draft")
                {
                    await tx.RollbackAsync();
                    return (null, $"Barangay budget is not active. Current status: {budget.Status}");
                }

                // Check if budget has sufficient funds
                var currentSpent = await db.BarangayBudgetItems
                    .Where(i => i.BudgetId == barangayBudgetId.Value)
                    .SumAsync(i => i.Amount);

                var available = budget.TotalAmount - currentSpent;
                if (totalCost > available)
                {
                    await tx.RollbackAsync();
                    return (null, $"Insufficient budget. Available: ₱{available:N2}, Required: ₱{totalCost:N2}");
                }

                // Create budget item entry
                var budgetItem = new BarangayBudgetItem
                {
                    BudgetId = barangayBudgetId.Value,
                    Category = "Inventory Purchase",
                    Description = $"Stock addition: {quantity} {reliefGood.Unit} of {reliefGood.Name}",
                    Amount = totalCost,
                    Notes = $"Unit Cost: ₱{unitCost:N2}",
                    CreatedAt = DateTime.UtcNow
                };
                db.BarangayBudgetItems.Add(budgetItem);
            }

            var stock = new Stock
            {
                RgId = rgId,
                Quantity = quantity,
                MaxCapacity = maxCapacity,
                UnitCost = unitCost,
                Location = location,
                IsActive = true,
                LastUpdated = DateTime.UtcNow
            };

            db.Stocks.Add(stock);
            await db.SaveChangesAsync();
            await tx.CommitAsync();

            return (stock, null);
        }
        catch (DbUpdateException ex)
        {
            await tx.RollbackAsync();
            return (null, ex.InnerException?.Message ?? ex.Message);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return (null, ex.Message);
        }
    }

    public async Task<(bool ok, string? error)> DeleteAsync(int id, bool softIfStock = true)
    {
        using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            var item = await db.ReliefGoods.Include(r => r.Stocks).FirstOrDefaultAsync(r => r.RgId == id);
            if (item == null) return (false, "Item not found");

            if (softIfStock && item.Stocks.Any())
            {
                item.IsActive = false; // soft delete
                await db.SaveChangesAsync();
            }
            else
            {
                // remove pivots first
                var pivots = await db.ReliefGoodCategories.Where(rc => rc.RgId == id).ToListAsync();
                db.ReliefGoodCategories.RemoveRange(pivots);
                db.ReliefGoods.Remove(item);
                await db.SaveChangesAsync();
            }

            await tx.CommitAsync();
            return (true, null);
        }
        catch (DbUpdateException ex)
        {
            await tx.RollbackAsync();
            return (false, ex.InnerException?.Message ?? ex.Message);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return (false, ex.Message);
        }
    }
}
