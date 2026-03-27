namespace Meeting_Of_Minutes.Models
{
    public class ImportReportRowModel
    {
        public int RowNumber { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string DataSummary { get; set; } = string.Empty;
    }
}
