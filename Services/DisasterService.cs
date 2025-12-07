using Microsoft.EntityFrameworkCore;
using ResQLink.Data;
using ResQLink.Data.Entities;

namespace ResQLink.Services;

public interface IDisasterService
{
    Task<List<Disaster>> GetAllAsync();
    Task<Disaster?> GetByIdAsync(int id);
    Task<Disaster> CreateAsync(Disaster disaster);
    Task<Disaster> UpdateAsync(Disaster disaster);
    Task<bool> DeleteAsync(int id);
    Task<List<Disaster>> GetActiveDisastersAsync();
    Task<int> GetActiveCountAsync();
}

public class DisasterService : IDisasterService
{
    private readonly AppDbContext _context;

    public DisasterService(AppDbContext context)
    {
        _context = context;
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
    

    public async Task<Disaster> CreateAsync(Disaster disaster)
    {
        disaster.CreatedAt = DateTime.UtcNow;
        _context.Disasters.Add(disaster);
        await _context.SaveChangesAsync();
        return disaster;
    }

    public async Task<Disaster> UpdateAsync(Disaster disaster)
    {      
        // Load tracked original entity
        var existing = await _context.Disasters.FirstOrDefaultAsync(d => d.DisasterId == disaster.DisasterId);
        if (existing == null)
            throw new InvalidOperationException("Disaster not found.");

        // Apply scalar property updates (avoid overwriting nav collections unintentionally)
        existing.Title = disaster.Title;
        existing.DisasterType = disaster.DisasterType;
        existing.Severity = disaster.Severity;
        existing.Status = disaster.Status;
        existing.StartDate = disaster.StartDate;
        existing.EndDate = disaster.EndDate;
        existing.Location = disaster.Location;
        // existing.CreatedAt remains unchanged

        await _context.SaveChangesAsync();
        return existing;
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

            // Check for related required foreign key entities that cannot be orphaned
            var hasEvacuees = disaster.Evacuees.Any();
            if (hasEvacuees)
            {
                throw new InvalidOperationException(
                    $"Cannot delete disaster: {disaster.Evacuees.Count} evacuee(s) are linked to this disaster. " +
                    "Please reassign or delete evacuees first.");
            }

            // Check for report summaries (required FK)
            var hasReportSummaries = await _context.ReportDisasterSummaries
                .AnyAsync(r => r.DisasterId == id);
            if (hasReportSummaries)
            {
                throw new InvalidOperationException(
                    "Cannot delete disaster: Report summaries are linked to this disaster. " +
                    "Please delete reports first.");
            }

            // Check for report distributions (required FK)
            var hasReportDistributions = await _context.ReportResourceDistributions
                .AnyAsync(r => r.DisasterId == id);
            if (hasReportDistributions)
            {
                throw new InvalidOperationException(
                    "Cannot delete disaster: Report distributions are linked to this disaster. " +
                    "Please delete reports first.");
            }

            // For nullable FKs, we can either set them to null or delete them
            // Here we'll nullify them (shelters, donations, stocks can exist without a disaster)
            
            // Nullify Shelter references
            var shelters = await _context.Shelters
                .Where(s => s.DisasterId == id)
                .ToListAsync();
            foreach (var shelter in shelters)
            {
                shelter.DisasterId = null;
            }

            // Nullify Donation references
            var donations = await _context.Donations
                .Where(d => d.DisasterId == id)
                .ToListAsync();
            foreach (var donation in donations)
            {
                donation.DisasterId = null;
            }

            // Nullify Stock references
            var stocks = await _context.Stocks
                .Where(s => s.DisasterId == id)
                .ToListAsync();
            foreach (var stock in stocks)
            {
                stock.DisasterId = null;
            }

            // Now safe to delete the disaster
            _context.Disasters.Remove(disaster);
            await _context.SaveChangesAsync();
            
            return true;
        }
        catch (DbUpdateException ex)
        {
            // Wrap database exceptions with more context
            throw new InvalidOperationException(
                $"An error occurred while deleting the disaster. {ex.InnerException?.Message ?? ex.Message}", 
                ex);
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