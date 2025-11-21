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
            .ToListAsync();
    }

    public async Task<Disaster?> GetByIdAsync(int id)
    {
        return await _context.Disasters
            .Include(d => d.Shelters)
            .Include(d => d.Evacuees)
            .FirstOrDefaultAsync(d => d.DisasterId == id);
    }

    public async Task<Disaster> CreateAsync(Disaster disaster)
    {
        disaster.CreatedAt = DateTime.UtcNow;
        _context.Disasters.Add(disaster);
        await _context.SaveChangesAsync();
        return disaster;
    }

    public async Task<Disaster> UpdateAsync(Disaster disaster)
    {
        _context.Disasters.Update(disaster);
        await _context.SaveChangesAsync();
        return disaster;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var disaster = await _context.Disasters.FindAsync(id);
        if (disaster == null)
            return false;

        _context.Disasters.Remove(disaster);
        await _context.SaveChangesAsync();
        return true;
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