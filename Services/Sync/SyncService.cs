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

            // Open connection and start a transaction
            await local.Database.OpenConnectionAsync(ct);
            using var tx = await local.Database.BeginTransactionAsync(ct);

            var remoteDisasters = await remote.Disasters.AsNoTracking().ToListAsync(ct);
            var toInsert = new List<Disaster>();

            foreach (var r in remoteDisasters)
            {
                var localEntity = await local.Disasters.FirstOrDefaultAsync(x => x.DisasterId == r.DisasterId, ct);
                if (localEntity == null)
                {
                    toInsert.Add(r);
                }
                else
                {
                    local.Entry(localEntity).CurrentValues.SetValues(r);
                }
            }

            // Save updates first
            if (local.ChangeTracker.HasChanges())
            {
                await local.SaveChangesAsync(ct);
            }

            // Insert new records with IDENTITY_INSERT enabled
            if (toInsert.Count > 0)
            {
                await local.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [Disasters] ON", ct);
                foreach (var disaster in toInsert)
                {
                    local.Disasters.Attach(disaster);
                    local.Entry(disaster).State = EntityState.Added;
                }
                await local.SaveChangesAsync(ct);
                await local.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [Disasters] OFF", ct);
            }

            await tx.CommitAsync(ct);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pull failed");
            return (false, ex.Message);
        }
    }

    // Helper method to compare if two entities have identical values
    private bool AreEntitiesEqual<TEntity>(AppDbContext context, TEntity local, TEntity remote) 
        where TEntity : class
    {
        try
        {
            var entry = context.Entry(remote);
            var currentValues = entry.CurrentValues;
            var proposedValues = entry.CurrentValues.Clone();
            
            proposedValues.SetValues(local);
            
            // Compare all properties
            foreach (var property in currentValues.Properties)
            {
                var currentValue = currentValues[property];
                var proposedValue = proposedValues[property];
                
                // Handle null comparisons
                if (currentValue == null && proposedValue == null)
                    continue;
                    
                if (currentValue == null || proposedValue == null)
                    return false;
                
                // Compare values
                if (!currentValue.Equals(proposedValue))
                    return false;
            }
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error comparing entities, assuming different");
            return false;
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

            try
            {
                // Helper local functions
                async Task SyncEntitiesAsync<TEntity>(IEnumerable<TEntity> localRows, Func<TEntity, object> keySelector, string tableName)
                    where TEntity : class
                {
                    var toInsert = new List<TEntity>();
                    var toUpdate = new List<TEntity>();
                    
                    foreach (var row in localRows)
                    {
                        var key = keySelector(row);
                        var exists = await remote.Set<TEntity>().FindAsync(new object[] { key }, ct);
                        
                        if (exists == null)
                        {
                            toInsert.Add(row);
                        }
                        else
                        {
                            // Check if data is actually different
                            if (!AreEntitiesEqual(remote, row, exists))
                            {
                                remote.Entry(exists).CurrentValues.SetValues(row);
                                toUpdate.Add(exists);
                            }
                            // If equal, skip this record (no update needed)
                        }
                    }

                    // Save updates first (without IDENTITY_INSERT) only if there are changes
                    if (toUpdate.Count > 0 && remote.ChangeTracker.HasChanges())
                    {
                        _logger.LogDebug("Updating {Count} {Entity} records", toUpdate.Count, tableName);
                        await remote.SaveChangesAsync(ct);
                    }

                    if (toInsert.Count > 0)
                    {
                        _logger.LogDebug("Inserting {Count} {Entity} records", toInsert.Count, tableName);
                        
                        // Enable identity insert for this table to preserve keys
                        await remote.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT [{tableName}] ON", ct);
                        
                        try
                        {
                            foreach (var r in toInsert)
                            {
                                remote.Attach(r);
                                remote.Entry(r).State = EntityState.Added;
                            }
                            await remote.SaveChangesAsync(ct);
                        }
                        finally
                        {
                            await remote.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT [{tableName}] OFF", ct);
                        }
                    }
                }

                // Special sync for UserRoles with unique constraint on RoleName
                async Task SyncUserRolesAsync()
                {
                    var localRoles = await local.UserRoles.AsNoTracking().ToListAsync(ct);
                    var updatedCount = 0;
                    var insertedCount = 0;
                    var skippedCount = 0;
                    
                    foreach (var localRole in localRoles)
                    {
                        // Check by RoleName (unique constraint) instead of just RoleId
                        var remoteRole = await remote.UserRoles
                            .FirstOrDefaultAsync(r => r.RoleName == localRole.RoleName, ct);
                        
                        if (remoteRole == null)
                        {
                            // Role doesn't exist remotely - insert with identity insert
                            await remote.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [UserRoles] ON", ct);
                            
                            try
                            {
                                remote.UserRoles.Attach(localRole);
                                remote.Entry(localRole).State = EntityState.Added;
                                await remote.SaveChangesAsync(ct);
                                insertedCount++;
                            }
                            finally
                            {
                                await remote.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [UserRoles] OFF", ct);
                            }
                        }
                        else if (remoteRole.RoleId != localRole.RoleId)
                        {
                            // Role exists but with different ID - check if data is different
                            var needsUpdate = remoteRole.Description != localRole.Description || 
                                            remoteRole.CreatedAt != localRole.CreatedAt;
                            
                            if (needsUpdate)
                            {
                                remoteRole.Description = localRole.Description;
                                remoteRole.CreatedAt = localRole.CreatedAt;
                                remote.Entry(remoteRole).State = EntityState.Modified;
                                await remote.SaveChangesAsync(ct);
                                updatedCount++;
                            }
                            else
                            {
                                skippedCount++;
                            }
                        }
                        else
                        {
                            // Role exists with same ID - check if update needed
                            if (!AreEntitiesEqual(remote, localRole, remoteRole))
                            {
                                remote.Entry(remoteRole).CurrentValues.SetValues(localRole);
                                if (remote.ChangeTracker.HasChanges())
                                {
                                    await remote.SaveChangesAsync(ct);
                                    updatedCount++;
                                }
                            }
                            else
                            {
                                skippedCount++;
                            }
                        }
                    }
                    
                    _logger.LogDebug("UserRoles sync: {Inserted} inserted, {Updated} updated, {Skipped} skipped", 
                        insertedCount, updatedCount, skippedCount);
                }

                // Special sync for Users with unique constraints on Username and Email
                async Task SyncUsersAsync()
                {
                    var localUsers = await local.Users.AsNoTracking().ToListAsync(ct);
                    var updatedCount = 0;
                    var insertedCount = 0;
                    var skippedCount = 0;
                    
                    foreach (var localUser in localUsers)
                    {
                        // Check by Username (unique constraint) first
                        var remoteUser = await remote.Users
                            .FirstOrDefaultAsync(u => u.Username == localUser.Username, ct);
                        
                        if (remoteUser == null)
                        {
                            // Check by Email (also unique constraint) as a fallback
                            remoteUser = await remote.Users
                                .FirstOrDefaultAsync(u => u.Email == localUser.Email, ct);
                        }
                        
                        if (remoteUser == null)
                        {
                            // User doesn't exist remotely - insert with identity insert
                            await remote.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [Users] ON", ct);
                            
                            try
                            {
                                remote.Users.Attach(localUser);
                                remote.Entry(localUser).State = EntityState.Added;
                                await remote.SaveChangesAsync(ct);
                                insertedCount++;
                            }
                            finally
                            {
                                await remote.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [Users] OFF", ct);
                            }
                        }
                        else if (remoteUser.UserId != localUser.UserId)
                        {
                            // User exists but with different ID - update by username/email, don't change ID
                            var needsUpdate = remoteUser.PasswordHash != localUser.PasswordHash ||
                                            remoteUser.Email != localUser.Email ||
                                            remoteUser.Username != localUser.Username ||
                                            remoteUser.RoleId != localUser.RoleId ||
                                            remoteUser.IsActive != localUser.IsActive ||
                                            remoteUser.CreatedAt != localUser.CreatedAt ||
                                            remoteUser.UpdatedAt != localUser.UpdatedAt;
                            
                            if (needsUpdate)
                            {
                                remoteUser.PasswordHash = localUser.PasswordHash;
                                remoteUser.Email = localUser.Email;
                                remoteUser.Username = localUser.Username;
                                remoteUser.RoleId = localUser.RoleId;
                                remoteUser.IsActive = localUser.IsActive;
                                remoteUser.CreatedAt = localUser.CreatedAt;
                                remoteUser.UpdatedAt = localUser.UpdatedAt;
                                remote.Entry(remoteUser).State = EntityState.Modified;
                                await remote.SaveChangesAsync(ct);
                                updatedCount++;
                            }
                            else
                            {
                                skippedCount++;
                            }
                        }
                        else
                        {
                            // User exists with same ID - check if update needed
                            if (!AreEntitiesEqual(remote, localUser, remoteUser))
                            {
                                remote.Entry(remoteUser).CurrentValues.SetValues(localUser);
                                if (remote.ChangeTracker.HasChanges())
                                {
                                    await remote.SaveChangesAsync(ct);
                                    updatedCount++;
                                }
                            }
                            else
                            {
                                skippedCount++;
                            }
                        }
                    }
                    
                    _logger.LogDebug("Users sync: {Inserted} inserted, {Updated} updated, {Skipped} skipped", 
                        insertedCount, updatedCount, skippedCount);
                }

                // Sync order (parents -> children)
                // 1. Category_Types
                var localCategoryTypes = await local.CategoryTypes.AsNoTracking().ToListAsync(ct);
                await SyncEntitiesAsync(localCategoryTypes, (x) => x.CategoryTypeId, "Category_Types");

                // 2. Categories
                var localCategories = await local.Categories.AsNoTracking().ToListAsync(ct);
                await SyncEntitiesAsync(localCategories, (x) => x.CategoryId, "Categories");

                // 3. UserRoles - Use special sync method
                await SyncUserRolesAsync();

                // 4. Users - Use special sync method
                await SyncUsersAsync();

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

                // 9. Relief_Goods_Categories (pivot) - no IDENTITY_INSERT needed
                var localPivots = await local.ReliefGoodCategories.AsNoTracking().ToListAsync(ct);
                var pivotUpdates = 0;
                var pivotInserts = 0;
                var pivotSkips = 0;
                
                foreach (var p in localPivots)
                {
                    var exists = await remote.ReliefGoodCategories.FindAsync(new object[] { p.RgId, p.CategoryId }, ct);
                    if (exists == null)
                    {
                        remote.ReliefGoodCategories.Add(p);
                        pivotInserts++;
                    }
                    else
                    {
                        // Check if data is different (pivot tables usually don't have extra fields, but check anyway)
                        if (!AreEntitiesEqual(remote, p, exists))
                        {
                            remote.Entry(exists).CurrentValues.SetValues(p);
                            pivotUpdates++;
                        }
                        else
                        {
                            pivotSkips++;
                        }
                    }
                }
                
                if (remote.ChangeTracker.HasChanges())
                {
                    await remote.SaveChangesAsync(ct);
                }
                
                _logger.LogDebug("Relief_Goods_Categories sync: {Inserted} inserted, {Updated} updated, {Skipped} skipped", 
                    pivotInserts, pivotUpdates, pivotSkips);

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

                // 18. AuditLogs (append-only) - only insert new logs, never update
                var lastRemoteLogId = await remote.AuditLogs.MaxAsync(a => (int?)a.LogId, ct) ?? 0;
                var newLogs = await local.AuditLogs.AsNoTracking().Where(a => a.LogId > lastRemoteLogId).ToListAsync(ct);
                
                if (newLogs.Count > 0)
                {
                    _logger.LogDebug("Inserting {Count} new audit logs", newLogs.Count);
                    
                    await remote.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [AuditLogs] ON", ct);
                    
                    try
                    {
                        foreach (var log in newLogs)
                        {
                            remote.Attach(log);
                            remote.Entry(log).State = EntityState.Added;
                        }
                        await remote.SaveChangesAsync(ct);
                    }
                    finally
                    {
                        await remote.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [AuditLogs] OFF", ct);
                    }
                }

                await tx.CommitAsync(ct);
                _logger.LogInformation("Push completed successfully");
                return (true, null);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Push failed");
            // Return inner exception message if available for better diagnostics
            var errorMessage = ex.InnerException?.Message ?? ex.Message;
            return (false, errorMessage);
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