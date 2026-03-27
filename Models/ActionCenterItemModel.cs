namespace Meeting_Of_Minutes.Models
{
    public class ActionCenterItemModel
    {
        public string ItemType { get; set; } = string.Empty;

        public int? ItemID { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Meta { get; set; } = string.Empty;

        public DateTime Created { get; set; }

        public string StatusLabel { get; set; } = string.Empty;

        public string PriorityLabel { get; set; } = string.Empty;

        public int PriorityRank { get; set; }

        public bool IsStale24Hours { get; set; }

        public bool IsStale72Hours { get; set; }

        public bool IsCritical { get; set; }

        public string TargetUrl { get; set; } = string.Empty;
    }
}
