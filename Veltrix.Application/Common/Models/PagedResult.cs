namespace Veltrix.Application.Common.Models;

/// <summary>Query-string paging: ?page=1&amp;pageSize=20</summary>
public class PageQuery
{
    public const int MaxPageSize = 100;

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public int Skip => (Page - 1) * PageSize;
}

/// <summary>One page of results plus what the UI needs to draw the pager.</summary>
public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
