namespace Meeting_Of_Minutes.Models
{
    public class ImportHistoryEntryModel
    {
        public string ImportHistoryID { get; set; } = Guid.NewGuid().ToString("N");
        public string CompanyName { get; set; } = string.Empty;
        public string ModuleName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public int? UserID { get; set; }
        public int ImportedCount { get; set; }
        public int SkippedCount { get; set; }
        public string ReportPath { get; set; } = string.Empty;
        public DateTime Created { get; set; } = DateTime.Now;
    }
}
