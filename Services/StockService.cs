using Microsoft.EntityFrameworkCore;
using ResQLink.Data;
using ResQLink.Data.Entities;

namespace ResQLink.Services;

public class StockService
{
    private readonly AppDbContext db;
    private readonly AuditService _auditService;
    private readonly ArchiveService _archiveService;
    private readonly AuthState? _authState;

    public StockService(AppDbContext db, AuditService auditService, ArchiveService archiveService, AuthState? authState = null)
    {
        this.db = db;
        _auditService = auditService;
        _archiveService = archiveService;
        _authState = authState;
    }
    public Task<List<Stock>> GetAllAsync(bool onlyActive = true) =>
        db.Stocks
          .Include(s => s.ReliefGood).ThenInclude(r => r.Categories).ThenInclude(rc => rc.Category)
          .Include(s => s.Disaster)
          .Include(s => s.Shelter)
          .Where(s => !onlyActive || s.IsActive)
          .OrderByDescending(s => s.LastUpdated)
          .ToListAsync();

    public async Task<Stock?> GetAsync(int stockId) =>
        await db.Stocks
          .Include(s => s.ReliefGood)
          .Include(s => s.Disaster)
          .Include(s => s.Shelter)
          .FirstOrDefaultAsync(s => s.StockId == stockId);

    public Task<List<Stock>> GetByReliefGoodAsync(int rgId, bool onlyActive = true) =>
        db.Stocks
          .Include(s => s.ReliefGood)
          .Include(s => s.Disaster)
          .Include(s => s.Shelter)
          .Where(s => s.RgId == rgId && (!onlyActive || s.IsActive))
          .OrderByDescending(s => s.LastUpdated)
          .ToListAsync();

    // New: Create with unit cost and optional budget deduction
    public async Task<(Stock? stock, string? error)> CreateAsync(
        int rgId,
        int quantity,
        int? maxCapacity = null,
        string? location = null,
        int? disasterId = null,
        int? shelterId = null,
        decimal unitCost = 0m,
        int? budgetId = null,
        int? userId = null)
    {
        try
        {
            var exists = await db.ReliefGoods.AnyAsync(r => r.RgId == rgId);
            if (!exists) return (null, "ReliefGood not found.");

            if (quantity < 0) return (null, "Quantity cannot be negative.");
            if (maxCapacity.HasValue && maxCapacity.Value <= 0) return (null, "Max capacity must be greater than zero.");
            if (unitCost < 0) return (null, "Unit cost cannot be negative.");

            // If a budget is specified, verify and deduct
            if (budgetId.HasValue)
            {
                var budget = await db.BarangayBudgets.Include(b => b.Items).FirstOrDefaultAsync(b => b.BudgetId == budgetId.Value);
                if (budget is null) return (null, "Barangay budget not found.");

                var totalCost = unitCost * quantity;
                if (totalCost <= 0m)
                    return (null, "Total cost must be greater than zero when deducting from budget.");

                if (budget.TotalAmount < totalCost)
                    return (null, $"Insufficient budget. Available: {budget.TotalAmount:0.00}, required: {totalCost:0.00}");

                // Deduct and add an expense line item
                budget.TotalAmount -= totalCost;
                budget.UpdatedAt = DateTime.UtcNow;

                var item = new BarangayBudgetItem
                {
                    BudgetId = budget.BudgetId,
                    Category = "Inventory",
                    Description = $"Purchase of RG#{rgId} x{quantity} @ {unitCost:0.00}",
                    Amount = totalCost,
                    Notes = location,
                    CreatedAt = DateTime.UtcNow
                };

                db.BarangayBudgetItems.Add(item);
            }

            var stock = new Stock
            {
                RgId = rgId,
                Quantity = quantity,
                MaxCapacity = maxCapacity ?? 1000,
                Location = location,
                DisasterId = disasterId,
                ShelterId = shelterId,
                UnitCost = unitCost,
                LastUpdated = DateTime.UtcNow
            };

            db.Stocks.Add(stock);

            await db.SaveChangesAsync();
            await db.Entry(stock).ReloadAsync(); // Load computed columns

            return (stock, null);
        }
        catch (DbUpdateException ex) { return (null, ex.InnerException?.Message ?? ex.Message); }
        catch (Exception ex) { return (null, ex.Message); }
    }

    public async Task<(Stock? stock, string? error)> UpdateAsync(int stockId, int quantity, int? maxCapacity = null, string? location = null, int? disasterId = null, int? shelterId = null)
    {
        try
        {
            var s = await db.Stocks.FindAsync(stockId);
            if (s == null) return (null, "Stock not found.");

            s.Quantity = Math.Max(0, quantity);
            if (maxCapacity.HasValue) s.MaxCapacity = maxCapacity.Value;
            s.Location = location;
            s.DisasterId = disasterId;
            s.ShelterId = shelterId;
            s.LastUpdated = DateTime.UtcNow;

            await db.SaveChangesAsync();
            await db.Entry(s).ReloadAsync(); // Refresh computed columns

            return (s, null);
        }
        catch (DbUpdateException ex) { return (null, ex.InnerException?.Message ?? ex.Message); }
        catch (Exception ex) { return (null, ex.Message); }
    }

    public async Task<(Stock? stock, string? error)> AdjustQuantityAsync(int stockId, int delta)
    {
        try
        {
            var s = await db.Stocks.FindAsync(stockId);
            if (s == null) return (null, "Stock not found.");

            var newQty = s.Quantity + delta;
            if (newQty < 0) return (null, "Insufficient stock quantity.");
            if (newQty > s.MaxCapacity) return (null, "Exceeds max capacity.");

            s.Quantity = newQty;
            s.LastUpdated = DateTime.UtcNow;

            await db.SaveChangesAsync();
            await db.Entry(s).ReloadAsync();

            return (s, null);
        }
        catch (DbUpdateException ex) { return (null, ex.InnerException?.Message ?? ex.Message); }
        catch (Exception ex) { return (null, ex.Message); }
    }

    public async Task<(bool ok, string? error)> SetActiveAsync(int stockId, bool active)
    {
        try
        {
            var s = await db.Stocks.FindAsync(stockId);
            if (s == null) return (false, "Stock not found.");

            s.IsActive = active;
            await db.SaveChangesAsync();

            return (true, null);
        }
        catch (DbUpdateException ex) { return (false, ex.InnerException?.Message ?? ex.Message); }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public async Task<(bool ok, string? error)> DeleteAsync(int stockId)
    {
        try
        {
            var s = await db.Stocks
                .Include(st => st.Allocations)
                .Include(st => st.ReliefGood)
                .FirstOrDefaultAsync(st => st.StockId == stockId);

            if (s == null)
            {
                await _auditService.LogAsync(
                    action: "ARCHIVE",
                    entityType: "Stock",
                    entityId: stockId,
                    userId: _authState?.UserId,
                    userType: _authState?.CurrentRole,
                    userName: _authState?.CurrentUser?.Username,
                    description: $"Failed to archive stock #{stockId}: Not found",
                    severity: "Warning",
                    isSuccessful: false,
                    errorMessage: "Stock not found"
                );
                return (false, "Stock not found.");
            }

            // Use centralized ArchiveService
            var allocationCount = s.Allocations.Count;
            var reason = allocationCount > 0
                ? $"Archived with {allocationCount} allocations"
                : "Archived by user";

            var result = await _archiveService.ArchiveAsync<Stock>(
                stockId,
                reason,
                s.ReliefGood?.Name ?? $"Stock #{stockId}");

            if (!result.success)
                return result;

            return (true, null);
        }
        catch (DbUpdateException ex)
        {
            await _auditService.LogAsync(
                action: "ARCHIVE",
                entityType: "Stock",
                entityId: stockId,
                userId: _authState?.UserId,
                userType: _authState?.CurrentRole,
                userName: _authState?.CurrentUser?.Username,
                description: $"Failed to archive stock #{stockId}: Database error",
                severity: "Error",
                isSuccessful: false,
                errorMessage: ex.InnerException?.Message ?? ex.Message
            );
            return (false, ex.InnerException?.Message ?? ex.Message);
        }
        catch (Exception ex)
        {
            await _auditService.LogAsync(
                action: "ARCHIVE",
                entityType: "Stock",
                entityId: stockId,
                userId: _authState?.UserId,
                userType: _authState?.CurrentRole,
                userName: _authState?.CurrentUser?.Username,
                description: $"Failed to archive stock #{stockId}: {ex.Message}",
                severity: "Error",
                isSuccessful: false,
                errorMessage: ex.Message
            );
            return (false, ex.Message);
        }
    }
}