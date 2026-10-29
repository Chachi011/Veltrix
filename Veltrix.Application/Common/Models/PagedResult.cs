namespace Veltrix.Application.Common.Models;

/// <summary>Query-string paging: ?page=1&amp;pageSize=20</summary>
public class PageQuery
{
    public const int MaxPageSize = 100;

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Rows to skip, computed from Page and PageSize. A method on purpose: properties of a [FromQuery]
    /// class become query parameters (bindable and listed in Swagger); methods never do.
    /// </summary>
    public int Offset() => (Page - 1) * PageSize;
}

/// <summary>One page of results plus what the UI needs to draw the pager. Not sealed: a page may carry extra totals.</summary>
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
