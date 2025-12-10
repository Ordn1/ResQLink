using Microsoft.EntityFrameworkCore;
using ResQLink.Data;
using ResQLink.Data.Entities;

namespace ResQLink.Services;

public class ProcurementService
{
    private readonly AppDbContext _db;
    private readonly AuditService? _auditService;
    private readonly AuthState? _authState;

    public ProcurementService(AppDbContext db, AuditService? auditService = null, AuthState? authState = null)
    {
        _db = db;
        _auditService = auditService;
        _authState = authState;
    }

    public Task<List<ProcurementRequest>> GetAllAsync(string? status = null) =>
        _db.ProcurementRequests
          .Include(r => r.Supplier)
          .Include(r => r.RequestedBy)
          .Include(r => r.Items)
          .Where(r => string.IsNullOrEmpty(status) || r.Status == status)
          .OrderByDescending(r => r.RequestDate)
          .AsNoTracking()
          .ToListAsync();

    public Task<ProcurementRequest?> GetAsync(int id) =>
        _db.ProcurementRequests
          .Include(r => r.Supplier)
          .Include(r => r.RequestedBy)
          .Include(r => r.Items)
          .FirstOrDefaultAsync(r => r.RequestId == id);

    public async Task<(ProcurementRequest? request, string? error)> CreateAsync(ProcurementRequest input)
    {
        try
        {
            // Server-side validation
            if (input.TotalAmount < 0)
                return (null, "Total Amount cannot be negative.");
            if (string.IsNullOrWhiteSpace(input.BarangayName))
                return (null, "Barangay is required.");

            input.BarangayName = input.BarangayName.Trim();
            input.RequestDate = DateTime.UtcNow;
            _db.ProcurementRequests.Add(input);
            await _db.SaveChangesAsync();

            // Reload with relationships
            await _db.Entry(input).Reference(r => r.Supplier).LoadAsync();
            await _db.Entry(input).Reference(r => r.RequestedBy).LoadAsync();

            // 🔥 Log procurement request creation
            if (_auditService != null)
            {
                await _auditService.LogProcurementRequestAsync(
                    requestId: input.RequestId,
                    status: input.Status,
                    totalAmount: input.TotalAmount,
                    itemCount: 0, // Will be updated when items are added
                    barangayBudgetId: null,
                    barangayName: input.BarangayName,
                    userId: _authState?.UserId ?? input.RequestedByUserId,
                    userName: _authState?.CurrentUser?.Username ?? input.RequestedBy?.Username,
                    notes: $"Supplier: {input.Supplier?.SupplierName ?? "N/A"}"
                );
            }

            return (input, null);
        }
        catch (DbUpdateException ex)
        {
            // 🔥 Log procurement request creation error
            if (_auditService != null)
            {
                await _auditService.LogAsync(
                    action: "PROCUREMENT_REQUEST",
                    entityType: "ProcurementRequest",
                    entityId: null,
                    userId: _authState?.UserId,
                    userType: _authState?.CurrentRole,
                    userName: _authState?.CurrentUser?.Username,
                    description: $"Failed to create procurement request for {input.BarangayName}",
                    severity: "Error",
                    isSuccessful: false,
                    errorMessage: ex.InnerException?.Message ?? ex.Message
                );
            }
            return (null, ex.InnerException?.Message ?? ex.Message);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    public async Task<(ProcurementRequest? request, string? error)> UpdateAsync(ProcurementRequest input)
    {
        try
        {
            // Server-side validation
            if (input.TotalAmount < 0)
                return (null, "Total Amount cannot be negative.");
            if (string.IsNullOrWhiteSpace(input.BarangayName))
                return (null, "Barangay is required.");

            var existing = await _db.ProcurementRequests
                .Include(r => r.Supplier)
                .Include(r => r.Items)
                .FirstOrDefaultAsync(r => r.RequestId == input.RequestId);
            
            if (existing is null) return (null, "Request not found.");

            // 🔥 Capture old values
            var oldValues = new
            {
                existing.BarangayName,
                Supplier = existing.Supplier?.SupplierName,
                existing.Status,
                existing.TotalAmount
            };

            existing.BarangayName = input.BarangayName.Trim();
            existing.SupplierId = input.SupplierId;
            existing.Status = input.Status;
            existing.TotalAmount = input.TotalAmount;

            await _db.SaveChangesAsync();

            // Reload supplier
            await _db.Entry(existing).Reference(r => r.Supplier).LoadAsync();

            // 🔥 Log procurement request update
            if (_auditService != null)
            {
                await _auditService.LogAsync(
                    action: "PROCUREMENT_UPDATE",
                    entityType: "ProcurementRequest",
                    entityId: input.RequestId,
                    userId: _authState?.UserId,
                    userType: _authState?.CurrentRole,
                    userName: _authState?.CurrentUser?.Username,
                    oldValues: oldValues,
                    newValues: new
                    {
                        input.BarangayName,
                        Supplier = existing.Supplier?.SupplierName,
                        input.Status,
                        input.TotalAmount
                    },
                    description: $"Procurement request #{input.RequestId} updated for {input.BarangayName}: ₱{input.TotalAmount:N2} - Status: {input.Status}",
                    severity: "Info",
                    isSuccessful: true
                );
            }

            return (existing, null);
        }
        catch (DbUpdateException ex)
        {
            return (null, ex.InnerException?.Message ?? ex.Message);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    public async Task<(bool ok, string? error)> DeleteAsync(int id)
    {
        try
        {
            var request = await _db.ProcurementRequests
                .Include(r => r.Items)
                .Include(r => r.Supplier)
                .FirstOrDefaultAsync(r => r.RequestId == id);
            
            if (request is null) return (false, "Request not found.");

            // 🔥 Capture request details
            var requestDetails = new
            {
                request.RequestId,
                request.BarangayName,
                Supplier = request.Supplier?.SupplierName,
                request.Status,
                request.TotalAmount,
                ItemCount = request.Items.Count
            };

            _db.ProcurementRequestItems.RemoveRange(request.Items);
            _db.ProcurementRequests.Remove(request);
            await _db.SaveChangesAsync();

            // 🔥 Log procurement request deletion
            if (_auditService != null)
            {
                await _auditService.LogAsync(
                    action: "PROCUREMENT_DELETE",
                    entityType: "ProcurementRequest",
                    entityId: id,
                    userId: _authState?.UserId,
                    userType: _authState?.CurrentRole,
                    userName: _authState?.CurrentUser?.Username,
                    oldValues: requestDetails,
                    description: $"Procurement request #{id} deleted: {request.BarangayName} - ₱{request.TotalAmount:N2}",
                    severity: "Warning",
                    isSuccessful: true
                );
            }

            return (true, null);
        }
        catch (DbUpdateException ex)
        {
            return (false, ex.InnerException?.Message ?? ex.Message);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(ProcurementRequestItem? item, string? error)> AddItemAsync(int requestId, ProcurementRequestItem item)
    {
        try
        {
            // Server-side validation
            if (item.Quantity < 0)
                return (null, "Quantity cannot be negative.");
            if (item.UnitPrice < 0)
                return (null, "Unit Price cannot be negative.");
            if (string.IsNullOrWhiteSpace(item.ItemName) || string.IsNullOrWhiteSpace(item.Unit))
                return (null, "Item and Unit are required.");

            var request = await _db.ProcurementRequests.FindAsync(requestId);
            if (request is null) return (null, "Request not found.");

            item.RequestId = requestId;
            _db.ProcurementRequestItems.Add(item);
            await _db.SaveChangesAsync();

            // 🔥 Log procurement item addition
            if (_auditService != null)
            {
                await _auditService.LogAsync(
                    action: "PROCUREMENT_ITEM_ADD",
                    entityType: "ProcurementRequestItem",
                    entityId: item.RequestItemId,
                    userId: _authState?.UserId,
                    userType: _authState?.CurrentRole,
                    userName: _authState?.CurrentUser?.Username,
                    newValues: new
                    {
                        item.ItemName,
                        item.Unit,
                        item.Quantity,
                        item.UnitPrice,
                        LineTotal = item.Quantity * item.UnitPrice
                    },
                    description: $"Item added to procurement request #{requestId}: {item.ItemName} - {item.Quantity} {item.Unit} @ ₱{item.UnitPrice:N2}",
                    severity: "Info",
                    isSuccessful: true
                );
            }

            return (item, null);
        }
        catch (DbUpdateException ex)
        {
            return (null, ex.InnerException?.Message ?? ex.Message);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    public async Task<(ProcurementRequestItem? item, string? error)> UpdateItemAsync(ProcurementRequestItem item)
    {
        try
        {
            // Server-side validation
            if (item.Quantity < 0)
                return (null, "Quantity cannot be negative.");
            if (item.UnitPrice < 0)
                return (null, "Unit Price cannot be negative.");
            if (string.IsNullOrWhiteSpace(item.ItemName) || string.IsNullOrWhiteSpace(item.Unit))
                return (null, "Item and Unit are required.");

            var existing = await _db.ProcurementRequestItems.FindAsync(item.RequestItemId);
            if (existing is null) return (null, "Item not found.");

            // 🔥 Capture old values
            var oldValues = new
            {
                existing.ItemName,
                existing.Unit,
                existing.Quantity,
                existing.UnitPrice,
                LineTotal = existing.Quantity * existing.UnitPrice
            };

            existing.ItemName = item.ItemName.Trim();
            existing.Unit = item.Unit.Trim();
            existing.Quantity = item.Quantity;
            existing.UnitPrice = item.UnitPrice;

            await _db.SaveChangesAsync();

            // 🔥 Log procurement item update
            if (_auditService != null)
            {
                await _auditService.LogAsync(
                    action: "PROCUREMENT_ITEM_UPDATE",
                    entityType: "ProcurementRequestItem",
                    entityId: item.RequestItemId,
                    userId: _authState?.UserId,
                    userType: _authState?.CurrentRole,
                    userName: _authState?.CurrentUser?.Username,
                    oldValues: oldValues,
                    newValues: new
                    {
                        item.ItemName,
                        item.Unit,
                        item.Quantity,
                        item.UnitPrice,
                        LineTotal = item.Quantity * item.UnitPrice
                    },
                    description: $"Procurement item #{item.RequestItemId} updated: {item.ItemName} - {item.Quantity} {item.Unit} @ ₱{item.UnitPrice:N2}",
                    severity: "Info",
                    isSuccessful: true
                );
            }

            return (existing, null);
        }
        catch (DbUpdateException ex)
        {
            return (null, ex.InnerException?.Message ?? ex.Message);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    public async Task<(bool ok, string? error)> DeleteItemAsync(int requestItemId)
    {
        try
        {
            var existing = await _db.ProcurementRequestItems.FindAsync(requestItemId);
            if (existing is null) return (false, "Item not found.");

            // 🔥 Capture item details
            var itemDetails = new
            {
                existing.ItemName,
                existing.Unit,
                existing.Quantity,
                existing.UnitPrice,
                LineTotal = existing.Quantity * existing.UnitPrice
            };

            _db.ProcurementRequestItems.Remove(existing);
            await _db.SaveChangesAsync();

            // 🔥 Log procurement item deletion
            if (_auditService != null)
            {
                await _auditService.LogAsync(
                    action: "PROCUREMENT_ITEM_DELETE",
                    entityType: "ProcurementRequestItem",
                    entityId: requestItemId,
                    userId: _authState?.UserId,
                    userType: _authState?.CurrentRole,
                    userName: _authState?.CurrentUser?.Username,
                    oldValues: itemDetails,
                    description: $"Procurement item deleted: {existing.ItemName} - {existing.Quantity} {existing.Unit} @ ₱{existing.UnitPrice:N2}",
                    severity: "Warning",
                    isSuccessful: true
                );
            }

            return (true, null);
        }
        catch (DbUpdateException ex)
        {
            return (false, ex.InnerException?.Message ?? ex.Message);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool ok, string? error)> SetStatusAsync(int requestId, string status)
    {
        try
        {
            var req = await _db.ProcurementRequests
                .Include(r => r.Supplier)
                .FirstOrDefaultAsync(r => r.RequestId == requestId);
            
            if (req is null) return (false, "Request not found.");

            var oldStatus = req.Status;
            req.Status = status;
            await _db.SaveChangesAsync();

            // 🔥 Log status change
            if (_auditService != null)
            {
                await _auditService.LogAsync(
                    action: "PROCUREMENT_STATUS_CHANGE",
                    entityType: "ProcurementRequest",
                    entityId: requestId,
                    userId: _authState?.UserId,
                    userType: _authState?.CurrentRole,
                    userName: _authState?.CurrentUser?.Username,
                    oldValues: new { Status = oldStatus },
                    newValues: new { Status = status },
                    description: $"Procurement request #{requestId} status changed from '{oldStatus}' to '{status}' for {req.BarangayName}",
                    severity: status == "Approved" ? "Info" : status == "Rejected" ? "Warning" : "Info",
                    isSuccessful: true
                );
            }

            return (true, null);
        }
        catch (DbUpdateException ex)
        {
            return (false, ex.InnerException?.Message ?? ex.Message);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<decimal> RecalculateTotalAsync(int requestId)
    {
        var items = await _db.ProcurementRequestItems.Where(i => i.RequestId == requestId).ToListAsync();
        var total = items.Sum(i => i.UnitPrice * i.Quantity);
        var req = await _db.ProcurementRequests.FindAsync(requestId);
        if (req != null)
        {
            req.TotalAmount = total;
            await _db.SaveChangesAsync();
        }
        return total;
    }
}