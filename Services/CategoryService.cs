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

    public async Task<Category?> UpdateAsync(int id, Category category)
    {
        var existing = await _db.Categories.FindAsync(id);
        if (existing == null) return null;

        existing.CategoryName = category.CategoryName;
        existing.Description = category.Description;
        existing.IsActive = category.IsActive;

        await _db.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category == null) return false;

        // Check if category is in use
        var inUse = await _db.ReliefGoodCategories.AnyAsync(rc => rc.CategoryId == id);
        if (inUse)
        {
            // Soft delete by setting IsActive = false
            category.IsActive = false;
            await _db.SaveChangesAsync();
            return true;
        }

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<int> GetReliefGoodsCountAsync(int categoryId)
    {
        return await _db.ReliefGoodCategories.CountAsync(rc => rc.CategoryId == categoryId);
    }
}