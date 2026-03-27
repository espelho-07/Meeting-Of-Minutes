using Meeting_Of_Minutes.Models;
using Microsoft.Data.SqlClient;

namespace Meeting_Of_Minutes.Services
{
    public static class PlatformHealthService
    {
        public static PlatformHealthViewModel Build()
        {
            PlatformHealthViewModel model = new PlatformHealthViewModel();
            SystemSettingsModel settings = SystemSettingsService.GetSettings();

            model.DatabaseHealthy = CheckDatabase(out string dbMessage, model);
            model.DatabaseMessage = dbMessage;

            model.InviteDeliveryConfigured = !string.IsNullOrWhiteSpace(settings.SmtpHost) && !string.IsNullOrWhiteSpace(settings.SmtpFromEmail);
            model.InviteDeliveryMessage = model.InviteDeliveryConfigured
                ? $"SMTP is configured for {settings.SmtpHost}:{settings.SmtpPort}."
                : "SMTP is not configured. Invite delivery is using HTML preview fallback.";

            string previewDirectory = Path.Combine(AppContext.BaseDirectory, "App_Data", "mail-preview");
            model.FileStorageHealthy = EnsureDirectory(previewDirectory, out string fileStorageMessage);
            model.FileStorageMessage = fileStorageMessage;

            if (!model.InviteDeliveryConfigured)
            {
                model.OperationalFlags.Add("SMTP delivery is disabled; onboarding relies on preview files.");
            }

            if (model.ExpiredInvites > 0)
            {
                model.OperationalFlags.Add($"{model.ExpiredInvites} onboarding invites have expired and may need resend.");
            }

            if (model.TemporaryPasswordUsers > 0)
            {
                model.OperationalFlags.Add($"{model.TemporaryPasswordUsers} users are still on temporary credentials.");
            }

            if (model.InactiveAdmins > 0)
            {
                model.OperationalFlags.Add($"{model.InactiveAdmins} admin accounts are inactive and should be reviewed.");
            }

            return model;
        }

        private static bool CheckDatabase(out string message, PlatformHealthViewModel model)
        {
            try
            {
                using SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
                con.Open();

                using SqlCommand cmd = new SqlCommand(@"
                    SELECT
                        (SELECT COUNT(DISTINCT CompanyName) FROM MST_User WHERE CompanyName IS NOT NULL AND LTRIM(RTRIM(CompanyName)) <> '') AS TotalCompanies,
                        (SELECT COUNT(*) FROM MST_User) AS TotalUsers,
                        (SELECT COUNT(*) FROM MST_UserInvite WHERE IsUsed = 0 AND ExpiresAt >= GETDATE()) AS ActiveInvites,
                        (SELECT COUNT(*) FROM MST_UserInvite WHERE IsUsed = 0 AND ExpiresAt < GETDATE()) AS ExpiredInvites,
                        (SELECT COUNT(*) FROM MST_User WHERE IsAutoPassword = 1 AND IsActive = 1) AS TemporaryPasswordUsers,
                        (SELECT COUNT(*) FROM MST_User WHERE UserRole IN ('Admin','SuperAdmin') AND IsActive = 0) AS InactiveAdmins", con);

                using SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    model.TotalCompanies = Convert.ToInt32(reader["TotalCompanies"]);
                    model.TotalUsers = Convert.ToInt32(reader["TotalUsers"]);
                    model.ActiveInvites = Convert.ToInt32(reader["ActiveInvites"]);
                    model.ExpiredInvites = Convert.ToInt32(reader["ExpiredInvites"]);
                    model.TemporaryPasswordUsers = Convert.ToInt32(reader["TemporaryPasswordUsers"]);
                    model.InactiveAdmins = Convert.ToInt32(reader["InactiveAdmins"]);
                }

                message = "Database connection and core platform tables are healthy.";
                return true;
            }
            catch (Exception ex)
            {
                message = $"Database health check failed: {ex.Message}";
                return false;
            }
        }

        private static bool EnsureDirectory(string path, out string message)
        {
            try
            {
                Directory.CreateDirectory(path);
                string probePath = Path.Combine(path, ".healthcheck");
                File.WriteAllText(probePath, DateTime.Now.ToString("O"));
                File.Delete(probePath);
                message = $"Filesystem access is healthy for {path}.";
                return true;
            }
            catch (Exception ex)
            {
                message = $"Filesystem health check failed: {ex.Message}";
                return false;
            }
        }
    }
}
