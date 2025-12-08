using Microsoft.EntityFrameworkCore;
using ResQLink.Data;
using ResQLink.Data.Entities;

namespace ResQLink.Services;

public class CategoryService
{
    private readonly AppDbContext _db;

    public CategoryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Category>> GetAllAsync()
    {
        return await _db.Categories
            .Include(c => c.CategoryType)
            .Include(c => c.ReliefGoods)
            .Where(c => c.IsActive)
            .OrderBy(c => c.CategoryName)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Category?> GetByIdAsync(int id)
    {
        return await _db.Categories
            .Include(c => c.CategoryType)
            .Include(c => c.ReliefGoods)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CategoryId == id);
    }

    public async Task<Category> CreateAsync(Category category)
    {
        category.CreatedAt = DateTime.UtcNow;
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        return category;
    }

    // ✅ FIX: Add CategoryTypeId update support
    public async Task<Category?> UpdateAsync(int id, Category category)
    {
        var existing = await _db.Categories.FindAsync(id);
        if (existing == null) return null;

        existing.CategoryName = category.CategoryName;
        existing.Description = category.Description;
        existing.IsActive = category.IsActive;
        
        // ✅ NEW: Allow updating category type
        existing.CategoryTypeId = category.CategoryTypeId;

        await _db.SaveChangesAsync();
        return existing;
    }

    // ✅ FIX: Improved delete with confirmation check and proper error handling
    public async Task<(bool success, string? error)> DeleteAsync(int id)
    {
        var category = await _db.Categories
            .Include(c => c.ReliefGoods)
            .FirstOrDefaultAsync(c => c.CategoryId == id);
            
        if (category == null) 
            return (false, "Category not found");

        // Check if category is in use
        var itemCount = category.ReliefGoods.Count;
        if (itemCount > 0)
        {
            // ✅ FIX: Better error message with item count
            return (false, $"Cannot delete '{category.CategoryName}' because it is assigned to {itemCount} item(s). Please reassign these items first.");
        }

        try
        {
            // Hard delete if not in use
            _db.Categories.Remove(category);
            await _db.SaveChangesAsync();
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, $"Database error: {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task<int> GetReliefGoodsCountAsync(int categoryId)
    {
        return await _db.ReliefGoodCategories.CountAsync(rc => rc.CategoryId == categoryId);
    }
}