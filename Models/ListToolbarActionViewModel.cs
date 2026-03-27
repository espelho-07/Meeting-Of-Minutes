namespace Meeting_Of_Minutes.Models
{
    public class ListToolbarActionViewModel
    {
        public string Text { get; set; } = string.Empty;
        public string IconClass { get; set; } = string.Empty;
        public string CssClass { get; set; } = "list-btn-light";
        public string? Action { get; set; }
        public string? Controller { get; set; }
        public string? Url { get; set; }
    }
}
