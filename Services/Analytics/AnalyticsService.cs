using Microsoft.EntityFrameworkCore;
using ResQLink.Data;

namespace ResQLink.Services.Analytics;

/// <summary>
/// Service for generating comprehensive dashboard analytics
/// </summary>
public class AnalyticsService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public AnalyticsService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<DashboardAnalytics> GetDashboardAnalyticsAsync(int? userId = null, string? userRole = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);
        var startOfLastMonth = startOfMonth.AddMonths(-1);
        var startOfWeek = now.AddDays(-(int)now.DayOfWeek);

        var analytics = new DashboardAnalytics();

        // Disaster Metrics
        var disasters = await context.Disasters.AsNoTracking().ToListAsync();
        analytics.TotalDisasters = disasters.Count;
        analytics.ActiveDisasters = disasters.Count(d => d.Status == "Active" || d.Status == "Open");
        analytics.DisastersThisMonth = disasters.Count(d => d.StartDate >= startOfMonth);
        analytics.DisastersLastMonth = disasters.Count(d => d.StartDate >= startOfLastMonth && d.StartDate < startOfMonth);
        analytics.DisasterGrowthRate = analytics.DisastersLastMonth > 0 
            ? ((analytics.DisastersThisMonth - analytics.DisastersLastMonth) / (double)analytics.DisastersLastMonth) * 100 
            : 0;

        // Evacuee Metrics
        var evacuees = await context.Evacuees.AsNoTracking().ToListAsync();
        analytics.TotalEvacuees = evacuees.Count;
        analytics.ActiveEvacuees = evacuees.Count(e => e.Status == "Active" || e.Status == "Sheltered");
        analytics.EvacueesThisMonth = evacuees.Count(e => e.RegisteredAt >= startOfMonth);
        analytics.EvacueesByStatus = evacuees.GroupBy(e => e.Status)
            .ToDictionary(g => g.Key, g => g.Count());

        // Shelter Metrics
        var shelters = await context.Shelters.AsNoTracking().ToListAsync();
        analytics.TotalShelters = shelters.Count;
        analytics.ActiveShelters = shelters.Count(s => s.IsActive);
        analytics.ShelterCapacity = shelters.Sum(s => s.Capacity);
        analytics.ShelterOccupancy = shelters.Sum(s => s.CurrentOccupancy);
        analytics.OccupancyRate = analytics.ShelterCapacity > 0 
            ? (analytics.ShelterOccupancy / (double)analytics.ShelterCapacity) * 100 
            : 0;

        // Inventory Metrics
        var stocks = await context.Stocks
            .Include(s => s.ReliefGood)
            .AsNoTracking()
            .ToListAsync();
        
        analytics.TotalReliefGoods = await context.ReliefGoods.CountAsync();
        analytics.LowStockItems = stocks.Count(s => s.Quantity > 0 && s.Quantity <= s.MaxCapacity * 0.25m);
        analytics.OutOfStockItems = stocks.Count(s => s.Quantity <= 0);
        analytics.TotalInventoryValue = stocks.Sum(s => s.Quantity * s.UnitCost);
        
        analytics.StockAlerts = stocks
            .Where(s => s.Quantity <= s.MaxCapacity * 0.25m)
            .OrderBy(s => s.Quantity)
            .Take(10)
            .Select(s => new StockAlert
            {
                StockId = s.StockId,
                ItemName = s.ReliefGood?.Name ?? "Unknown",
                CurrentQuantity = s.Quantity,
                MaxCapacity = s.MaxCapacity,
                Status = s.Status ?? "Unknown",
                AlertLevel = s.Quantity <= 0 ? "Critical" : s.Quantity <= s.MaxCapacity * 0.1m ? "Critical" : "Warning"
            })
            .ToList();

        // Financial Metrics
        var budgets = await context.BarangayBudgets
            .Include(b => b.Items)
            .AsNoTracking()
            .ToListAsync();
        analytics.TotalBudget = budgets.Sum(b => b.TotalAmount);
        // Calculate utilized from items
        var budgetUtilized = budgets.Sum(b => b.Items.Sum(i => i.Amount));
        analytics.BudgetUtilized = budgetUtilized;
        analytics.BudgetRemaining = analytics.TotalBudget - budgetUtilized;
        analytics.BudgetUtilizationRate = analytics.TotalBudget > 0 
            ? (double)((budgetUtilized / analytics.TotalBudget) * 100)
            : 0;

        var procurements = await context.ProcurementRequests.AsNoTracking().ToListAsync();
        analytics.PendingProcurements = procurements.Count(p => p.Status == "Pending" || p.Status == "Draft");
        analytics.ProcurementValue = procurements
            .Where(p => p.Status == "Pending" || p.Status == "Draft")
            .Sum(p => p.TotalAmount);

        // Distribution Metrics
        var distributions = await context.ResourceDistributions.AsNoTracking().ToListAsync();
        analytics.TotalDistributions = distributions.Count;
        analytics.DistributionsThisWeek = distributions.Count(d => d.DistributedAt >= startOfWeek);

        // Volunteer Metrics
        var volunteers = await context.Volunteers.AsNoTracking().ToListAsync();
        analytics.TotalVolunteers = volunteers.Count;
        analytics.ActiveVolunteers = volunteers.Count(v => v.Status == "Active");
        analytics.VolunteersOnLeave = volunteers.Count(v => v.Status == "On Leave");

        // Trending Data (Last 30 days)
        var last30Days = now.AddDays(-30);
        analytics.DisasterTrend = disasters
            .Where(d => d.StartDate >= last30Days)
            .GroupBy(d => d.StartDate.Date)
            .Select(g => new TrendDataPoint
            {
                Date = g.Key,
                Value = g.Count(),
                Label = g.Key.ToString("MMM dd")
            })
            .OrderBy(t => t.Date)
            .ToList();

        analytics.EvacueeTrend = evacuees
            .Where(e => e.RegisteredAt >= last30Days)
            .GroupBy(e => e.RegisteredAt.Date)
            .Select(g => new TrendDataPoint
            {
                Date = g.Key,
                Value = g.Count(),
                Label = g.Key.ToString("MMM dd")
            })
            .OrderBy(t => t.Date)
            .ToList();

        // Recent Activities (from audit log)
        analytics.RecentActivities = await context.AuditLogs
            .Where(a => a.Timestamp >= now.AddHours(-24))
            .OrderByDescending(a => a.Timestamp)
            .Take(20)
            .Select(a => new RecentActivity
            {
                Timestamp = a.Timestamp,
                ActivityType = a.Action,
                Description = a.Description ?? $"{a.Action} on {a.EntityType}",
                UserName = a.UserName,
                EntityType = a.EntityType,
                EntityId = a.EntityId
            })
            .ToListAsync();

        return analytics;
    }

    public async Task<Dictionary<string, int>> GetDisastersByTypeAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        return await context.Disasters
            .GroupBy(d => d.DisasterType)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Type, x => x.Count);
    }

    public async Task<Dictionary<string, int>> GetDisastersBySeverityAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        return await context.Disasters
            .GroupBy(d => d.Severity)
            .Select(g => new { Severity = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Severity, x => x.Count);
    }

    public async Task<List<TrendDataPoint>> GetStockTrendAsync(int reliefGoodId, int days = 30)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var startDate = DateTime.UtcNow.AddDays(-days);

        // Get stock changes from audit logs
        var stockChanges = await context.AuditLogs
            .Where(a => a.EntityType == "Stock" 
                     && a.EntityId == reliefGoodId 
                     && a.Timestamp >= startDate)
            .OrderBy(a => a.Timestamp)
            .Select(a => new TrendDataPoint
            {
                Date = a.Timestamp.Date,
                Value = 0, // Would need to parse from NewValues JSON
                Label = a.Timestamp.ToString("MMM dd")
            })
            .ToListAsync();

        return stockChanges;
    }
}
