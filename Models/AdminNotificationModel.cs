namespace Meeting_Of_Minutes.Models
{
    public class AdminNotificationModel
    {
        public int AdminNotificationID { get; set; }

        public string CompanyName { get; set; } = string.Empty;

        public string NotificationType { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public int? RelatedUserID { get; set; }

        public bool IsRead { get; set; }

        public DateTime Created { get; set; }
    }
}
