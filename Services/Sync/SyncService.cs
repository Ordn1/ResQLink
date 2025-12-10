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

    // Allow-list of tables we toggle IDENTITY_INSERT for (prevents CA2100 and injection concerns)
    private static readonly HashSet<string> AllowedIdentityInsertTables = new(StringComparer.OrdinalIgnoreCase)
    {
        "Category_Types",
        "Categories",
        "UserRoles",
        "Users",
        "Disasters",
        "Donors",
        "Shelters",
        "Relief_Goods",
        "Relief_Goods_Categories",
        "Stocks",
        "Evacuees",
        "Donations",
        "ResourceAllocations",
        "ResourceAllocation",
        "ResourceDistributions",
        "ReportDisasterSummary",
        "ReportResourceDistribution",
        "AuditLogs",
        "Suppliers",
        "BarangayBudgets",
        "BarangayBudgetItems"
    };

    private static string GetSafeIdentityInsertTable(string tableName)
    {
        if (!AllowedIdentityInsertTables.Contains(tableName))
            throw new ArgumentException($"Identity insert not allowed for table: {tableName}", nameof(tableName));
        return tableName;
    }

    // Returns [schema].[table] or [table]
    private static string GetMappedTableIdentifier<TEntity>(DbContext context)
        where TEntity : class
    {
        var et = context.Model.FindEntityType(typeof(TEntity))
                 ?? throw new InvalidOperationException($"Entity type not found: {typeof(TEntity).Name}");
        var tableName = et.GetTableName()
                        ?? throw new InvalidOperationException($"No table mapping for: {typeof(TEntity).Name}");
        var schema = et.GetSchema();

        var safeBase = GetSafeIdentityInsertTable(tableName);
        return schema is not null ? $"[{schema}].[{safeBase}]" : $"[{safeBase}]";
    }

    // Split bracketed identifier and check existence on the remote DB
    private static (string? schema, string table) ParseIdentifier(string ident)
    {
        var parts = ident.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                         .Select(p => p.Trim('[', ']')).ToArray();
        if (parts.Length == 2) return (parts[0], parts[1]);
        return (null, parts[0]);
    }

    private static async Task<bool> TableExistsAsync(DbContext ctx, string ident, CancellationToken ct)
    {
        var (schema, table) = ParseIdentifier(ident);

        var sql = schema is null
            ? "SELECT CAST(CASE WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = {0}) THEN 1 ELSE 0 END AS INT) AS [Value]"
            : "SELECT CAST(CASE WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = {0} AND TABLE_NAME = {1}) THEN 1 ELSE 0 END AS INT) AS [Value]";

        if (schema is null)
        {
            var exists = await ctx.Database.SqlQueryRaw<int>(sql, table).SingleAsync(ct);
            return exists == 1;
        }
        else
        {
            var exists = await ctx.Database.SqlQueryRaw<int>(sql, schema, table).SingleAsync(ct);
            return exists == 1;
        }
    }

    private static async Task<bool> HasIdentityColumnAsync(DbContext ctx, string ident, CancellationToken ct)
    {
        var (schema, table) = ParseIdentifier(ident);
        schema ??= "dbo";

        const string sql = @"
SELECT COUNT(*) 
FROM sys.columns c
JOIN sys.objects o ON c.object_id = o.object_id
LEFT JOIN sys.schemas s ON o.schema_id = s.schema_id
WHERE o.type = 'U'
  AND o.name = {0}
  AND (s.name = {1} OR {1} IS NULL)
  AND c.is_identity = 1";

        var count = await ctx.Database.SqlQueryRaw<int>(sql, table, schema).SingleAsync(ct);
        return count > 0;
    }

    /// <summary>
    /// Safely executes SET IDENTITY_INSERT command using validated table identifier.
    /// The table identifier is validated against an allow-list to prevent SQL injection.
    /// </summary>
    private static async Task SetIdentityInsertAsync(
        DbContext context,
        string validatedTableIdent,
        bool enabled,
        CancellationToken ct)
    {
        var onOff = enabled ? "ON" : "OFF";
        
        // Safe: validatedTableIdent comes from GetMappedTableIdentifier which validates against allow-list
        // Table names cannot be parameterized in SQL Server, so we build the command string
        // after validation. The suppression is justified because we validate the table name.
        var sql = $"SET IDENTITY_INSERT {validatedTableIdent} {onOff}";
        
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
        await context.Database.ExecuteSqlRawAsync(sql, ct);
#pragma warning restore CA2100
    }

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

            async Task PullEntitiesAsync<TEntity>(
                IQueryable<TEntity> remoteQuery,
                DbSet<TEntity> localSet,
                Func<TEntity, object> keySelector)
                where TEntity : class
            {
                var remoteRows = await remoteQuery.AsNoTracking().ToListAsync(ct);
                if (remoteRows.Count == 0) return;

                var toInsert = new List<TEntity>();
                var toUpdate = new List<(TEntity localEntity, TEntity remoteEntity)>();

                foreach (var r in remoteRows)
                {
                    var key = keySelector(r);
                    var localEntity = await localSet.FindAsync(new object[] { key }, ct);
                    if (localEntity is null)
                    {
                        toInsert.Add(r);
                    }
                    else
                    {
                        if (!AreEntitiesEqual(local, localEntity, r))
                        {
                            toUpdate.Add((localEntity, r));
                        }
                    }
                }

                foreach (var (localEntity, remoteEntity) in toUpdate)
                {
                    local.Entry(localEntity).CurrentValues.SetValues(remoteEntity);
                }
                if (local.ChangeTracker.HasChanges())
                {
                    await local.SaveChangesAsync(ct);
                }

                if (toInsert.Count > 0)
                {
                    var tableIdent = GetMappedTableIdentifier<TEntity>(local);
                    var isIdentityEntity = EntityHasIdentityKey<TEntity>(local);

                    static TDestEntity CreateScalarOnlyCopy<TDestEntity>(DbContext ctx, TDestEntity source) where TDestEntity : class
                    {
                        var et = ctx.Model.FindEntityType(typeof(TDestEntity))!;
                        var dest = Activator.CreateInstance<TDestEntity>()!;
                        var props = et.GetProperties();

                        foreach (var p in props)
                        {
                            var clrProp = p.PropertyInfo;
                            if (clrProp is null) continue;
                            var value = clrProp.GetValue(source);
                            clrProp.SetValue(dest, value);
                        }

                        return dest;
                    }

                    var scalarOnlyRows = new List<TEntity>();
                    foreach (var r in toInsert)
                    {
                        var copy = CreateScalarOnlyCopy(local, r);
                        scalarOnlyRows.Add(copy);
                    }

                    void AttachForInsertWithoutCascade(TEntity entity, DbContext ctx)
                    {
                        ctx.Attach(entity);
                        ctx.Entry(entity).State = EntityState.Added;
                        foreach (var nav in ctx.Entry(entity).Navigations)
                        {
                            if (nav.IsLoaded && nav.CurrentValue != null)
                            {
                                if (nav.Metadata.IsCollection)
                                {
                                    foreach (var child in (IEnumerable<object>)nav.CurrentValue)
                                    {
                                        ctx.Attach(child);
                                        ctx.Entry(child).State = EntityState.Unchanged;
                                    }
                                }
                                else
                                {
                                    ctx.Attach(nav.CurrentValue);
                                    ctx.Entry(nav.CurrentValue).State = EntityState.Unchanged;
                                }
                            }
                        }
                    }

                    await local.Database.OpenConnectionAsync(ct);

                    if (!isIdentityEntity)
                    {
                        foreach (var r in scalarOnlyRows)
                        {
                            AttachForInsertWithoutCascade(r, local);
                        }
                        await local.SaveChangesAsync(ct);
                    }
                    else
                    {
                        await SetIdentityInsertAsync(local, tableIdent, true, ct);
                        try
                        {
                            foreach (var r in scalarOnlyRows)
                            {
                                AttachForInsertWithoutCascade(r, local);
                            }
                            await local.SaveChangesAsync(ct);
                        }
                        finally
                        {
                            await SetIdentityInsertAsync(local, tableIdent, false, ct);
                        }
                    }
                }
            }

            await PullEntitiesAsync(remote.CategoryTypes, local.CategoryTypes, x => x.CategoryTypeId);
            await PullEntitiesAsync(remote.Categories, local.Categories, x => x.CategoryId);
            await PullEntitiesAsync(remote.UserRoles, local.UserRoles, x => x.RoleId);
            await PullEntitiesAsync(remote.Users, local.Users, x => x.UserId);
            await PullEntitiesAsync(remote.Disasters, local.Disasters, x => x.DisasterId);
            await PullEntitiesAsync(remote.Donors, local.Donors, x => x.DonorId);
            await PullEntitiesAsync(remote.Shelters, local.Shelters, x => x.ShelterId);
            await PullEntitiesAsync(remote.ReliefGoods, local.ReliefGoods, x => x.RgId);
            await PullEntitiesAsync(remote.Stocks, local.Stocks, x => x.StockId);
            await PullEntitiesAsync(remote.Evacuees, local.Evacuees, x => x.EvacueeId);
            await PullEntitiesAsync(remote.Donations, local.Donations, x => x.DonationId);
            await PullEntitiesAsync(remote.ResourceAllocations, local.ResourceAllocations, x => x.AllocationId);
            await PullEntitiesAsync(remote.ResourceDistributions, local.ResourceDistributions, x => x.DistributionId);
            await PullEntitiesAsync(remote.ReportDisasterSummaries, local.ReportDisasterSummaries, x => x.ReportId);
            await PullEntitiesAsync(remote.ReportResourceDistributions, local.ReportResourceDistributions, x => x.ReportId);
            await PullEntitiesAsync(remote.AuditLogs, local.AuditLogs, x => x.AuditLogId);

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pull failed");
            return (false, ex.InnerException?.Message ?? ex.Message);
        }
    }

    private bool AreEntitiesEqual<TEntity>(AppDbContext context, TEntity local, TEntity remote) 
        where TEntity : class
    {
        try
        {
            var entry = context.Entry(remote);
            var currentValues = entry.CurrentValues;
            var proposedValues = entry.CurrentValues.Clone();
            
            proposedValues.SetValues(local);
            
            foreach (var property in currentValues.Properties)
            {
                var currentValue = currentValues[property];
                var proposedValue = proposedValues[property];
                
                if (currentValue == null && proposedValue == null)
                    continue;
                    
                if (currentValue == null || proposedValue == null)
                    return false;
                
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

    public async Task<(bool ok, string? error)> PushAsync(CancellationToken ct = default)
    {
        try
        {
            using var remote = CreateRemoteContext();
            await using var local = await _localFactory.CreateDbContextAsync(ct);

            await remote.Database.OpenConnectionAsync(ct);
            using var tx = await remote.Database.BeginTransactionAsync(ct);

            try
            {
                async Task SyncEntitiesAsync<TEntity>(IEnumerable<TEntity> localRows, Func<TEntity, object> keySelector)
                    where TEntity : class
                {
                    var toInsert = new List<TEntity>();
                    var toUpdate = new List<TEntity>();
                    var tableIdent = GetMappedTableIdentifier<TEntity>(remote);
                    
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
                            if (!AreEntitiesEqual(remote, row, exists))
                            {
                                remote.Entry(exists).CurrentValues.SetValues(row);
                                toUpdate.Add(exists);
                            }
                        }
                    }

                    if (toUpdate.Count > 0 && remote.ChangeTracker.HasChanges())
                    {
                        _logger.LogDebug("Updating {Count} {Entity} records", toUpdate.Count, tableIdent);
                        await remote.SaveChangesAsync(ct);
                    }

                    if (toInsert.Count > 0)
                    {
                        static bool HasExplicitIdentity(object keyObj)
                        {
                            return keyObj switch
                            {
                                int i => i > 0,
                                long l => l > 0,
                                short s => s > 0,
                                _ => keyObj is not null
                            };
                        }

                        var withExplicitKey = new List<TEntity>();
                        var withoutKey = new List<TEntity>();

                        foreach (var r in toInsert)
                        {
                            var key = keySelector(r);
                            if (HasExplicitIdentity(key))
                                withExplicitKey.Add(r);
                            else
                                withoutKey.Add(r);
                        }

                        if (withoutKey.Count > 0)
                        {
                            _logger.LogDebug("Inserting {Count} {Entity} rows with generated identity", withoutKey.Count, tableIdent);
                            foreach (var r in withoutKey)
                            {
                                remote.Attach(r);
                                remote.Entry(r).State = EntityState.Added;
                            }
                            await remote.SaveChangesAsync(ct);
                        }

                        if (withExplicitKey.Count > 0)
                        {
                            _logger.LogDebug("Inserting {Count} {Entity} rows with explicit identity", withExplicitKey.Count, tableIdent);
                            await SetIdentityInsertAsync(remote, tableIdent, true, ct);
                            try
                            {
                                foreach (var r in withExplicitKey)
                                {
                                    remote.Attach(r);
                                    remote.Entry(r).State = EntityState.Added;
                                }
                                await remote.SaveChangesAsync(ct);
                            }
                            finally
                            {
                                await SetIdentityInsertAsync(remote, tableIdent, false, ct);
                            }
                        }
                    }
                }

                async Task SyncUserRolesAsync()
                {
                    var tableIdent = GetMappedTableIdentifier<UserRole>(remote);
                    var localRoles = await local.UserRoles.AsNoTracking().ToListAsync(ct);
                    var updatedCount = 0;
                    var insertedCount = 0;
                    var skippedCount = 0;
                    
                    foreach (var localRole in localRoles)
                    {
                        var remoteRole = await remote.UserRoles
                            .FirstOrDefaultAsync(r => r.RoleName == localRole.RoleName, ct);
                        
                        if (remoteRole == null)
                        {
                            await SetIdentityInsertAsync(remote, tableIdent, true, ct);
                            try
                            {
                                remote.UserRoles.Attach(localRole);
                                remote.Entry(localRole).State = EntityState.Added;
                                await remote.SaveChangesAsync(ct);
                                insertedCount++;
                            }
                            finally
                            {
                                await SetIdentityInsertAsync(remote, tableIdent, false, ct);
                            }
                        }
                        else if (remoteRole.RoleId != localRole.RoleId)
                        {
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

                async Task SyncUsersAsync()
                {
                    var tableIdent = GetMappedTableIdentifier<User>(remote);
                    var localUsers = await local.Users.AsNoTracking().ToListAsync(ct);
                    var updatedCount = 0;
                    var insertedCount = 0;
                    var skippedCount = 0;
					
                    foreach (var localUser in localUsers)
                    {
                        var remoteUser = await remote.Users
                            .FirstOrDefaultAsync(u => u.Username == localUser.Username, ct)
                            ?? await remote.Users.FirstOrDefaultAsync(u => u.Email == localUser.Email, ct);
						
                        if (remoteUser == null)
                        {
                            await SetIdentityInsertAsync(remote, tableIdent, true, ct);
                            try
                            {
                                remote.Users.Attach(localUser);
                                remote.Entry(localUser).State = EntityState.Added;
                                await remote.SaveChangesAsync(ct);
                                insertedCount++;
                            }
                            finally
                            {
                                await SetIdentityInsertAsync(remote, tableIdent, false, ct);
                            }
                        }
                        else if (remoteUser.UserId != localUser.UserId)
                        {
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

                async Task SyncStocksAsync()
                {
                    var tableIdent = GetMappedTableIdentifier<Stock>(remote);
                    var localStocks = await local.Stocks.AsNoTracking().ToListAsync(ct);
                    var inserted = 0;
                    var updated = 0;

                    foreach (var ls in localStocks)
                    {
                        var exists = await remote.Stocks.AnyAsync(s => s.StockId == ls.StockId, ct);

                        if (!exists)
                        {
                            await SetIdentityInsertAsync(remote, tableIdent, true, ct);
                            try
                            {
                                remote.Stocks.Attach(ls);
                                remote.Entry(ls).State = EntityState.Added;
                                await remote.SaveChangesAsync(ct);
                                inserted++;
                            }
                            finally
                            {
                                await SetIdentityInsertAsync(remote, tableIdent, false, ct);
                            }
                        }
                        else
                        {
                            var stub = new Stock { StockId = ls.StockId };
                            remote.Stocks.Attach(stub);
                            remote.Entry(stub).CurrentValues.SetValues(ls);
                            remote.Entry(stub).State = EntityState.Modified;
                            await remote.SaveChangesAsync(ct);
                            updated++;
                        }
                    }

                    _logger.LogDebug("Stocks sync: {Inserted} inserted, {Updated} updated", inserted, updated);
                }

                var localCategoryTypes = await local.CategoryTypes.AsNoTracking().ToListAsync(ct);
                await SyncEntitiesAsync(localCategoryTypes, x => x.CategoryTypeId);

                var localCategories = await local.Categories.AsNoTracking().ToListAsync(ct);
                await SyncEntitiesAsync(localCategories, x => x.CategoryId);

                await SyncUserRolesAsync();
                await SyncUsersAsync();

                var localDisasters = await local.Disasters.AsNoTracking().ToListAsync(ct);
                await SyncEntitiesAsync(localDisasters, x => x.DisasterId);

                var localDonors = await local.Donors.AsNoTracking().ToListAsync(ct);
                await SyncEntitiesAsync(localDonors, x => x.DonorId);

                var localShelters = await local.Shelters.AsNoTracking().ToListAsync(ct);
                await SyncEntitiesAsync(localShelters, x => x.ShelterId);

                var localGoods = await local.ReliefGoods.AsNoTracking().ToListAsync(ct);
                await SyncEntitiesAsync(localGoods, x => x.RgId);

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

                await SyncStocksAsync();

                var localEvac = await local.Evacuees.AsNoTracking().ToListAsync(ct);
                await SyncEntitiesAsync(localEvac, x => x.EvacueeId);

                var localDonations = await local.Donations.AsNoTracking().ToListAsync(ct);
                await SyncEntitiesAsync(localDonations, x => x.DonationId);

                var raIdent = GetMappedTableIdentifier<ResourceAllocation>(remote);
                if (await TableExistsAsync(remote, raIdent, ct))
                {
                    var localAlloc = await local.ResourceAllocations.AsNoTracking().ToListAsync(ct);
                    await SyncEntitiesAsync(localAlloc, x => x.AllocationId);
                }
                else
                {
                    _logger.LogWarning("Skipping ResourceAllocations sync: remote table {Table} does not exist.", raIdent);
                }

                var rdIdent = GetMappedTableIdentifier<ResourceDistribution>(remote);
                if (await TableExistsAsync(remote, rdIdent, ct))
                {
                    var localDist = await local.ResourceDistributions.AsNoTracking().ToListAsync(ct);
                    await SyncEntitiesAsync(localDist, x => x.DistributionId);
                }
                else
                {
                    _logger.LogWarning("Skipping ResourceDistributions sync: remote table {Table} does not exist.", rdIdent);
                }

                var localR1 = await local.ReportDisasterSummaries.AsNoTracking().ToListAsync(ct);
                await SyncEntitiesAsync(localR1, x => x.ReportId);
                var localR2 = await local.ReportResourceDistributions.AsNoTracking().ToListAsync(ct);
                await SyncEntitiesAsync(localR2, x => x.ReportId);

                var lastRemoteLogId = await remote.AuditLogs.MaxAsync(a => (int?)a.AuditLogId, ct) ?? 0;
                var newLogs = await local.AuditLogs.AsNoTracking().Where(a => a.AuditLogId > lastRemoteLogId).ToListAsync(ct);

                if (newLogs.Count > 0)
                {
                    var auditTableIdent = GetMappedTableIdentifier<AuditLog>(remote);
                    _logger.LogDebug("Inserting {Count} new audit logs", newLogs.Count);
                    await SetIdentityInsertAsync(remote, auditTableIdent, true, ct);
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
                        await SetIdentityInsertAsync(remote, auditTableIdent, false, ct);
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

    private static bool EntityHasIdentityKey<TEntity>(DbContext ctx) where TEntity : class
    {
        var et = ctx.Model.FindEntityType(typeof(TEntity))
                 ?? throw new InvalidOperationException($"Entity type not found: {typeof(TEntity).Name}");
        var pk = et.FindPrimaryKey() ?? throw new InvalidOperationException($"Primary key not found for: {typeof(TEntity).Name}");
        return pk.Properties.Any(p => p.ValueGenerated == Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAdd);
    }
}