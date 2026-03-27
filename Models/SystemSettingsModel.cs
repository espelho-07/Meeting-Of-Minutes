using System.ComponentModel.DataAnnotations;

namespace Meeting_Of_Minutes.Models
{
    public class SystemSettingsModel
    {
        [Required]
        [StringLength(120)]
        public string PlatformName { get; set; } = "Meeting Of Minutes";

        [Required]
        [EmailAddress]
        public string SupportEmail { get; set; } = string.Empty;

        [StringLength(250)]
        public string MaintenanceBanner { get; set; } = string.Empty;

        public bool AllowAdminSelfRegistration { get; set; }

        [Range(15, 240)]
        public int SessionTimeoutMinutes { get; set; } = 30;

        [StringLength(200)]
        public string SmtpHost { get; set; } = string.Empty;

        [Range(1, 65535)]
        public int SmtpPort { get; set; } = 587;

        [StringLength(150)]
        public string SmtpUsername { get; set; } = string.Empty;

        [StringLength(250)]
        public string SmtpPassword { get; set; } = string.Empty;

        [EmailAddress]
        public string SmtpFromEmail { get; set; } = string.Empty;

        [StringLength(120)]
        public string SmtpFromName { get; set; } = string.Empty;

        public bool SmtpUseSsl { get; set; } = true;

        [StringLength(250)]
        public string InviteBaseUrl { get; set; } = string.Empty;

        public DateTime Modified { get; set; } = DateTime.Now;
        public string ModifiedBy { get; set; } = string.Empty;

        public bool IsSmtpConfigured =>
            !string.IsNullOrWhiteSpace(SmtpHost) &&
            !string.IsNullOrWhiteSpace(SmtpFromEmail);
    }
}
