namespace Meeting_Of_Minutes.Models
{
    public class InviteHistoryEntryModel
    {
        public int UserInviteID { get; set; }
        public int UserID { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string InviteEmail { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string DeliveryStatus { get; set; } = string.Empty;
        public string DeliveryChannel { get; set; } = string.Empty;
        public string PreviewPath { get; set; } = string.Empty;
        public bool IsUsed { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime Created { get; set; }
        public DateTime? UsedAt { get; set; }
    }
}
