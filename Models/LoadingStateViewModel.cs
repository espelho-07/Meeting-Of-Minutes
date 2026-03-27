namespace Meeting_Of_Minutes.Models;

public class LoadingStateViewModel
{
    public string Title { get; set; } = "Loading workspace";
    public string Description { get; set; } = "Preparing the latest data and layout for you.";
    public int Lines { get; set; } = 3;
    public bool Compact { get; set; }
}
