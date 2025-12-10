using Microsoft.EntityFrameworkCore;
using ResQLink.Data;
using ResQLink.Services.Caching;
using System.Linq.Expressions;

namespace ResQLink.Services.Repositories;

/// <summary>
/// Enhanced repository with caching support
/// Note: This is an optional enhancement. Use EfRepository directly if caching is not needed.
/// </summary>
public class CachedRepository<TEntity> where TEntity : class
{
    private readonly AppDbContext _context;
    private readonly ICacheService _cache;
    private readonly string _cachePrefix;
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(10);

    public CachedRepository(AppDbContext context, ICacheService cache)
    {
        _context = context;
        _cache = cache;
        _cachePrefix = $"{typeof(TEntity).Name}_";
    }

    public async Task<TEntity?> GetByIdAsync(object id, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{_cachePrefix}{id}";
        var cached = await _cache.GetAsync<TEntity>(cacheKey, cancellationToken);
        
        if (cached != null)
            return cached;

        var entity = await _context.Set<TEntity>().FindAsync(new object[] { id }, cancellationToken);
        
        if (entity != null)
            await _cache.SetAsync(cacheKey, entity, CacheExpiration, cancellationToken);

        return entity;
    }

    public async Task<List<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{_cachePrefix}All";
        var cached = await _cache.GetAsync<List<TEntity>>(cacheKey, cancellationToken);
        
        if (cached != null)
            return cached;

        var entities = await _context.Set<TEntity>().AsNoTracking().ToListAsync(cancellationToken);
        await _cache.SetAsync(cacheKey, entities, CacheExpiration, cancellationToken);

        return entities;
    }

    public async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await _context.Set<TEntity>().AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        await InvalidateCacheAsync();
        return entity;
    }

    public async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        _context.Set<TEntity>().Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
        await InvalidateCacheAsync();
    }

    public async Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        _context.Set<TEntity>().Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        await InvalidateCacheAsync();
    }

    private async Task InvalidateCacheAsync()
    {
        await _cache.RemoveByPrefixAsync(_cachePrefix);
    }
}
