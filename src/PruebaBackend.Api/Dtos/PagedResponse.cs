namespace PruebaBackend.Api.Dtos;

/// <summary>Response contract for a paginated collection.</summary>
public record PagedResponse<T>(
    IReadOnlyList<T> Data,
    int CurrentPage,
    int PageSize,
    int TotalItems,
    int TotalPages);
