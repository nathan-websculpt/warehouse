using Warehouse.Contracts.Paging;

namespace Warehouse.Api.Infrastructure;


/*
    NOTE: a cursor is a bookmark for where the next request should continue
          
            packages fetched rows into a response and creates the navigation cursors
*/


// temporarily groups the response item with its timestamp and database ID
public sealed record CursorRow<T>(DateTime CreatedAtUtc, int Id, T Item);


public static class CursorPage
{
    // queries fetch one extra row in their travel direction... then return display order
    //      creates NextCursor from last displayed row and PreviousCursor from first displayed row
    //      returns only each row's Item in a PagedResponse: Items, NextCursor, PreviousCursor
    public static PagedResponse<T> Create<T>(List<CursorRow<T>> rows, int pageSize, TransferCursor? cursor, string context)
    {
        var more = rows.Count > pageSize;
        var page = rows.Take(pageSize).ToList();

        if (cursor?.Backwards == true)
            page.Reverse();

        if (page.Count == 0)
            return new([], null, null);

        var hasPrevious = cursor?.Backwards == true ? more : cursor is not null;
        var hasNext = cursor?.Backwards == true || more;

        return new(page.Select(row => row.Item).ToArray(),
            hasNext ? TransferCursor.Encode(page[^1].CreatedAtUtc, page[^1].Id, false, context) : null,
            hasPrevious ? TransferCursor.Encode(page[0].CreatedAtUtc, page[0].Id, true, context) : null);

    } 
}
