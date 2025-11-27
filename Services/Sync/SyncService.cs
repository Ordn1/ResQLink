using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ResQLink.Data;
using ResQLink.Data.Entities;

namespace ResQLink.Services.Sync;

public class SyncService : ISyncService
{
    private readonly IDbContextFactory<AppDbContext> _localFactory;
    private readonly ILogger<SyncService> _logger;
    private readonly SyncSettings _settings;
    private Timer? _timer;

    public SyncService(IDbContextFactory<AppDbContext> localFactory,
                       ILogger<SyncService> logger,
                       SyncSettings settings)
    {
        _localFactory = localFactory;
        _logger = logger;
        _settings = settings;
    }

    public TimeSpan? CurrentInterval { get; private set; }

    private AppDbContext CreateRemoteContext()
    {
        if (string.IsNullOrWhiteSpace(_settings.RemoteConnectionString))
            throw new InvalidOperationException("RemoteConnectionString not configured.");
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_settings.RemoteConnectionString)
#if DEBUG
            .EnableDetailedErrors()
            .EnableSensitiveDataLogging()
#endif
            .Options;
        return new AppDbContext(opts);
    }

    public async Task<bool> CheckOnlineAsync(CancellationToken ct = default)
    {
        if (!_settings.RemoteEnabled || string.IsNullOrWhiteSpace(_settings.RemoteConnectionString))
            return false;
        try
        {
            using var remote = CreateRemoteContext();
            return await remote.Database.CanConnectAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Remote DB connectivity failed");
            return false;
        }
    }

    public async Task<(bool ok, string? error)> SyncNowAsync(CancellationToken ct = default)
    {
        if (!await CheckOnlineAsync(ct)) return (false, "Remote offline");
        var pull = await PullAsync(ct);
        if (!pull.ok) return pull;
        var push = await PushAsync(ct);
        return push;
    }

    public async Task<(bool ok, string? error)> PullAsync(CancellationToken ct = default)
    {
        try
        {
            using var remote = CreateRemoteContext();
            await using var local = await _localFactory.CreateDbContextAsync(ct);

            var remoteDisasters = await remote.Disasters.AsNoTracking().ToListAsync(ct);
            foreach (var r in remoteDisasters)
            {
                var localEntity = await local.Disasters.FirstOrDefaultAsync(x => x.DisasterId == r.DisasterId, ct);
                if (localEntity == null)
                    local.Disasters.Add(r);
                else
                    local.Entry(localEntity).CurrentValues.SetValues(r);
            }
            await local.SaveChangesAsync(ct);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pull failed");
            return (false, ex.Message);
        }
    }

    // Push: sync tables from local -> remote. Parents first to satisfy FKs.
    public async Task<(bool ok, string? error)> PushAsync(CancellationToken ct = default)
    {
        try
        {
            using var remote = CreateRemoteContext();
            await using var local = await _localFactory.CreateDbContextAsync(ct);

            // Open connection and start a transaction covering the sequence
            await remote.Database.OpenConnectionAsync(ct);
            using var tx = await remote.Database.BeginTransactionAsync(ct);

            // Helper local functions
            async Task SyncEntitiesAsync<TEntity>(IEnumerable<TEntity> localRows, Func<TEntity, object> keySelector, string tableName)
                where TEntity : class
            {
                var toInsert = new List<TEntity>();
                foreach (var row in localRows)
                {
                    var key = keySelector(row);
                    var exists = await remote.Set<TEntity>().FindAsync(new object[] { key }, ct);
                    if (exists == null)
                        toInsert.Add(row);
                    else
                        remote.Entry(exists).CurrentValues.SetValues(row);
                }

                if (toInsert.Count > 0)
                {
                    // Enable identity insert for this table to preserve keys
                    await remote.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT [{tableName}] ON", ct);
                    foreach (var r in toInsert)
                    {
                        remote.Attach(r);
                        remote.Entry(r).State = EntityState.Added;
                    }
                    await remote.SaveChangesAsync(ct);
                    await remote.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT [{tableName}] OFF", ct);
                }
            }

            // Sync order (parents -> children)
            // 1. Category_Types
            var localCategoryTypes = await local.CategoryTypes.AsNoTracking().ToListAsync(ct);
            await SyncEntitiesAsync(localCategoryTypes, (x) => x.CategoryTypeId, "Category_Types");

            // 2. Categories
            var localCategories = await local.Categories.AsNoTracking().ToListAsync(ct);
            await SyncEntitiesAsync(localCategories, (x) => x.CategoryId, "Categories");

            // 3. UserRoles
            var localRoles = await local.UserRoles.AsNoTracking().ToListAsync(ct);
            await SyncEntitiesAsync(localRoles, (x) => x.RoleId, "UserRoles");

            // 4. Users
            var localUsers = await local.Users.AsNoTracking().ToListAsync(ct);
            await SyncEntitiesAsync(localUsers, (x) => x.UserId, "Users");

            // 5. Disasters
            var localDisasters = await local.Disasters.AsNoTracking().ToListAsync(ct);
            await SyncEntitiesAsync(localDisasters, (x) => x.DisasterId, "Disasters");

            // 6. Donors
            var localDonors = await local.Donors.AsNoTracking().ToListAsync(ct);
            await SyncEntitiesAsync(localDonors, (x) => x.DonorId, "Donors");

            // 7. Shelters
            var localShelters = await local.Shelters.AsNoTracking().ToListAsync(ct);
            await SyncEntitiesAsync(localShelters, (x) => x.ShelterId, "Shelters");

            // 8. Relief_Goods
            var localGoods = await local.ReliefGoods.AsNoTracking().ToListAsync(ct);
            await SyncEntitiesAsync(localGoods, (x) => x.RgId, "Relief_Goods");

            // 9. Relief_Goods_Categories (pivot) - insert via raw if needed
            var localPivots = await local.ReliefGoodCategories.AsNoTracking().ToListAsync(ct);
            foreach (var p in localPivots)
            {
                var exists = await remote.ReliefGoodCategories.FindAsync(new object[] { p.RgId, p.CategoryId }, ct);
                if (exists == null)
                {
                    remote.ReliefGoodCategories.Add(p);
                }
            }
            await remote.SaveChangesAsync(ct);

            // 10. Stocks
            var localStocks = await local.Stocks.AsNoTracking().ToListAsync(ct);
            await SyncEntitiesAsync(localStocks, (x) => x.StockId, "Stocks");

            // 11. Evacuees
            var localEvac = await local.Evacuees.AsNoTracking().ToListAsync(ct);
            await SyncEntitiesAsync(localEvac, (x) => x.EvacueeId, "Evacuees");

            // 12. Donations
            var localDonations = await local.Donations.AsNoTracking().ToListAsync(ct);
            await SyncEntitiesAsync(localDonations, (x) => x.DonationId, "Donations");

            // 13. ResourceAllocations
            var localAlloc = await local.ResourceAllocations.AsNoTracking().ToListAsync(ct);
            await SyncEntitiesAsync(localAlloc, (x) => x.AllocationId, "ResourceAllocations");

            // 14. ResourceDistributions
            var localDist = await local.ResourceDistributions.AsNoTracking().ToListAsync(ct);
            await SyncEntitiesAsync(localDist, (x) => x.DistributionId, "ResourceDistributions");

            // NOTE: Shelters goods/transactions tables are not mapped in the current AppDbContext
            // If you add DbSet<ShelterGoodTransaction> and DbSet<ShelterGood> later, restore syncing here.

            // 16. Report tables
            var localR1 = await local.ReportDisasterSummaries.AsNoTracking().ToListAsync(ct);
            await SyncEntitiesAsync(localR1, (x) => x.ReportId, "ReportDisasterSummary");
            var localR2 = await local.ReportResourceDistributions.AsNoTracking().ToListAsync(ct);
            await SyncEntitiesAsync(localR2, (x) => x.ReportId, "ReportResourceDistribution");

            // 18. AuditLogs (append-only)
            var lastRemoteLogId = await remote.AuditLogs.MaxAsync(a => (int?)a.LogId, ct) ?? 0;
            var newLogs = await local.AuditLogs.AsNoTracking().Where(a => a.LogId > lastRemoteLogId).ToListAsync(ct);
            if (newLogs.Count > 0)
            {
                await remote.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [AuditLogs] ON", ct);
                foreach (var log in newLogs)
                {
                    remote.Attach(log);
                    remote.Entry(log).State = EntityState.Added;
                }
                await remote.SaveChangesAsync(ct);
                await remote.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [AuditLogs] OFF", ct);
            }

            await tx.CommitAsync(ct);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Push failed");
            return (false, ex.Message);
        }
    }

    public void StartAutoSync(TimeSpan interval)
    {
        StopAutoSync();
        CurrentInterval = interval;
        _timer = new Timer(async _ =>
        {
            try { await SyncNowAsync(); }
            catch (Exception ex) { _logger.LogError(ex, "Auto-sync error"); }
        }, null, interval, interval);
    }

    public void StopAutoSync()
    {
        CurrentInterval = null;
        _timer?.Dispose();
        _timer = null;
    }
}