namespace Meeting_Of_Minutes.Models
{
    public class AppShellViewModel
    {
        public string CurrentController { get; set; } = string.Empty;
        public string CurrentAction { get; set; } = string.Empty;
        public bool IsAdminUser { get; set; }
        public bool IsSuperAdminUser { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
        public int UnreadNotificationCount { get; set; }
        public int OpenActionCount { get; set; }
        public int CriticalActionCount { get; set; }
        public List<AdminNotificationModel> Notifications { get; set; } = new();
        public ActionCenterSummaryModel ActionCenterSummary { get; set; } = new();
        public SystemSettingsModel PlatformSettings { get; set; } = new();
    }
}
