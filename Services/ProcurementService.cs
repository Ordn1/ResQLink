using Microsoft.EntityFrameworkCore;
using ResQLink.Data;
using ResQLink.Data.Entities;

namespace ResQLink.Services;

public class ProcurementService(AppDbContext db)
{
    public Task<List<ProcurementRequest>> GetAllAsync(string? status = null) =>
        db.ProcurementRequests
          .Include(r => r.Supplier)
          .Include(r => r.RequestedBy)
          .Include(r => r.Items)
          .Where(r => string.IsNullOrEmpty(status) || r.Status == status)
          .OrderByDescending(r => r.RequestDate)
          .AsNoTracking()
          .ToListAsync();

    public Task<ProcurementRequest?> GetAsync(int id) =>
        db.ProcurementRequests
          .Include(r => r.Supplier)
          .Include(r => r.RequestedBy)
          .Include(r => r.Items)
          .FirstOrDefaultAsync(r => r.RequestId == id);

    public async Task<(ProcurementRequest? request, string? error)> CreateAsync(ProcurementRequest input)
    {
        try
        {
            input.RequestDate = DateTime.UtcNow;
            db.ProcurementRequests.Add(input);
            await db.SaveChangesAsync();
            return (input, null);
        }
        catch (DbUpdateException ex) { return (null, ex.InnerException?.Message ?? ex.Message); }
        catch (Exception ex) { return (null, ex.Message); }
    }

    public async Task<(ProcurementRequest? request, string? error)> UpdateAsync(ProcurementRequest input)
    {
        try
        {
            var existing = await db.ProcurementRequests.FindAsync(input.RequestId);
            if (existing is null) return (null, "Request not found.");

            existing.BarangayName = input.BarangayName.Trim();
            existing.SupplierId = input.SupplierId;
            existing.Status = input.Status;
            existing.TotalAmount = input.TotalAmount;

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
            var request = await db.ProcurementRequests.Include(r => r.Items).FirstOrDefaultAsync(r => r.RequestId == id);
            if (request is null) return (false, "Request not found.");

            db.ProcurementRequestItems.RemoveRange(request.Items);
            db.ProcurementRequests.Remove(request);
            await db.SaveChangesAsync();
            return (true, null);
        }
        catch (DbUpdateException ex) { return (false, ex.InnerException?.Message ?? ex.Message); }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public async Task<(ProcurementRequestItem? item, string? error)> AddItemAsync(int requestId, ProcurementRequestItem item)
    {
        try
        {
            var exists = await db.ProcurementRequests.AnyAsync(r => r.RequestId == requestId);
            if (!exists) return (null, "Request not found.");

            item.RequestId = requestId;
            db.ProcurementRequestItems.Add(item);
            await db.SaveChangesAsync();
            return (item, null);
        }
        catch (DbUpdateException ex) { return (null, ex.InnerException?.Message ?? ex.Message); }
        catch (Exception ex) { return (null, ex.Message); }
    }

    public async Task<(ProcurementRequestItem? item, string? error)> UpdateItemAsync(ProcurementRequestItem item)
    {
        try
        {
            var existing = await db.ProcurementRequestItems.FindAsync(item.RequestItemId);
            if (existing is null) return (null, "Item not found.");

            existing.ItemName = item.ItemName.Trim();
            existing.Unit = item.Unit.Trim();
            existing.Quantity = item.Quantity;
            existing.UnitPrice = item.UnitPrice;

            await db.SaveChangesAsync();
            return (existing, null);
        }
        catch (DbUpdateException ex) { return (null, ex.InnerException?.Message ?? ex.Message); }
        catch (Exception ex) { return (null, ex.Message); }
    }

    public async Task<(bool ok, string? error)> DeleteItemAsync(int requestItemId)
    {
        try
        {
            var existing = await db.ProcurementRequestItems.FindAsync(requestItemId);
            if (existing is null) return (false, "Item not found.");

            db.ProcurementRequestItems.Remove(existing);
            await db.SaveChangesAsync();
            return (true, null);
        }
        catch (DbUpdateException ex) { return (false, ex.InnerException?.Message ?? ex.Message); }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public async Task<(bool ok, string? error)> SetStatusAsync(int requestId, string status)
    {
        try
        {
            var req = await db.ProcurementRequests.FindAsync(requestId);
            if (req is null) return (false, "Request not found.");
            req.Status = status;
            await db.SaveChangesAsync();
            return (true, null);
        }
        catch (DbUpdateException ex) { return (false, ex.InnerException?.Message ?? ex.Message); }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public async Task<decimal> RecalculateTotalAsync(int requestId)
    {
        var items = await db.ProcurementRequestItems.Where(i => i.RequestId == requestId).ToListAsync();
        var total = items.Sum(i => i.UnitPrice * i.Quantity);
        var req = await db.ProcurementRequests.FindAsync(requestId);
        if (req != null)
        {
            req.TotalAmount = total;
            await db.SaveChangesAsync();
        }
        return total;
    }
}