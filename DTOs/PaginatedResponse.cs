namespace EverestRemit.Api.DTOs
{
    public sealed class PaginatedResponse<T>
    {
        public IReadOnlyList<T> Items { get; init; } = [];
        public PaginationMetadata Pagination { get; init; } = new();
    }

    public sealed class PaginationMetadata
    {
        public int PageNumber { get; init; }
        public int PageSize { get; init; }
        public int TotalCount { get; init; }

        public int TotalPages =>
            PageSize > 0
                ? (int)Math.Ceiling((double)TotalCount / PageSize)
                : 0;

        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}
