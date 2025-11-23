using Microsoft.EntityFrameworkCore;
using ResQLink.Data;
using ResQLink.Data.Entities;

namespace ResQLink.Services;

public class ResourceAllocationService
{
    private readonly AppDbContext _db;

    public ResourceAllocationService(AppDbContext db) => _db = db;

    /// <summary>
    /// Allocates stock from central inventory to a shelter (creates ResourceAllocation and adjusts quantities).
    /// </summary>
    public async Task<(ResourceAllocation? allocation, string? error)> AllocateToShelterAsync(
        int stockId,
        int shelterId,
        int quantity,
        int allocatedByUserId)
    {
        using var transaction = await _db.Database.BeginTransactionAsync();

        try
        {
            var stock = await _db.Stocks
                .Include(s => s.ReliefGood)
                .FirstOrDefaultAsync(s => s.StockId == stockId && s.IsActive);

            if (stock == null)
                return (null, "Stock not found or inactive.");

            if (quantity <= 0)
                return (null, "Quantity must be greater than zero.");

            if (stock.Quantity < quantity)
                return (null, $"Insufficient stock. Available: {stock.Quantity}, Requested: {quantity}");

            var shelter = await _db.Shelters.FindAsync(shelterId);
            if (shelter == null || !shelter.IsActive)
                return (null, "Shelter not found or inactive.");

            var user = await _db.Users.FindAsync(allocatedByUserId);
            if (user == null || !user.IsActive)
                return (null, "User not found or inactive.");

            // Decrease central stock
            stock.Quantity -= quantity;
            stock.LastUpdated = DateTime.UtcNow;

            // Existing shelter stock?
            var existingShelterStock = await _db.Stocks
                .FirstOrDefaultAsync(s => s.RgId == stock.RgId &&
                                          s.ShelterId == shelterId &&
                                          s.IsActive);

            if (existingShelterStock != null)
            {
                existingShelterStock.Quantity += quantity;
                existingShelterStock.LastUpdated = DateTime.UtcNow;
            }
            else
            {
                var shelterStock = new Stock
                {
                    RgId = stock.RgId,
                    ShelterId = shelterId,
                    Quantity = quantity,
                    MaxCapacity = 1000,
                    Location = shelter.Name,
                    IsActive = true,
                    LastUpdated = DateTime.UtcNow
                };
                _db.Stocks.Add(shelterStock);
            }

            var allocation = new ResourceAllocation
            {
                StockId = stockId,
                ShelterId = shelterId,
                AllocatedByUserId = allocatedByUserId,
                AllocatedQuantity = quantity,
                AllocatedAt = DateTime.UtcNow
            };

            _db.ResourceAllocations.Add(allocation);

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            // Reload navigation/computed properties
            await _db.Entry(allocation).ReloadAsync();
            await _db.Entry(allocation)
                .Reference(a => a.Stock)
                .Query()
                .Include(s => s.ReliefGood)
                .LoadAsync();
            await _db.Entry(allocation).Reference(a => a.Shelter).LoadAsync();
            await _db.Entry(allocation).Reference(a => a.AllocatedBy).LoadAsync();

            return (allocation, null);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (null, $"Transaction failed: {ex.Message}");
        }
    }

    public Task<List<ResourceAllocation>> GetAllAsync() =>
        _db.ResourceAllocations
            .Include(a => a.Stock).ThenInclude(s => s.ReliefGood)
            .Include(a => a.Shelter)
            .Include(a => a.AllocatedBy)
            .OrderByDescending(a => a.AllocatedAt)
            .ToListAsync();

    public Task<List<ResourceAllocation>> GetByShelterAsync(int shelterId) =>
        _db.ResourceAllocations
            .Include(a => a.Stock).ThenInclude(s => s.ReliefGood)
            .Include(a => a.AllocatedBy)
            .Where(a => a.ShelterId == shelterId)
            .OrderByDescending(a => a.AllocatedAt)
            .ToListAsync();

    public Task<List<Stock>> GetShelterStockAsync(int shelterId) =>
        _db.Stocks
            .Include(s => s.ReliefGood)
            .Where(s => s.ShelterId == shelterId && s.IsActive)
            .OrderBy(s => s.ReliefGood.Name)
            .ToListAsync();
}
