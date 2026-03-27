namespace Meeting_Of_Minutes.Models
{
    public interface IPagedListViewModel
    {
        int PageNumber { get; }
        int PageSize { get; }
        int TotalItems { get; }
        int TotalPages { get; }
        string? SearchText { get; }
        string? SortBy { get; }
        string? SortDirection { get; }
    }
}
