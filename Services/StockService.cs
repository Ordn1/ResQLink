using Microsoft.EntityFrameworkCore;
using ResQLink.Data;
using ResQLink.Data.Entities;

namespace ResQLink.Services;

public class StockService(AppDbContext db)
{
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

    public async Task<(Stock? stock, string? error)> CreateAsync(int rgId, int quantity, int? maxCapacity = null, string? location = null, int? disasterId = null, int? shelterId = null)
    {
        try
        {
            var exists = await db.ReliefGoods.AnyAsync(r => r.RgId == rgId);
            if (!exists) return (null, "ReliefGood not found.");
            
            var stock = new Stock
            {
                RgId = rgId,
                Quantity = quantity,
                MaxCapacity = maxCapacity ?? 1000,
                Location = location,
                DisasterId = disasterId,
                ShelterId = shelterId
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
            var s = await db.Stocks.Include(st => st.Allocations).FirstOrDefaultAsync(st => st.StockId == stockId);
            if (s == null) return (false, "Stock not found.");
            
            if (s.Allocations.Count != 0)
            {
                // Soft delete if has allocations
                s.IsActive = false;
                await db.SaveChangesAsync();
            }
            else
            {
                db.Stocks.Remove(s);
                await db.SaveChangesAsync();
            }
            
            return (true, null);
        }
        catch (DbUpdateException ex) { return (false, ex.InnerException?.Message ?? ex.Message); }
        catch (Exception ex) { return (false, ex.Message); }
    }
}