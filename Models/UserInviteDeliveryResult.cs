namespace Meeting_Of_Minutes.Models
{
    public class UserInviteDeliveryResult
    {
        public int UserInviteID { get; set; }
        public string InviteToken { get; set; } = string.Empty;
        public string InviteLink { get; set; } = string.Empty;
        public string DeliveryStatus { get; set; } = string.Empty;
        public string DeliveryChannel { get; set; } = string.Empty;
        public string PreviewPath { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
}
