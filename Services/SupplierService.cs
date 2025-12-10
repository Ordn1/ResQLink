using Microsoft.EntityFrameworkCore;
using ResQLink.Data;
using ResQLink.Data.Entities;

namespace ResQLink.Services;

public class SupplierService
{
    private readonly AppDbContext _db;
    private readonly AuditService _auditService;
    private readonly ArchiveService _archiveService;
    private readonly AuthState? _authState;

    public SupplierService(AppDbContext db, AuditService auditService, ArchiveService archiveService, AuthState? authState = null)
    {
        _db = db;
        _auditService = auditService;
        _archiveService = archiveService;
        _authState = authState;
    }

    public Task<List<Supplier>> GetAllAsync() =>
        _db.Suppliers.OrderBy(s => s.SupplierName).AsNoTracking().ToListAsync();

    public Task<Supplier?> GetByIdAsync(int id) =>
        _db.Suppliers.AsNoTracking().FirstOrDefaultAsync(s => s.SupplierId == id);

    public async Task<(Supplier? supplier, string? error)> CreateAsync(Supplier supplier)
    {
        try
        {
            // Check for duplicate supplier name (case-insensitive)
            var duplicate = await _db.Suppliers
                .AnyAsync(s => s.SupplierName.ToLower() == supplier.SupplierName.ToLower());
            
            if (duplicate)
            {
                await _auditService.LogAsync(
                    action: "CREATE",
                    entityType: "Supplier",
                    entityId: null,
                    userId: _authState?.UserId,
                    userType: _authState?.CurrentRole,
                    userName: _authState?.CurrentUser?.Username,
                    description: $"Failed to create supplier '{supplier.SupplierName}': Duplicate name",
                    severity: "Warning",
                    isSuccessful: false,
                    errorMessage: "A supplier with this name already exists"
                );
                return (null, "A supplier with this name already exists");
            }

            supplier.CreatedAt = DateTime.UtcNow;
            _db.Suppliers.Add(supplier);
            await _db.SaveChangesAsync();

            // Log supplier creation
            await _auditService.LogAsync(
                action: "CREATE",
                entityType: "Supplier",
                entityId: supplier.SupplierId,
                userId: _authState?.UserId,
                userType: _authState?.CurrentRole,
                userName: _authState?.CurrentUser?.Username,
                newValues: new { supplier.SupplierName, supplier.ContactPerson, supplier.Email, supplier.PhoneNumber },
                description: $"Created supplier '{supplier.SupplierName}'",
                severity: "Info",
                isSuccessful: true
            );

            return (supplier, null);
        }
        catch (DbUpdateException ex)
        {
            await _auditService.LogAsync(
                action: "CREATE",
                entityType: "Supplier",
                entityId: null,
                userId: _authState?.UserId,
                userType: _authState?.CurrentRole,
                userName: _authState?.CurrentUser?.Username,
                description: $"Database error while creating supplier '{supplier.SupplierName}'",
                severity: "Error",
                isSuccessful: false,
                errorMessage: ex.InnerException?.Message ?? ex.Message
            );
            return (null, ex.InnerException?.Message ?? ex.Message);
        }
        catch (Exception ex)
        {
            await _auditService.LogAsync(
                action: "CREATE",
                entityType: "Supplier",
                entityId: null,
                userId: _authState?.UserId,
                userType: _authState?.CurrentRole,
                userName: _authState?.CurrentUser?.Username,
                description: $"Error while creating supplier '{supplier.SupplierName}'",
                severity: "Error",
                isSuccessful: false,
                errorMessage: ex.Message
            );
            return (null, ex.Message);
        }
    }

    public async Task<(Supplier? supplier, string? error)> UpdateAsync(int id, Supplier supplier)
    {
        try
        {
            var existing = await _db.Suppliers.FindAsync(id);
            if (existing is null)
            {
                await _auditService.LogAsync(
                    action: "UPDATE",
                    entityType: "Supplier",
                    entityId: id,
                    userId: _authState?.UserId,
                    userType: _authState?.CurrentRole,
                    userName: _authState?.CurrentUser?.Username,
                    description: $"Failed to update supplier #{id}: Not found",
                    severity: "Warning",
                    isSuccessful: false,
                    errorMessage: "Supplier not found"
                );
                return (null, "Supplier not found");
            }

            // Check for duplicate supplier name (excluding current supplier)
            var duplicate = await _db.Suppliers
                .AnyAsync(s => s.SupplierId != id && s.SupplierName.ToLower() == supplier.SupplierName.ToLower());
            
            if (duplicate)
            {
                await _auditService.LogAsync(
                    action: "UPDATE",
                    entityType: "Supplier",
                    entityId: id,
                    userId: _authState?.UserId,
                    userType: _authState?.CurrentRole,
                    userName: _authState?.CurrentUser?.Username,
                    description: $"Failed to update supplier #{id}: Duplicate name '{supplier.SupplierName}'",
                    severity: "Warning",
                    isSuccessful: false,
                    errorMessage: "A supplier with this name already exists"
                );
                return (null, "A supplier with this name already exists");
            }

            var oldValues = new
            {
                existing.SupplierName,
                existing.ContactPerson,
                existing.Email,
                existing.PhoneNumber,
                existing.Address,
                existing.IsActive
            };

            existing.SupplierName = supplier.SupplierName;
            existing.ContactPerson = supplier.ContactPerson;
            existing.Email = supplier.Email;
            existing.PhoneNumber = supplier.PhoneNumber;
            existing.Address = supplier.Address;
            existing.Notes = supplier.Notes;
            existing.IsActive = supplier.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            // Log supplier update
            await _auditService.LogAsync(
                action: "UPDATE",
                entityType: "Supplier",
                entityId: id,
                userId: _authState?.UserId,
                userType: _authState?.CurrentRole,
                userName: _authState?.CurrentUser?.Username,
                oldValues: oldValues,
                newValues: new { existing.SupplierName, existing.ContactPerson, existing.Email, existing.PhoneNumber, existing.IsActive },
                description: $"Updated supplier '{existing.SupplierName}'",
                severity: "Info",
                isSuccessful: true
            );

            return (existing, null);
        }
        catch (DbUpdateException ex)
        {
            await _auditService.LogAsync(
                action: "UPDATE",
                entityType: "Supplier",
                entityId: id,
                userId: _authState?.UserId,
                userType: _authState?.CurrentRole,
                userName: _authState?.CurrentUser?.Username,
                description: $"Database error while updating supplier #{id}",
                severity: "Error",
                isSuccessful: false,
                errorMessage: ex.InnerException?.Message ?? ex.Message
            );
            return (null, ex.InnerException?.Message ?? ex.Message);
        }
        catch (Exception ex)
        {
            await _auditService.LogAsync(
                action: "UPDATE",
                entityType: "Supplier",
                entityId: id,
                userId: _authState?.UserId,
                userType: _authState?.CurrentRole,
                userName: _authState?.CurrentUser?.Username,
                description: $"Error while updating supplier #{id}",
                severity: "Error",
                isSuccessful: false,
                errorMessage: ex.Message
            );
            return (null, ex.Message);
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var existing = await _db.Suppliers.FindAsync(id);
        if (existing is null)
        {
            await _auditService.LogAsync(
                action: "ARCHIVE",
                entityType: "Supplier",
                entityId: id,
                userId: _authState?.UserId,
                userType: _authState?.CurrentRole,
                userName: _authState?.CurrentUser?.Username,
                description: $"Failed to archive supplier #{id}: Not found",
                severity: "Warning",
                isSuccessful: false,
                errorMessage: "Supplier not found"
            );
            return false;
        }

        // Use centralized ArchiveService
        var (success, error) = await _archiveService.ArchiveAsync<Supplier>(
            id,
            "Archived by user",
            existing.SupplierName);

        return success;
    }
}