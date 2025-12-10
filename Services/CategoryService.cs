using Microsoft.EntityFrameworkCore;
using ResQLink.Data;
using ResQLink.Data.Entities;

namespace ResQLink.Services;

public class CategoryService
{
    private readonly AppDbContext _db;
    private readonly AuthState? _authState;
    private readonly AuditService _auditService;
    private readonly ArchiveService _archiveService;

    public CategoryService(AppDbContext db, AuditService auditService, ArchiveService archiveService, AuthState? authState = null)
    {
        _db = db;
        _auditService = auditService;
        _archiveService = archiveService;
        _authState = authState;
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

    public async Task<(Category? category, string? error)> CreateAsync(Category category)
    {
        try
        {
            // Check for duplicate category name (case-insensitive)
            var duplicate = await _db.Categories
                .AnyAsync(c => c.CategoryName.ToLower() == category.CategoryName.ToLower());
            
            if (duplicate)
            {
                await _auditService.LogAsync(
                    action: "CREATE",
                    entityType: "Category",
                    entityId: null,
                    userId: _authState?.UserId,
                    userType: _authState?.CurrentRole,
                    userName: _authState?.CurrentUser?.Username,
                    description: $"Failed to create category '{category.CategoryName}': Duplicate name",
                    severity: "Warning",
                    isSuccessful: false,
                    errorMessage: "A category with this name already exists"
                );
                return (null, "A category with this name already exists");
            }

            category.CreatedAt = DateTime.UtcNow;
            _db.Categories.Add(category);
            await _db.SaveChangesAsync();
            
            // Log category creation
            await _auditService.LogAsync(
                action: "CREATE",
                entityType: "Category",
                entityId: category.CategoryId,
                userId: _authState?.UserId,
                userType: _authState?.CurrentRole,
                userName: _authState?.CurrentUser?.Username,
                newValues: new { category.CategoryName, category.Description, category.CategoryTypeId, category.IsActive },
                description: $"Created category '{category.CategoryName}'",
                severity: "Info",
                isSuccessful: true
            );
            
            return (category, null);
        }
        catch (DbUpdateException ex)
        {
            var innerMessage = ex.InnerException?.Message ?? ex.Message;
            await _auditService.LogAsync(
                action: "CREATE",
                entityType: "Category",
                entityId: null,
                userId: _authState?.UserId,
                userType: _authState?.CurrentRole,
                userName: _authState?.CurrentUser?.Username,
                description: $"Database error while creating category '{category.CategoryName}'",
                severity: "Error",
                isSuccessful: false,
                errorMessage: innerMessage
            );
            return (null, $"Database error: {innerMessage}");
        }
        catch (Exception ex)
        {
            await _auditService.LogAsync(
                action: "CREATE",
                entityType: "Category",
                entityId: null,
                userId: _authState?.UserId,
                userType: _authState?.CurrentRole,
                userName: _authState?.CurrentUser?.Username,
                description: $"Error while creating category '{category.CategoryName}'",
                severity: "Error",
                isSuccessful: false,
                errorMessage: ex.Message
            );
            return (null, ex.Message);
        }
    }

    // ✅ FIX: Add CategoryTypeId update support
    public async Task<(Category? category, string? error)> UpdateAsync(int id, Category category)
    {
        try
        {
            var existing = await _db.Categories.FindAsync(id);
            if (existing == null)
            {
                await _auditService.LogAsync(
                    action: "UPDATE",
                    entityType: "Category",
                    entityId: id,
                    userId: _authState?.UserId,
                    userType: _authState?.CurrentRole,
                    userName: _authState?.CurrentUser?.Username,
                    description: $"Failed to update category #{id}: Not found",
                    severity: "Warning",
                    isSuccessful: false,
                    errorMessage: "Category not found"
                );
                return (null, "Category not found");
            }

            // Check for duplicate category name (excluding current category)
            var duplicate = await _db.Categories
                .AnyAsync(c => c.CategoryId != id && c.CategoryName.ToLower() == category.CategoryName.ToLower());
            
            if (duplicate)
            {
                await _auditService.LogAsync(
                    action: "UPDATE",
                    entityType: "Category",
                    entityId: id,
                    userId: _authState?.UserId,
                    userType: _authState?.CurrentRole,
                    userName: _authState?.CurrentUser?.Username,
                    description: $"Failed to update category #{id}: Duplicate name '{category.CategoryName}'",
                    severity: "Warning",
                    isSuccessful: false,
                    errorMessage: "A category with this name already exists"
                );
                return (null, "A category with this name already exists");
            }

            var oldValues = new
            {
                existing.CategoryName,
                existing.Description,
                existing.IsActive,
                existing.CategoryTypeId
            };

            existing.CategoryName = category.CategoryName;
            existing.Description = category.Description;
            existing.IsActive = category.IsActive;
            existing.CategoryTypeId = category.CategoryTypeId;

            await _db.SaveChangesAsync();
            
            // Log category update
            await _auditService.LogAsync(
                action: "UPDATE",
                entityType: "Category",
                entityId: id,
                userId: _authState?.UserId,
                userType: _authState?.CurrentRole,
                userName: _authState?.CurrentUser?.Username,
                oldValues: oldValues,
                newValues: new { existing.CategoryName, existing.Description, existing.IsActive, existing.CategoryTypeId },
                description: $"Updated category '{existing.CategoryName}'",
                severity: "Info",
                isSuccessful: true
            );
            
            return (existing, null);
        }
        catch (DbUpdateException ex)
        {
            var innerMessage = ex.InnerException?.Message ?? ex.Message;
            await _auditService.LogAsync(
                action: "UPDATE",
                entityType: "Category",
                entityId: id,
                userId: _authState?.UserId,
                userType: _authState?.CurrentRole,
                userName: _authState?.CurrentUser?.Username,
                description: $"Database error while updating category #{id}",
                severity: "Error",
                isSuccessful: false,
                errorMessage: innerMessage
            );
            return (null, $"Database error: {innerMessage}");
        }
        catch (Exception ex)
        {
            await _auditService.LogAsync(
                action: "UPDATE",
                entityType: "Category",
                entityId: id,
                userId: _authState?.UserId,
                userType: _authState?.CurrentRole,
                userName: _authState?.CurrentUser?.Username,
                description: $"Error while updating category #{id}",
                severity: "Error",
                isSuccessful: false,
                errorMessage: ex.Message
            );
            return (null, ex.Message);
        }
    }

    // ✅ Archive instead of delete using centralized ArchiveService
    public async Task<(bool success, string? error)> DeleteAsync(int id)
    {
        var category = await _db.Categories
            .Include(c => c.ReliefGoods)
            .FirstOrDefaultAsync(c => c.CategoryId == id);
            
        if (category == null) 
            return (false, "Category not found");

        try
        {
            var itemCount = category.ReliefGoods.Count;
            var reason = itemCount > 0 
                ? $"Archived with {itemCount} linked relief goods" 
                : "Archived by user";

            // Use centralized ArchiveService
            var result = await _archiveService.ArchiveAsync<Category>(
                id, 
                reason, 
                category.CategoryName);
            
            return result;
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