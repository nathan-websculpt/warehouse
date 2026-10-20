namespace Warehouse.Contracts.Paging;

public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    string? NextCursor,
    string? PreviousCursor);
