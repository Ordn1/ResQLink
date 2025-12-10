using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;

namespace ResQLink.Services.Search;

/// <summary>
/// Advanced search service with dynamic filtering and pagination
/// </summary>
public class SearchService : ISearchService
{
    public async Task<SearchResult<T>> SearchAsync<T>(SearchCriteria<T> criteria, IQueryable<T> query) where T : class
    {
        // Apply filters
        foreach (var filter in criteria.Filters)
        {
            query = ApplyFilter(query, filter);
        }

        // Apply search term if provided
        if (!string.IsNullOrWhiteSpace(criteria.SearchTerm))
        {
            query = ApplySearchTerm(query, criteria.SearchTerm);
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply sorting
        if (!string.IsNullOrWhiteSpace(criteria.SortBy))
        {
            query = ApplySorting(query, criteria.SortBy, criteria.SortDescending);
        }

        // Apply pagination
        var results = await query
            .Skip((criteria.PageNumber - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .ToListAsync();

        return new SearchResult<T>
        {
            Results = results,
            TotalCount = totalCount,
            PageNumber = criteria.PageNumber,
            PageSize = criteria.PageSize
        };
    }

    public async Task<List<T>> QuickSearchAsync<T>(string searchTerm, IQueryable<T> query, params Expression<Func<T, string>>[] searchProperties) where T : class
    {
        if (string.IsNullOrWhiteSpace(searchTerm) || !searchProperties.Any())
            return await query.ToListAsync();

        Expression<Func<T, bool>>? combinedPredicate = null;

        foreach (var property in searchProperties)
        {
            var parameter = property.Parameters[0];
            var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
            
            if (containsMethod == null)
                continue;

            var searchValue = Expression.Constant(searchTerm, typeof(string));
            var containsExpression = Expression.Call(property.Body, containsMethod, searchValue);
            var lambda = Expression.Lambda<Func<T, bool>>(containsExpression, parameter);

            combinedPredicate = combinedPredicate == null 
                ? lambda 
                : CombinePredicates(combinedPredicate, lambda, LogicalOperator.Or);
        }

        if (combinedPredicate != null)
            query = query.Where(combinedPredicate);

        return await query.ToListAsync();
    }

    private IQueryable<T> ApplyFilter<T>(IQueryable<T> query, SearchFilter filter) where T : class
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        var property = Expression.Property(parameter, filter.PropertyName);
        var constant = Expression.Constant(filter.Value);

        Expression comparison = filter.Operator switch
        {
            FilterOperator.Equals => Expression.Equal(property, constant),
            FilterOperator.NotEquals => Expression.NotEqual(property, constant),
            FilterOperator.GreaterThan => Expression.GreaterThan(property, constant),
            FilterOperator.GreaterThanOrEqual => Expression.GreaterThanOrEqual(property, constant),
            FilterOperator.LessThan => Expression.LessThan(property, constant),
            FilterOperator.LessThanOrEqual => Expression.LessThanOrEqual(property, constant),
            FilterOperator.Contains => Expression.Call(property, "Contains", null, constant),
            FilterOperator.StartsWith => Expression.Call(property, "StartsWith", null, constant),
            FilterOperator.EndsWith => Expression.Call(property, "EndsWith", null, constant),
            _ => throw new NotImplementedException($"Operator {filter.Operator} not implemented")
        };

        var lambda = Expression.Lambda<Func<T, bool>>(comparison, parameter);
        return query.Where(lambda);
    }

    private IQueryable<T> ApplySearchTerm<T>(IQueryable<T> query, string searchTerm) where T : class
    {
        var stringProperties = typeof(T).GetProperties()
            .Where(p => p.PropertyType == typeof(string))
            .ToList();

        if (!stringProperties.Any())
            return query;

        var parameter = Expression.Parameter(typeof(T), "x");
        Expression? combinedExpression = null;

        var toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes);
        var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });

        if (toLowerMethod == null || containsMethod == null)
            return query;

        foreach (var property in stringProperties)
        {
            var propertyExpression = Expression.Property(parameter, property);
            var toLowerExpression = Expression.Call(propertyExpression, toLowerMethod);
            var searchValue = Expression.Constant(searchTerm.ToLower());
            var containsExpression = Expression.Call(toLowerExpression, containsMethod, searchValue);

            combinedExpression = combinedExpression == null
                ? containsExpression
                : Expression.OrElse(combinedExpression, containsExpression);
        }

        if (combinedExpression != null)
        {
            var lambda = Expression.Lambda<Func<T, bool>>(combinedExpression, parameter);
            query = query.Where(lambda);
        }

        return query;
    }

    private IQueryable<T> ApplySorting<T>(IQueryable<T> query, string sortBy, bool descending) where T : class
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        var property = Expression.Property(parameter, sortBy);
        var lambda = Expression.Lambda(property, parameter);

        var methodName = descending ? "OrderByDescending" : "OrderBy";
        var resultExpression = Expression.Call(
            typeof(Queryable),
            methodName,
            new Type[] { typeof(T), property.Type },
            query.Expression,
            Expression.Quote(lambda));

        return query.Provider.CreateQuery<T>(resultExpression);
    }

    private Expression<Func<T, bool>> CombinePredicates<T>(
        Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right,
        LogicalOperator op)
    {
        var parameter = left.Parameters[0];
        var body = op == LogicalOperator.And
            ? Expression.AndAlso(left.Body, Expression.Invoke(right, parameter))
            : Expression.OrElse(left.Body, Expression.Invoke(right, parameter));

        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }
}
