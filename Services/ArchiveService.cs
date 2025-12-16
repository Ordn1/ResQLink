using Microsoft.EntityFrameworkCore;
using ResQLink.Data;
using ResQLink.Data.Entities;
using System.Text.Json;

namespace ResQLink.Services;

/// <summary>
/// Centralized service for archiving and restoring entities
/// </summary>
public class ArchiveService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly AuditService _auditService;
    private readonly AuthState? _authState;

    public ArchiveService(
        IDbContextFactory<AppDbContext> contextFactory,
        AuditService auditService,
        AuthState? authState = null)
    {
        _contextFactory = contextFactory;
        _auditService = auditService;
        _authState = authState;
    }

    /// <summary>
    /// Archive an entity by storing it in the Archives table and deleting from source table
    /// </summary>
    public async Task<(bool success, string? error)> ArchiveAsync<T>(
        int entityId,
        string? reason = null,
        string? entityName = null) where T : class
    {
        using var db = await _contextFactory.CreateDbContextAsync();
        
        try
        {
            var entityType = typeof(T).Name;
            var userId = _authState?.UserId ?? 0;

            // Get the entity (ignore query filters to get even if already marked as archived)
            var dbSet = db.Set<T>();
            var entity = await dbSet.IgnoreQueryFilters()
                .FirstOrDefaultAsync(e => EF.Property<int>(e, GetPrimaryKeyName<T>()) == entityId);

            if (entity == null)
                return (false, $"{entityType} with ID {entityId} not found.");

            // Serialize entity to JSON
            var archivedData = JsonSerializer.Serialize(entity, new JsonSerializerOptions
            {
                WriteIndented = false,
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
            });

            // Create archive record
            var archive = new Archive
            {
                EntityType = entityType,
                EntityId = entityId,
                ArchivedData = archivedData,
                ArchivedAt = DateTime.UtcNow,
                ArchivedBy = userId,
                ArchiveReason = reason,
                EntityName = entityName ?? GetEntityDisplayName(entity)
            };

            db.Archives.Add(archive);

            // Remove the entity from its original table
            dbSet.Remove(entity);

            await db.SaveChangesAsync();

            // Log to audit
            await _auditService.LogAsync(
                action: "Archive",
                entityType: entityType,
                entityId: entityId,
                userId: userId,
                description: $"Archived {entityType} (ID: {entityId})",
                newValues: new { Reason = reason, EntityName = entityName }
            );

            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, $"Failed to archive: {ex.Message}");
        }
    }

    /// <summary>
    /// Restore an archived entity back to its original table
    /// </summary>
    public async Task<(bool success, string? error)> RestoreAsync<T>(int archiveId) where T : class
    {
        using var db = await _contextFactory.CreateDbContextAsync();
        
        try
        {
            var archive = await db.Archives
                .FirstOrDefaultAsync(a => a.ArchiveId == archiveId);

            if (archive == null)
                return (false, "Archive record not found.");

            var entityType = typeof(T).Name;
            if (archive.EntityType != entityType)
                return (false, $"Archive type mismatch. Expected {entityType}, found {archive.EntityType}");

            // Deserialize entity from JSON with proper options
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            var entity = JsonSerializer.Deserialize<T>(archive.ArchivedData, jsonOptions);
            if (entity == null)
                return (false, "Failed to deserialize archived data.");

            // Get table name and primary key
            var tableName = GetTableName<T>();
            var primaryKeyName = GetPrimaryKeyName<T>();
            var entityId = (int)typeof(T).GetProperty(primaryKeyName)!.GetValue(entity)!;
            
            Console.WriteLine($"Restoring {entityType} ID {entityId} to table {tableName}");
            
            // Check if entity with this ID already exists
            var existingEntity = await db.Set<T>()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(e => EF.Property<int>(e, primaryKeyName) == entityId);

            // Open connection before starting transaction
            await db.Database.OpenConnectionAsync();
            
            using var transaction = await db.Database.BeginTransactionAsync();
            
            try
            {
                // Enable IDENTITY_INSERT for this table
                var enableIdentityInsert = $"SET IDENTITY_INSERT {tableName} ON";
                Console.WriteLine($"Executing: {enableIdentityInsert}");
                await db.Database.ExecuteSqlRawAsync(enableIdentityInsert);

                if (existingEntity != null)
                {
                    Console.WriteLine($"Updating existing entity ID {entityId}");
                    // Entity exists - update it
                    db.Entry(existingEntity).CurrentValues.SetValues(entity);
                    db.Entry(existingEntity).State = EntityState.Modified;
                }
                else
                {
                    Console.WriteLine($"Inserting new entity ID {entityId}");
                    // Entity doesn't exist - insert it with explicit ID
                    db.Set<T>().Attach(entity);
                    db.Entry(entity).State = EntityState.Added;
                }

                // Remove archive record
                db.Archives.Remove(archive);

                // Save changes while IDENTITY_INSERT is ON
                await db.SaveChangesAsync();

                // Disable IDENTITY_INSERT
                var disableIdentityInsert = $"SET IDENTITY_INSERT {tableName} OFF";
                Console.WriteLine($"Executing: {disableIdentityInsert}");
                await db.Database.ExecuteSqlRawAsync(disableIdentityInsert);

                // Commit transaction
                await transaction.CommitAsync();

                var userId = _authState?.UserId ?? 0;
                await _auditService.LogAsync(
                    action: "Restore",
                    entityType: entityType,
                    entityId: archive.EntityId,
                    userId: userId,
                    description: $"Restored {entityType} from archive (Archive ID: {archiveId})",
                    newValues: new { RestoredFrom = archiveId }
                );

                return (true, null);
            }
            catch (Exception ex)
            {
                // Rollback transaction on error
                await transaction.RollbackAsync();
                
                // Try to disable IDENTITY_INSERT even on error
                try
                {
                    await db.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT {tableName} OFF");
                }
                catch
                {
                    // Ignore errors when disabling
                }
                
                throw;
            }
        }
        catch (DbUpdateException ex)
        {
            var innerMessage = ex.InnerException?.Message ?? ex.Message;
            Console.WriteLine($"DbUpdateException during restore: {innerMessage}");
            return (false, $"Database error during restore: {innerMessage}");
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"JsonException during restore: {ex.Message}");
            return (false, $"Failed to deserialize archived data: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception during restore: {ex.Message}\nStack: {ex.StackTrace}");
            return (false, $"Failed to restore: {ex.Message}");
        }
    }

    /// <summary>
    /// Get all archived records for a specific entity type
    /// </summary>
    public async Task<List<Archive>> GetArchivedRecordsAsync<T>() where T : class
    {
        using var db = await _contextFactory.CreateDbContextAsync();
        
        var entityType = typeof(T).Name;
        return await db.Archives
            .Include(a => a.ArchivedByUser)
            .Where(a => a.EntityType == entityType)
            .OrderByDescending(a => a.ArchivedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>
    /// Get all archived records (all types)
    /// </summary>
    public async Task<List<Archive>> GetAllArchivedRecordsAsync()
    {
        using var db = await _contextFactory.CreateDbContextAsync();
        
        return await db.Archives
            .Include(a => a.ArchivedByUser)
            .OrderByDescending(a => a.ArchivedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>
    /// Get a specific archive record
    /// </summary>
    public async Task<Archive?> GetArchiveByIdAsync(int archiveId)
    {
        using var db = await _contextFactory.CreateDbContextAsync();
        
        return await db.Archives
            .Include(a => a.ArchivedByUser)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.ArchiveId == archiveId);
    }

    /// <summary>
    /// Permanently delete an archive record (cannot be undone)
    /// </summary>
    public async Task<(bool success, string? error)> DeletePermanentlyAsync(int archiveId)
    {
        using var db = await _contextFactory.CreateDbContextAsync();
        
        try
        {
            var archive = await db.Archives.FindAsync(archiveId);
            if (archive == null)
                return (false, "Archive record not found.");

            var userId = _authState?.UserId ?? 0;
            var entityType = archive.EntityType;
            var entityId = archive.EntityId;

            db.Archives.Remove(archive);
            await db.SaveChangesAsync();

            await _auditService.LogAsync(
                action: "PermanentDelete",
                entityType: "Archives",
                entityId: archiveId,
                userId: userId,
                description: $"Permanently deleted archive (ID: {archiveId}, Type: {entityType}, Entity ID: {entityId})",
                oldValues: new { EntityType = entityType, EntityId = entityId }
            );

            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, $"Failed to delete permanently: {ex.Message}");
        }
    }

    /// <summary>
    /// Get the primary key property name for an entity type
    /// </summary>
    private string GetPrimaryKeyName<T>() where T : class
    {
        var type = typeof(T);
        return type.Name switch
        {
            "ReliefGood" => "RgId",
            "Category" => "CategoryId",
            "Disaster" => "DisasterId",
            "Supplier" => "SupplierId",
            "Stock" => "StockId",
            "ProcurementRequest" => "RequestId",
            "BarangayBudget" => "BudgetId",
            "Evacuee" => "EvacueeId",
            "Shelter" => "ShelterId",
            "Volunteer" => "VolunteerId",
            "User" => "UserId",
            _ => $"{type.Name}Id"
        };
    }

    /// <summary>
    /// Get the database table name for an entity type
    /// </summary>
    private string GetTableName<T>() where T : class
    {
        try
        {
            using var db = _contextFactory.CreateDbContext();
            var entityType = db.Model.FindEntityType(typeof(T));
            
            if (entityType != null)
            {
                var schema = entityType.GetSchema() ?? "dbo";
                var tableName = entityType.GetTableName();
                var result = $"[{schema}].[{tableName}]";
                Console.WriteLine($"GetTableName<{typeof(T).Name}>() = {result}");
                return result;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting table name from EF: {ex.Message}");
        }
        
        // Fallback to naming convention
        var type = typeof(T);
        var fallback = type.Name switch
        {
            "ReliefGood" => "[dbo].[Relief_Goods]",
            "Category" => "[dbo].[Categories]",
            "Disaster" => "[dbo].[Disasters]",
            "Supplier" => "[dbo].[Suppliers]",
            "Stock" => "[dbo].[Stocks]",
            "ProcurementRequest" => "[dbo].[ProcurementRequests]",
            "BarangayBudget" => "[dbo].[BarangayBudgets]",
            "Evacuee" => "[dbo].[Evacuees]",
            "Shelter" => "[dbo].[Shelters]",
            "Volunteer" => "[dbo].[Volunteers]",
            "User" => "[dbo].[Users]",
            _ => $"[dbo].[{type.Name}s]"
        };
        
        Console.WriteLine($"GetTableName<{typeof(T).Name}>() = {fallback} (fallback)");
        return fallback;
    }

    /// <summary>
    /// Get a display name for an entity
    /// </summary>
    private string? GetEntityDisplayName(object entity)
    {
        var type = entity.GetType();
        
        // Special handling for specific entity types
        switch (type.Name)
        {
            case "Evacuee":
            case "Volunteer":
            case "User":
                var firstName = type.GetProperty("FirstName")?.GetValue(entity)?.ToString();
                var lastName = type.GetProperty("LastName")?.GetValue(entity)?.ToString();
                if (!string.IsNullOrEmpty(firstName) || !string.IsNullOrEmpty(lastName))
                {
                    return $"{firstName} {lastName}".Trim();
                }
                break;
                
            case "Shelter":
                var shelterName = type.GetProperty("Name")?.GetValue(entity)?.ToString();
                var location = type.GetProperty("Location")?.GetValue(entity)?.ToString();
                if (!string.IsNullOrEmpty(shelterName))
                {
                    return !string.IsNullOrEmpty(location) ? $"{shelterName} ({location})" : shelterName;
                }
                break;
                
            case "ProcurementRequest":
                var requestId = type.GetProperty("RequestId")?.GetValue(entity)?.ToString();
                var barangayName = type.GetProperty("BarangayName")?.GetValue(entity)?.ToString();
                return !string.IsNullOrEmpty(barangayName) 
                    ? $"Request #{requestId} - {barangayName}" 
                    : $"Request #{requestId}";
        }
        
        // Try common name properties
        var nameProperty = type.GetProperty("Name") 
            ?? type.GetProperty("Title") 
            ?? type.GetProperty("ItemName")
            ?? type.GetProperty("SupplierName")
            ?? type.GetProperty("BarangayName")
            ?? type.GetProperty("Username");

        return nameProperty?.GetValue(entity)?.ToString();
    }

    /// <summary>
    /// Get all archived records
    /// </summary>
    public async Task<List<Archive>> GetAllArchivesAsync()
    {
        using var db = await _contextFactory.CreateDbContextAsync();
        
        return await db.Archives
            .Include(a => a.ArchivedByUser)
            .OrderByDescending(a => a.ArchivedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>
    /// Search archived records by entity name or type
    /// </summary>
    public async Task<List<Archive>> SearchArchivedRecordsAsync(string searchTerm)
    {
        using var db = await _contextFactory.CreateDbContextAsync();
        
        var term = searchTerm.ToLower();
        return await db.Archives
            .Include(a => a.ArchivedByUser)
            .Where(a => 
                a.EntityType.ToLower().Contains(term) ||
                (a.EntityName != null && a.EntityName.ToLower().Contains(term)) ||
                (a.ArchiveReason != null && a.ArchiveReason.ToLower().Contains(term)))
            .OrderByDescending(a => a.ArchivedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>
    /// Get count of archived records by type
    /// </summary>
    public async Task<Dictionary<string, int>> GetArchiveCountsByTypeAsync()
    {
        using var db = await _contextFactory.CreateDbContextAsync();
        
        return await db.Archives
            .GroupBy(a => a.EntityType)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Type, x => x.Count);
    }
}
