namespace Firepuma.Scheduling.Domain.Plumbing.Pagination;

public class PaginatedItems<T>
{
    public IEnumerable<T> Items { get; }
    public int PageIndex { get; }
    public int PageSize { get; }
    public long TotalCount { get; }
    public int LastPageIndex { get; }

    public PaginatedItems(
        IEnumerable<T> items,
        int pageIndex,
        int pageSize,
        long totalCount)
    {
        var lastPageIndex = (int)Math.Ceiling(totalCount / (double)pageSize) - 1;

        Items = items;
        PageIndex = pageIndex;
        PageSize = pageSize;
        TotalCount = totalCount;
        LastPageIndex = lastPageIndex;
    }
}