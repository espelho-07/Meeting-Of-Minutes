namespace Meeting_Of_Minutes.Models
{
    public class PlatformHealthViewModel
    {
        public bool DatabaseHealthy { get; set; }
        public string DatabaseMessage { get; set; } = string.Empty;
        public bool InviteDeliveryConfigured { get; set; }
        public string InviteDeliveryMessage { get; set; } = string.Empty;
        public bool FileStorageHealthy { get; set; }
        public string FileStorageMessage { get; set; } = string.Empty;
        public int TotalCompanies { get; set; }
        public int TotalUsers { get; set; }
        public int ActiveInvites { get; set; }
        public int ExpiredInvites { get; set; }
        public int TemporaryPasswordUsers { get; set; }
        public int InactiveAdmins { get; set; }
        public List<string> OperationalFlags { get; set; } = new();
    }
}
