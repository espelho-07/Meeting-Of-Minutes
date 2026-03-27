namespace Meeting_Of_Minutes.Models
{
    public class ActionCenterSummaryModel
    {
        public int OpenActionCount { get; set; }

        public int CriticalActionCount { get; set; }

        public int Overdue24Count { get; set; }

        public int Overdue72Count { get; set; }

        public List<ActionCenterItemModel> PreviewItems { get; set; } = new List<ActionCenterItemModel>();
    }
}
