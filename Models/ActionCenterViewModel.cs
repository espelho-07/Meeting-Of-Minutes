namespace Meeting_Of_Minutes.Models
{
    public class ActionCenterViewModel
    {
        public bool IsAdminUser { get; set; }

        public List<ProfileUpdateRequestModel> PendingProfileRequests { get; set; } = new List<ProfileUpdateRequestModel>();

        public List<StaffTransferRequestModel> PendingTransferRequests { get; set; } = new List<StaffTransferRequestModel>();

        public List<AuditLogModel> CriticalActivity { get; set; } = new List<AuditLogModel>();

        public List<AuditLogModel> RecentActivity { get; set; } = new List<AuditLogModel>();

        public List<ActionCenterItemModel> PriorityQueue { get; set; } = new List<ActionCenterItemModel>();

        public List<ActionCenterItemModel> FilteredPriorityQueue { get; set; } = new List<ActionCenterItemModel>();

        public int ActionCount { get; set; }

        public int Overdue24Count { get; set; }

        public int Overdue72Count { get; set; }

        public string CurrentScope { get; set; } = "all";

        public string SearchText { get; set; } = string.Empty;
    }
}
