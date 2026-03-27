namespace Meeting_Of_Minutes.Models
{
    public class AuditLogModel
    {
        public int AuditLogID { get; set; }

        public string CompanyName { get; set; } = string.Empty;

        public int? UserID { get; set; }

        public string UserName { get; set; } = string.Empty;

        public string UserRole { get; set; } = string.Empty;

        public string ActionType { get; set; } = string.Empty;

        public string EntityName { get; set; } = string.Empty;

        public string EntityID { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateTime Created { get; set; }
    }
}
