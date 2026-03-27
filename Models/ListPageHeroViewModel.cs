namespace Meeting_Of_Minutes.Models
{
    public class ListPageHeroViewModel
    {
        public string IconClass { get; set; } = "bi bi-grid";
        public string Kicker { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string NoteLabel { get; set; } = string.Empty;
        public string NoteTitle { get; set; } = string.Empty;
        public List<string> Notes { get; set; } = new();
    }
}
