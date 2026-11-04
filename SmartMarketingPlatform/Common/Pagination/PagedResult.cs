using Microsoft.EntityFrameworkCore;

namespace SmartMarketingPlatform.Common.Pagination;

// Page-based pagination with total count: the shape every list endpoint returns.
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNext => Page < TotalPages;
    public bool HasPrevious => Page > 1;

    // Entities are paged in SQL, then mapped to response DTOs in memory.
    public PagedResult<TOut> Map<TOut>(Func<T, TOut> map) =>
        new(Items.Select(map).ToList(), Page, PageSize, TotalCount);
}

public static class PaginationExtensions
{
    // The query MUST already have a stable OrderBy (e.g. CreatedAt DESC, Id DESC),
    // otherwise rows can repeat or disappear between pages.
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query, PageQuery page, CancellationToken cancellationToken = default)
    {
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page.Page - 1) * page.PageSize)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>(items, page.Page, page.PageSize, total);
    }
}
