namespace Meeting_Of_Minutes.Models
{
    public class PagedListViewModel<T> : IPagedListViewModel
    {
        public List<T> Items { get; set; } = new();
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalItems { get; set; }
        public int TotalPages => PageSize <= 0 ? 1 : (int)Math.Ceiling(TotalItems / (double)PageSize);
        public string? SearchText { get; set; }
        public string? SortBy { get; set; }
        public string? SortDirection { get; set; }
    }
}
