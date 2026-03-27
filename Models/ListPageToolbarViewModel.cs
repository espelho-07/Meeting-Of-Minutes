namespace Meeting_Of_Minutes.Models
{
    public class ListPageToolbarViewModel
    {
        public string Heading { get; set; } = string.Empty;
        public string Meta { get; set; } = string.Empty;
        public string SearchAction { get; set; } = string.Empty;
        public string SearchController { get; set; } = string.Empty;
        public string SearchPlaceholder { get; set; } = string.Empty;
        public string? SearchText { get; set; }
        public string StatusText { get; set; } = string.Empty;
        public List<ListToolbarActionViewModel> Actions { get; set; } = new();
        public string? TemplateAction { get; set; }
        public string? TemplateController { get; set; }
        public string? ImportAction { get; set; }
        public string? ImportController { get; set; }
        public string ImportInputName { get; set; } = "excelFile";
    }
}
