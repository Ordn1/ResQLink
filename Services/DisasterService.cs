using Microsoft.EntityFrameworkCore;
using ResQLink.Data;
using ResQLink.Data.Entities;

namespace ResQLink.Services;

public interface IDisasterService
{
    Task<List<Disaster>> GetAllAsync();
    Task<Disaster?> GetByIdAsync(int id);
    Task<(Disaster? disaster, string? error)> CreateAsync(Disaster disaster);
    Task<(Disaster? disaster, string? error)> UpdateAsync(Disaster disaster);
    Task<bool> DeleteAsync(int id);
    Task<List<Disaster>> GetActiveDisastersAsync();
    Task<int> GetActiveCountAsync();
}

public class DisasterService : IDisasterService
{
    private readonly AppDbContext _context;
    private readonly AuthState? _authState;
    private readonly AuditService _auditService;
    private readonly ArchiveService _archiveService;

    public DisasterService(AppDbContext context, AuditService auditService, ArchiveService archiveService, AuthState? authState = null)
    {
        _context = context;
        _auditService = auditService;
        _archiveService = archiveService;
        _authState = authState;
    }

    public async Task<List<Disaster>> GetAllAsync()
    {
        return await _context.Disasters
            .Include(d => d.Shelters)
            .Include(d => d.Evacuees)
            .OrderByDescending(d => d.StartDate)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Disaster?> GetByIdAsync(int id) =>
       await _context.Disasters
           .Include(d => d.Shelters)
            .Include(d => d.Evacuees)
        .AsNoTracking()
            .FirstOrDefaultAsync(d => d.DisasterId == id);
    

    public async Task<(Disaster? disaster, string? error)> CreateAsync(Disaster disaster)
    {
        try
        {
            // Check for duplicate disaster title (case-insensitive)
            var duplicate = await _context.Disasters
                .AnyAsync(d => d.Title.ToLower() == disaster.Title.ToLower());
            
            if (duplicate)
            {
                await _auditService.LogAsync(
                    action: "CREATE",
                    entityType: "Disaster",
                    entityId: null,
                    userId: _authState?.UserId,
                    userType: _authState?.CurrentRole,
                    userName: _authState?.CurrentUser?.Username,
                    description: $"Failed to create disaster '{disaster.Title}': Duplicate title",
                    severity: "Warning",
                    isSuccessful: false,
                    errorMessage: "A disaster with this title already exists"
                );
                return (null, "A disaster with this title already exists");
            }

            disaster.CreatedAt = DateTime.UtcNow;
            _context.Disasters.Add(disaster);
            await _context.SaveChangesAsync();
            
            // Log disaster creation
            await _auditService.LogAsync(
                action: "CREATE",
                entityType: "Disaster",
                entityId: disaster.DisasterId,
                userId: _authState?.UserId,
                userType: _authState?.CurrentRole,
                userName: _authState?.CurrentUser?.Username,
                newValues: new { disaster.Title, disaster.DisasterType, disaster.Severity, disaster.Status, disaster.Location, disaster.StartDate },
                description: $"Created disaster '{disaster.Title}' ({disaster.DisasterType})",
                severity: "Info",
                isSuccessful: true
            );
            
            return (disaster, null);
        }
        catch (DbUpdateException ex)
        {
            await _auditService.LogAsync(
                action: "CREATE",
                entityType: "Disaster",
                entityId: null,
                userId: _authState?.UserId,
                userType: _authState?.CurrentRole,
                userName: _authState?.CurrentUser?.Username,
                description: $"Database error while creating disaster '{disaster.Title}'",
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
                entityType: "Disaster",
                entityId: null,
                userId: _authState?.UserId,
                userType: _authState?.CurrentRole,
                userName: _authState?.CurrentUser?.Username,
                description: $"Error while creating disaster '{disaster.Title}'",
                severity: "Error",
                isSuccessful: false,
                errorMessage: ex.Message
            );
            return (null, ex.Message);
        }
    }

    public async Task<(Disaster? disaster, string? error)> UpdateAsync(Disaster disaster)
    {      
        try
        {
            // Load tracked original entity
            var existing = await _context.Disasters.FirstOrDefaultAsync(d => d.DisasterId == disaster.DisasterId);
            if (existing == null)
            {
                await _auditService.LogAsync(
                    action: "UPDATE",
                    entityType: "Disaster",
                    entityId: disaster.DisasterId,
                    userId: _authState?.UserId,
                    userType: _authState?.CurrentRole,
                    userName: _authState?.CurrentUser?.Username,
                    description: $"Failed to update disaster #{disaster.DisasterId}: Not found",
                    severity: "Warning",
                    isSuccessful: false,
                    errorMessage: "Disaster not found"
                );
                return (null, "Disaster not found");
            }

            // Check for duplicate disaster title (excluding current disaster)
            var duplicate = await _context.Disasters
                .AnyAsync(d => d.DisasterId != disaster.DisasterId && d.Title.ToLower() == disaster.Title.ToLower());
            
            if (duplicate)
            {
                await _auditService.LogAsync(
                    action: "UPDATE",
                    entityType: "Disaster",
                    entityId: disaster.DisasterId,
                    userId: _authState?.UserId,
                    userType: _authState?.CurrentRole,
                    userName: _authState?.CurrentUser?.Username,
                    description: $"Failed to update disaster #{disaster.DisasterId}: Duplicate title '{disaster.Title}'",
                    severity: "Warning",
                    isSuccessful: false,
                    errorMessage: "A disaster with this title already exists"
                );
                return (null, "A disaster with this title already exists");
            }

            var oldValues = new
            {
                existing.Title,
                existing.DisasterType,
                existing.Severity,
                existing.Status,
                existing.StartDate,
                existing.EndDate,
                existing.Location
            };

            // Apply scalar property updates (avoid overwriting nav collections unintentionally)
            existing.Title = disaster.Title;
            existing.DisasterType = disaster.DisasterType;
            existing.Severity = disaster.Severity;
            existing.Status = disaster.Status;
            existing.StartDate = disaster.StartDate;
            existing.EndDate = disaster.EndDate;
            existing.Location = disaster.Location;

            await _context.SaveChangesAsync();
            
            // Log disaster update
            await _auditService.LogAsync(
                action: "UPDATE",
                entityType: "Disaster",
                entityId: disaster.DisasterId,
                userId: _authState?.UserId,
                userType: _authState?.CurrentRole,
                userName: _authState?.CurrentUser?.Username,
                oldValues: oldValues,
                newValues: new { existing.Title, existing.DisasterType, existing.Severity, existing.Status, existing.Location },
                description: $"Updated disaster '{existing.Title}'",
                severity: "Info",
                isSuccessful: true
            );
            
            return (existing, null);
        }
        catch (DbUpdateException ex)
        {
            await _auditService.LogAsync(
                action: "UPDATE",
                entityType: "Disaster",
                entityId: disaster.DisasterId,
                userId: _authState?.UserId,
                userType: _authState?.CurrentRole,
                userName: _authState?.CurrentUser?.Username,
                description: $"Database error while updating disaster #{disaster.DisasterId}",
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
                entityType: "Disaster",
                entityId: disaster.DisasterId,
                userId: _authState?.UserId,
                userType: _authState?.CurrentRole,
                userName: _authState?.CurrentUser?.Username,
                description: $"Error while updating disaster #{disaster.DisasterId}",
                severity: "Error",
                isSuccessful: false,
                errorMessage: ex.Message
            );
            return (null, ex.Message);
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            var disaster = await _context.Disasters
                .Include(d => d.Evacuees)
                .FirstOrDefaultAsync(d => d.DisasterId == id);

            if (disaster == null)
                return false;

            // Build archive reason with related data
            var relatedInfo = new List<string>();
            if (disaster.Evacuees.Any())
                relatedInfo.Add($"{disaster.Evacuees.Count} evacuees");
            
            var shelterCount = await _context.Shelters.CountAsync(s => s.DisasterId == id);
            if (shelterCount > 0)
                relatedInfo.Add($"{shelterCount} shelters");
                
            var stockCount = await _context.Stocks.CountAsync(s => s.DisasterId == id);
            if (stockCount > 0)
                relatedInfo.Add($"{stockCount} stock entries");
            
            var reason = relatedInfo.Any() 
                ? $"Archived with {string.Join(", ", relatedInfo)}" 
                : "Archived by user";

            // Use centralized ArchiveService
            var (success, error) = await _archiveService.ArchiveAsync<Disaster>(
                id,
                reason,
                disaster.Title);

            if (!success)
            {
                await _auditService.LogAsync(
                    action: "ARCHIVE",
                    entityType: "Disaster",
                    entityId: id,
                    userId: _authState?.UserId,
                    userType: _authState?.CurrentRole,
                    userName: _authState?.CurrentUser?.Username,
                    description: $"Failed to archive disaster '{disaster.Title}': {error}",
                    severity: "Error",
                    isSuccessful: false,
                    errorMessage: error
                );
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            await _auditService.LogAsync(
                action: "ARCHIVE",
                entityType: "Disaster",
                entityId: id,
                userId: _authState?.UserId,
                userType: _authState?.CurrentRole,
                userName: _authState?.CurrentUser?.Username,
                description: $"Exception while archiving disaster #{id}: {ex.Message}",
                severity: "Error",
                isSuccessful: false,
                errorMessage: ex.Message
            );
            return false;
        }
    }

    public async Task<List<Disaster>> GetActiveDisastersAsync()
    {
        return await _context.Disasters
            .Where(d => d.Status.ToLower() == "active" || d.Status.ToLower() == "open")
            .OrderByDescending(d => d.StartDate)
            .ToListAsync();
    }

    public async Task<int> GetActiveCountAsync()
    {
        return await _context.Disasters
            .CountAsync(d => d.Status.ToLower() == "active" || d.Status.ToLower() == "open");
    }
}