namespace GatumsGourmet.Application.Common;

public record PaginatedResponse<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
