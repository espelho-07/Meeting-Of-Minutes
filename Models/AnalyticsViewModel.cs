namespace Meeting_Of_Minutes.Models
{
    public class AnalyticsViewModel
    {
        public bool IsAdminUser { get; set; }
        public int DaysFilter { get; set; }
        public int TotalMeetings { get; set; }
        public int ScheduledMeetings { get; set; }
        public int CompletedMeetings { get; set; }
        public int CancelledMeetings { get; set; }
        public int AttendanceEntries { get; set; }
        public decimal CompletionRate { get; set; }
        public decimal CancellationRate { get; set; }
        public List<string> TrendLabels { get; set; } = new();
        public List<int> TrendMeetingValues { get; set; } = new();
        public List<int> TrendCancelledValues { get; set; } = new();
        public List<string> TopDepartmentLabels { get; set; } = new();
        public List<int> TopDepartmentValues { get; set; } = new();
        public List<string> TopMeetingTypeLabels { get; set; } = new();
        public List<int> TopMeetingTypeValues { get; set; } = new();
        public List<AuditLogModel> RiskSignals { get; set; } = new();
    }
}
