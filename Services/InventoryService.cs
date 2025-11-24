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

    public async Task<(ReliefGood? entity,string? error)> CreateAsync(ReliefGood input, IEnumerable<int> categoryIds)
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

    public async Task<(ReliefGood? entity,string? error)> UpdateAsync(int id, ReliefGood input, IEnumerable<int> categoryIds)
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

    public async Task<(bool ok,string? error)> DeleteAsync(int id, bool softIfStock = true)
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
