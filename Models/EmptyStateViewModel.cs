namespace Meeting_Of_Minutes.Models;

public class EmptyStateViewModel
{
    public string IconClass { get; set; } = "bi bi-inbox";
    public string Title { get; set; } = "Nothing here yet";
    public string Description { get; set; } = "There is no data to show right now.";
    public string? ActionText { get; set; }
    public string? ActionUrl { get; set; }
    public string Tone { get; set; } = "neutral";
    public bool Compact { get; set; }
}
