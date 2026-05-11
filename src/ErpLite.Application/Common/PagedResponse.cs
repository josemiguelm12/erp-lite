namespace ErpLite.Application.Common;

public sealed record PagedResponse<T>(
    IReadOnlyCollection<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);
