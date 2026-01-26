namespace BlogApi.Application.Common.Models;

public class CursorPagedList<T>
{
    public List<T> Items { get; }
    public string? NextCursor { get; }
    public bool HasNextPage { get; }

    public CursorPagedList(List<T> items, string? nextCursor, bool hasNextPage)
    {
        Items = items;
        NextCursor = nextCursor;
        HasNextPage = hasNextPage;
    }
}
