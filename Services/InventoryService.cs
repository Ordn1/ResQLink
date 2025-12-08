using Microsoft.EntityFrameworkCore;
using ResQLink.Data;
using ResQLink.Data.Entities;
using ResQLink.Services;

namespace ResQLink.Services;

public class InventoryService
{
    private readonly AppDbContext _db;
    private readonly AuditService _auditService;
    private readonly AuthState _authState;

    public InventoryService(AppDbContext db, AuditService auditService, AuthState authState)
    {
        _db = db;
        _auditService = auditService;
        _authState = authState;
    }

    public async Task<List<ReliefGood>> GetAllAsync()
    {
        return await _db.ReliefGoods
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
        return await _db.ReliefGoods
            .Include(r => r.Categories)
                .ThenInclude(rc => rc.Category)
                    .ThenInclude(c => c.CategoryType)
            .Include(r => r.Stocks)
            .FirstOrDefaultAsync(r => r.RgId == id);
    }

    public async Task<(ReliefGood? entity, string? error)> CreateAsync(
        ReliefGood input,
        IEnumerable<int> categoryIds,
        int quantity = 0,
        decimal unitCost = 0m,
        int? barangayBudgetId = null)
    {
        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            input.CreatedAt = DateTime.UtcNow;
            _db.ReliefGoods.Add(input);
            await _db.SaveChangesAsync();

            // add pivot rows
            foreach (var cid in categoryIds.Distinct())
            {
                if (await _db.Categories.AnyAsync(c => c.CategoryId == cid))
                    _db.ReliefGoodCategories.Add(new ReliefGoodCategory { RgId = input.RgId, CategoryId = cid });
            }
            await _db.SaveChangesAsync();

            // 🔥 NEW: Log relief good creation
            await _auditService.LogAsync(
                action: "CREATE",
                entityType: "ReliefGood",
                entityId: input.RgId,
                userId: _authState.UserId,
                userType: _authState.CurrentRole,
                userName: _authState.CurrentUser?.Username,
                newValues: new
                {
                    input.RgId,
                    input.Name,
                    input.Unit,
                    input.Description,
                    Categories = categoryIds.ToList(),
                    InitialQuantity = quantity,
                    UnitCost = unitCost
                },
                description: $"Relief good '{input.Name}' created with initial quantity: {quantity} {input.Unit}",
                severity: "Info",
                isSuccessful: true
            );

            // Create initial stock with unit cost if quantity > 0
            if (quantity > 0)
            {
                var totalCost = quantity * unitCost;

                // Deduct from barangay budget if specified
                if (barangayBudgetId.HasValue && unitCost > 0)
                {
                    var budget = await _db.BarangayBudgets.FindAsync(barangayBudgetId.Value);
                    if (budget == null)
                    {
                        await tx.RollbackAsync();
                        
                        // 🔥 NEW: Log failed transaction
                        await _auditService.LogAsync(
                            action: "CREATE",
                            entityType: "ReliefGood",
                            entityId: input.RgId,
                            userId: _authState.UserId,
                            userType: _authState.CurrentRole,
                            userName: _authState.CurrentUser?.Username,
                            description: $"Failed to create relief good '{input.Name}': Barangay budget not found",
                            severity: "Error",
                            isSuccessful: false,
                            errorMessage: "Barangay budget not found."
                        );
                        
                        return (null, "Barangay budget not found.");
                    }

                    if (budget.Status != "Approved" && budget.Status != "Draft")
                    {
                        await tx.RollbackAsync();
                        
                        // 🔥 NEW: Log failed transaction
                        await _auditService.LogAsync(
                            action: "CREATE",
                            entityType: "ReliefGood",
                            entityId: input.RgId,
                            userId: _authState.UserId,
                            userType: _authState.CurrentRole,
                            userName: _authState.CurrentUser?.Username,
                            description: $"Failed to create relief good '{input.Name}': Budget status is {budget.Status}",
                            severity: "Warning",
                            isSuccessful: false,
                            errorMessage: $"Barangay budget is not active. Current status: {budget.Status}"
                        );
                        
                        return (null, $"Barangay budget is not active. Current status: {budget.Status}");
                    }

                    var currentSpent = await _db.BarangayBudgetItems
                        .Where(i => i.BudgetId == barangayBudgetId.Value)
                        .SumAsync(i => i.Amount);

                    var available = budget.TotalAmount - currentSpent;
                    if (totalCost > available)
                    {
                        await tx.RollbackAsync();
                        
                        // 🔥 NEW: Log failed transaction
                        await _auditService.LogAsync(
                            action: "CREATE",
                            entityType: "ReliefGood",
                            entityId: input.RgId,
                            userId: _authState.UserId,
                            userType: _authState.CurrentRole,
                            userName: _authState.CurrentUser?.Username,
                            description: $"Failed to create relief good '{input.Name}': Insufficient budget",
                            severity: "Warning",
                            isSuccessful: false,
                            errorMessage: $"Insufficient budget. Available: ₱{available:N2}, Required: ₱{totalCost:N2}"
                        );
                        
                        return (null, $"Insufficient budget. Available: ₱{available:N2}, Required: ₱{totalCost:N2}");
                    }

                    var budgetItem = new BarangayBudgetItem
                    {
                        BudgetId = barangayBudgetId.Value,
                        Category = "Inventory Purchase",
                        Description = $"Purchase of {quantity} {input.Unit} of {input.Name}",
                        Amount = totalCost,
                        Notes = $"Unit Cost: ₱{unitCost:N2} | Item Type: {(input.RequiresExpiration ? "Perishable/Medical" : "Non-Perishable")}",
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.BarangayBudgetItems.Add(budgetItem);
                    await _db.SaveChangesAsync();
                    
                    // 🔥 NEW: Log budget expenditure
                    await _auditService.LogBudgetExpenditureAsync(
                        budgetItemId: budgetItem.BudgetItemId,
                        budgetId: budget.BudgetId,
                        barangayName: budget.BarangayName,
                        category: "Inventory Purchase",
                        description: budgetItem.Description,
                        amount: totalCost,
                        remainingBudget: available - totalCost,
                        userId: _authState.UserId,
                        userName: _authState.CurrentUser?.Username
                    );
                }

                var stock = new Stock
                {
                    RgId = input.RgId,
                    Quantity = quantity,
                    MaxCapacity = 1000,
                    UnitCost = unitCost,
                    IsActive = true,
                    LastUpdated = DateTime.UtcNow
                };
                _db.Stocks.Add(stock);
                await _db.SaveChangesAsync();
                
                // 🔥 NEW: Log stock-in transaction
                await _auditService.LogStockInAsync(
                    stockId: stock.StockId,
                    itemName: input.Name,
                    quantity: quantity,
                    unit: input.Unit,
                    unitCost: unitCost,
                    totalCost: totalCost,
                    supplier: null,
                    userId: _authState.UserId,
                    userName: _authState.CurrentUser?.Username,
                    barangayBudgetId: barangayBudgetId
                );
            }

            await tx.CommitAsync();
            return (await GetByIdAsync(input.RgId), null);
        }
        catch (DbUpdateException ex)
        {
            await tx.RollbackAsync();
            
            // 🔥 NEW: Log database error
            await _auditService.LogAsync(
                action: "CREATE",
                entityType: "ReliefGood",
                entityId: null,
                userId: _authState.UserId,
                userType: _authState.CurrentRole,
                userName: _authState.CurrentUser?.Username,
                description: $"Database error while creating relief good '{input.Name}'",
                severity: "Error",
                isSuccessful: false,
                errorMessage: ex.InnerException?.Message ?? ex.Message
            );
            
            return (null, ex.InnerException?.Message ?? ex.Message);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            
            // 🔥 NEW: Log general error
            await _auditService.LogAsync(
                action: "CREATE",
                entityType: "ReliefGood",
                entityId: null,
                userId: _authState.UserId,
                userType: _authState.CurrentRole,
                userName: _authState.CurrentUser?.Username,
                description: $"Error while creating relief good '{input.Name}'",
                severity: "Error",
                isSuccessful: false,
                errorMessage: ex.Message
            );
            
            return (null, ex.Message);
        }
    }

    public async Task<(ReliefGood? entity, string? error)> UpdateAsync(int id, ReliefGood input, IEnumerable<int> categoryIds)
    {
        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var existing = await _db.ReliefGoods.Include(r => r.Categories).FirstOrDefaultAsync(r => r.RgId == id);
            if (existing == null)
            {
                // 🔥 NEW: Log not found error
                await _auditService.LogAsync(
                    action: "UPDATE",
                    entityType: "ReliefGood",
                    entityId: id,
                    userId: _authState.UserId,
                    userType: _authState.CurrentRole,
                    userName: _authState.CurrentUser?.Username,
                    description: $"Failed to update relief good #{id}: Item not found",
                    severity: "Warning",
                    isSuccessful: false,
                    errorMessage: "Item not found"
                );
                
                return (null, "Item not found");
            }

            // 🔥 NEW: Capture old values for audit
            var oldValues = new
            {
                existing.Name,
                existing.Unit,
                existing.Description,
                existing.IsActive,
                Categories = existing.Categories.Select(c => c.CategoryId).ToList()
            };

            existing.Name = input.Name;
            existing.Unit = input.Unit;
            existing.Description = input.Description;
            existing.IsActive = input.IsActive;

            // sync categories
            var currentIds = existing.Categories.Select(c => c.CategoryId).ToHashSet();
            var newIds = categoryIds.Distinct().ToHashSet();

            // remove
            foreach (var rc in existing.Categories.Where(c => !newIds.Contains(c.CategoryId)).ToList())
                _db.ReliefGoodCategories.Remove(rc);
            // add
            foreach (var addId in newIds.Where(id2 => !currentIds.Contains(id2)))
                if (await _db.Categories.AnyAsync(c => c.CategoryId == addId))
                    _db.ReliefGoodCategories.Add(new ReliefGoodCategory { RgId = existing.RgId, CategoryId = addId });

            await _db.SaveChangesAsync();
            
            // 🔥 NEW: Log update transaction
            await _auditService.LogAsync(
                action: "UPDATE",
                entityType: "ReliefGood",
                entityId: id,
                userId: _authState.UserId,
                userType: _authState.CurrentRole,
                userName: _authState.CurrentUser?.Username,
                oldValues: oldValues,
                newValues: new
                {
                    input.Name,
                    input.Unit,
                    input.Description,
                    input.IsActive,
                    Categories = newIds.ToList()
                },
                description: $"Relief good '{input.Name}' (ID: {id}) updated",
                severity: "Info",
                isSuccessful: true
            );
            
            await tx.CommitAsync();
            return (await GetByIdAsync(existing.RgId), null);
        }
        catch (DbUpdateException ex)
        {
            await tx.RollbackAsync();
            
            // 🔥 NEW: Log database error
            await _auditService.LogAsync(
                action: "UPDATE",
                entityType: "ReliefGood",
                entityId: id,
                userId: _authState.UserId,
                userType: _authState.CurrentRole,
                userName: _authState.CurrentUser?.Username,
                description: $"Database error while updating relief good #{id}",
                severity: "Error",
                isSuccessful: false,
                errorMessage: ex.InnerException?.Message ?? ex.Message
            );
            
            return (null, ex.InnerException?.Message ?? ex.Message);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            
            // 🔥 NEW: Log general error
            await _auditService.LogAsync(
                action: "UPDATE",
                entityType: "ReliefGood",
                entityId: id,
                userId: _authState.UserId,
                userType: _authState.CurrentRole,
                userName: _authState.CurrentUser?.Username,
                description: $"Error while updating relief good #{id}",
                severity: "Error",
                isSuccessful: false,
                errorMessage: ex.Message
            );
            
            return (null, ex.Message);
        }
    }

    /// <summary>
    /// Add stock with cost and deduct from barangay budget
    /// </summary>
    public async Task<(Stock? stock, string? error)> AddStockWithCostAsync(
        int rgId,
        int quantity,
        decimal unitCost,
        int? barangayBudgetId = null,
        int maxCapacity = 1000,
        string? location = null,
        string? supplier = null)
    {
        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            if (quantity <= 0)
            {
                // 🔥 NEW: Log validation error
                await _auditService.LogAsync(
                    action: "STOCK_IN",
                    entityType: "Stock",
                    entityId: null,
                    userId: _authState.UserId,
                    userType: _authState.CurrentRole,
                    userName: _authState.CurrentUser?.Username,
                    description: $"Failed to add stock for ReliefGood #{rgId}: Invalid quantity",
                    severity: "Warning",
                    isSuccessful: false,
                    errorMessage: "Quantity must be greater than zero."
                );
                
                return (null, "Quantity must be greater than zero.");
            }

            if (unitCost < 0)
            {
                // 🔥 NEW: Log validation error
                await _auditService.LogAsync(
                    action: "STOCK_IN",
                    entityType: "Stock",
                    entityId: null,
                    userId: _authState.UserId,
                    userType: _authState.CurrentRole,
                    userName: _authState.CurrentUser?.Username,
                    description: $"Failed to add stock for ReliefGood #{rgId}: Invalid unit cost",
                    severity: "Warning",
                    isSuccessful: false,
                    errorMessage: "Unit cost cannot be negative."
                );
                
                return (null, "Unit cost cannot be negative.");
            }

            var reliefGood = await _db.ReliefGoods.FindAsync(rgId);
            if (reliefGood == null)
            {
                // 🔥 NEW: Log not found error
                await _auditService.LogAsync(
                    action: "STOCK_IN",
                    entityType: "Stock",
                    entityId: null,
                    userId: _authState.UserId,
                    userType: _authState.CurrentRole,
                    userName: _authState.CurrentUser?.Username,
                    description: $"Failed to add stock: ReliefGood #{rgId} not found",
                    severity: "Warning",
                    isSuccessful: false,
                    errorMessage: "Relief good not found."
                );
                
                return (null, "Relief good not found.");
            }

            var totalCost = quantity * unitCost;

            // Deduct from barangay budget if specified
            if (barangayBudgetId.HasValue && unitCost > 0)
            {
                var budget = await _db.BarangayBudgets.FindAsync(barangayBudgetId.Value);
                if (budget == null)
                {
                    await tx.RollbackAsync();
                    
                    // 🔥 NEW: Log budget error
                    await _auditService.LogAsync(
                        action: "STOCK_IN",
                        entityType: "Stock",
                        entityId: null,
                        userId: _authState.UserId,
                        userType: _authState.CurrentRole,
                        userName: _authState.CurrentUser?.Username,
                        description: $"Failed to add stock for '{reliefGood.Name}': Budget #{barangayBudgetId.Value} not found",
                        severity: "Error",
                        isSuccessful: false,
                        errorMessage: "Barangay budget not found."
                    );
                    
                    return (null, "Barangay budget not found.");
                }

                if (budget.Status != "Approved" && budget.Status != "Draft")
                {
                    await tx.RollbackAsync();
                    
                    // 🔥 NEW: Log budget status error
                    await _auditService.LogAsync(
                        action: "STOCK_IN",
                        entityType: "Stock",
                        entityId: null,
                        userId: _authState.UserId,
                        userType: _authState.CurrentRole,
                        userName: _authState.CurrentUser?.Username,
                        description: $"Failed to add stock for '{reliefGood.Name}': Budget status is {budget.Status}",
                        severity: "Warning",
                        isSuccessful: false,
                        errorMessage: $"Barangay budget is not active. Current status: {budget.Status}"
                    );
                    
                    return (null, $"Barangay budget is not active. Current status: {budget.Status}");
                }

                var currentSpent = await _db.BarangayBudgetItems
                    .Where(i => i.BudgetId == barangayBudgetId.Value)
                    .SumAsync(i => i.Amount);

                var available = budget.TotalAmount - currentSpent;
                if (totalCost > available)
                {
                    await tx.RollbackAsync();
                    
                    // 🔥 NEW: Log insufficient funds error
                    await _auditService.LogAsync(
                        action: "STOCK_IN",
                        entityType: "Stock",
                        entityId: null,
                        userId: _authState.UserId,
                        userType: _authState.CurrentRole,
                        userName: _authState.CurrentUser?.Username,
                        description: $"Failed to add stock for '{reliefGood.Name}': Insufficient budget",
                        severity: "Warning",
                        isSuccessful: false,
                        errorMessage: $"Insufficient budget. Available: ₱{available:N2}, Required: ₱{totalCost:N2}"
                    );
                    
                    return (null, $"Insufficient budget. Available: ₱{available:N2}, Required: ₱{totalCost:N2}");
                }

                var budgetItem = new BarangayBudgetItem
                {
                    BudgetId = barangayBudgetId.Value,
                    Category = "Inventory Purchase",
                    Description = $"Stock addition: {quantity} {reliefGood.Unit} of {reliefGood.Name}",
                    Amount = totalCost,
                    Notes = $"Unit Cost: ₱{unitCost:N2}" + (supplier != null ? $" | Supplier: {supplier}" : ""),
                    CreatedAt = DateTime.UtcNow
                };
                _db.BarangayBudgetItems.Add(budgetItem);
                await _db.SaveChangesAsync();
                
                // Log budget expenditure
                await _auditService.LogBudgetExpenditureAsync(
                    budgetItemId: budgetItem.BudgetItemId,
                    budgetId: budget.BudgetId,
                    barangayName: budget.BarangayName,
                    category: "Inventory Purchase",
                    description: budgetItem.Description,
                    amount: totalCost,
                    remainingBudget: available - totalCost,
                    userId: _authState.UserId,
                    userName: _authState.CurrentUser?.Username
                );
            }

            var stock = new Stock
            {
                RgId = rgId,
                Quantity = quantity,
                MaxCapacity = maxCapacity,
                UnitCost = unitCost,
                Location = location,
                IsActive = true,
                LastUpdated = DateTime.UtcNow
            };

            _db.Stocks.Add(stock);
            await _db.SaveChangesAsync();

            // Log stock-in transaction
            await _auditService.LogStockInAsync(
                stockId: stock.StockId,
                itemName: reliefGood.Name,
                quantity: quantity,
                unit: reliefGood.Unit,
                unitCost: unitCost,
                totalCost: totalCost,
                supplier: supplier,
                userId: _authState.UserId,
                userName: _authState.CurrentUser?.Username,
                barangayBudgetId: barangayBudgetId
            );

            await tx.CommitAsync();
            return (stock, null);
        }
        catch (DbUpdateException ex)
        {
            await tx.RollbackAsync();
            
            // 🔥 NEW: Log database error
            await _auditService.LogAsync(
                action: "STOCK_IN",
                entityType: "Stock",
                entityId: null,
                userId: _authState.UserId,
                userType: _authState.CurrentRole,
                userName: _authState.CurrentUser?.Username,
                description: $"Database error while adding stock for ReliefGood #{rgId}",
                severity: "Error",
                isSuccessful: false,
                errorMessage: ex.InnerException?.Message ?? ex.Message
            );
            
            return (null, ex.InnerException?.Message ?? ex.Message);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            
            // 🔥 NEW: Log general error
            await _auditService.LogAsync(
                action: "STOCK_IN",
                entityType: "Stock",
                entityId: null,
                userId: _authState.UserId,
                userType: _authState.CurrentRole,
                userName: _authState.CurrentUser?.Username,
                description: $"Error while adding stock for ReliefGood #{rgId}",
                severity: "Error",
                isSuccessful: false,
                errorMessage: ex.Message
            );
            
            return (null, ex.Message);
        }
    }

    public async Task<(bool ok, string? error)> DeleteAsync(int id, bool softIfStock = true)
    {
        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var item = await _db.ReliefGoods.Include(r => r.Stocks).FirstOrDefaultAsync(r => r.RgId == id);
            if (item == null)
            {
                // 🔥 NEW: Log not found error
                await _auditService.LogAsync(
                    action: "DELETE",
                    entityType: "ReliefGood",
                    entityId: id,
                    userId: _authState.UserId,
                    userType: _authState.CurrentRole,
                    userName: _authState.CurrentUser?.Username,
                    description: $"Failed to delete relief good #{id}: Item not found",
                    severity: "Warning",
                    isSuccessful: false,
                    errorMessage: "Item not found"
                );
                
                return (false, "Item not found");
            }

            // 🔥 NEW: Capture item details before deletion
            var itemDetails = new
            {
                item.RgId,
                item.Name,
                item.Unit,
                item.Description,
                StockCount = item.Stocks.Count,
                TotalQuantity = item.Stocks.Sum(s => s.Quantity)
            };

            if (softIfStock && item.Stocks.Any())
            {
                item.IsActive = false; // soft delete
                await _db.SaveChangesAsync();
                
                // 🔥 NEW: Log soft delete
                await _auditService.LogAsync(
                    action: "DELETE",
                    entityType: "ReliefGood",
                    entityId: id,
                    userId: _authState.UserId,
                    userType: _authState.CurrentRole,
                    userName: _authState.CurrentUser?.Username,
                    oldValues: itemDetails,
                    description: $"Relief good '{item.Name}' (ID: {id}) soft deleted (has {item.Stocks.Count} stock entries)",
                    severity: "Info",
                    isSuccessful: true
                );
            }
            else
            {
                // remove pivots first
                var pivots = await _db.ReliefGoodCategories.Where(rc => rc.RgId == id).ToListAsync();
                _db.ReliefGoodCategories.RemoveRange(pivots);
                _db.ReliefGoods.Remove(item);
                await _db.SaveChangesAsync();
                
                // 🔥 NEW: Log hard delete
                await _auditService.LogAsync(
                    action: "DELETE",
                    entityType: "ReliefGood",
                    entityId: id,
                    userId: _authState.UserId,
                    userType: _authState.CurrentRole,
                    userName: _authState.CurrentUser?.Username,
                    oldValues: itemDetails,
                    description: $"Relief good '{item.Name}' (ID: {id}) permanently deleted",
                    severity: "Warning",
                    isSuccessful: true
                );
            }

            await tx.CommitAsync();
            return (true, null);
        }
        catch (DbUpdateException ex)
        {
            await tx.RollbackAsync();
            
            // 🔥 NEW: Log database error
            await _auditService.LogAsync(
                action: "DELETE",
                entityType: "ReliefGood",
                entityId: id,
                userId: _authState.UserId,
                userType: _authState.CurrentRole,
                userName: _authState.CurrentUser?.Username,
                description: $"Database error while deleting relief good #{id}",
                severity: "Error",
                isSuccessful: false,
                errorMessage: ex.InnerException?.Message ?? ex.Message
            );
            
            return (false, ex.InnerException?.Message ?? ex.Message);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            
            // 🔥 NEW: Log general error
            await _auditService.LogAsync(
                action: "DELETE",
                entityType: "ReliefGood",
                entityId: id,
                userId: _authState.UserId,
                userType: _authState.CurrentRole,
                userName: _authState.CurrentUser?.Username,
                description: $"Error while deleting relief good #{id}",
                severity: "Error",
                isSuccessful: false,
                errorMessage: ex.Message
            );
            
            return (false, ex.Message);
        }
    }
}
