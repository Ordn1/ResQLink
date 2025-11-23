using Microsoft.EntityFrameworkCore;
using ResQLink.Data;
using ResQLink.Data.Entities;

namespace ResQLink.Services;

public class SupplierService(AppDbContext db)
{
    public Task<List<Supplier>> GetAllAsync() =>
        db.Suppliers.OrderBy(s => s.SupplierName).AsNoTracking().ToListAsync();

    public Task<Supplier?> GetByIdAsync(int id) =>
        db.Suppliers.AsNoTracking().FirstOrDefaultAsync(s => s.SupplierId == id);

    public async Task<Supplier> CreateAsync(Supplier supplier)
    {
        supplier.CreatedAt = DateTime.UtcNow;
        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync();
        return supplier;
    }

    public async Task<Supplier?> UpdateAsync(int id, Supplier supplier)
    {
        var existing = await db.Suppliers.FindAsync(id);
        if (existing is null) return null;

        existing.SupplierName = supplier.SupplierName;
        existing.ContactPerson = supplier.ContactPerson;
        existing.Email = supplier.Email;
        existing.PhoneNumber = supplier.PhoneNumber;
        existing.Address = supplier.Address;
        existing.Notes = supplier.Notes;
        existing.IsActive = supplier.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var existing = await db.Suppliers.FindAsync(id);
        if (existing is null) return false;
        existing.IsActive = false;
        existing.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }
}