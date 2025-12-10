using System.Linq.Expressions;

namespace ResQLink.Services.Search;

/// <summary>
/// Search criteria builder for advanced filtering
/// </summary>
public class SearchCriteria<T> where T : class
{
    public string? SearchTerm { get; set; }
    public List<SearchFilter> Filters { get; set; } = new();
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class SearchFilter
{
    public string PropertyName { get; set; } = string.Empty;
    public FilterOperator Operator { get; set; }
    public object? Value { get; set; }
    public LogicalOperator LogicalOperator { get; set; } = LogicalOperator.And;
}

public enum FilterOperator
{
    Equals,
    NotEquals,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    Contains,
    StartsWith,
    EndsWith,
    In,
    Between
}

public enum LogicalOperator
{
    And,
    Or
}

/// <summary>
/// Paginated search results
/// </summary>
public class SearchResult<T> where T : class
{
    public List<T> Results { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}

/// <summary>
/// Interface for advanced search service
/// </summary>
public interface ISearchService
{
    Task<SearchResult<T>> SearchAsync<T>(SearchCriteria<T> criteria, IQueryable<T> query) where T : class;
    Task<List<T>> QuickSearchAsync<T>(string searchTerm, IQueryable<T> query, params Expression<Func<T, string>>[] searchProperties) where T : class;
}
