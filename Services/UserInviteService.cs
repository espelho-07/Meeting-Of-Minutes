using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using Meeting_Of_Minutes.Models;
using Meeting_Of_Minutes.Security;
using Microsoft.Data.SqlClient;

namespace Meeting_Of_Minutes.Services
{
    public static class UserInviteService
    {
        private static readonly string MailPreviewDirectory = Path.Combine(AppContext.BaseDirectory, "App_Data", "mail-preview");

        public static UserInviteDeliveryResult CreateAndDeliverInvite(int userId, string userName, string email, string companyName, string userRole, string createdBy)
        {
            SystemSettingsModel settings = SystemSettingsService.GetSettings();
            string token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            DateTime expiresAt = DateTime.Now.AddDays(7);
            string baseUrl = ResolveBaseUrl(settings);
            string inviteLink = $"{baseUrl.TrimEnd('/')}/Auth/AcceptInvite?token={token}";

            using SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            con.Open();

            ExpireOutstandingInvites(con, userId);

            int inviteId;
            using (SqlCommand cmd = new SqlCommand(@"
                INSERT INTO MST_UserInvite
                (UserID, InviteEmail, InviteToken, ExpiresAt, IsUsed, DeliveryStatus, DeliveryChannel, Created, Modified)
                VALUES
                (@UserID, @InviteEmail, @InviteToken, @ExpiresAt, 0, @DeliveryStatus, @DeliveryChannel, GETDATE(), GETDATE());
                SELECT CAST(SCOPE_IDENTITY() AS INT);", con))
            {
                cmd.Parameters.AddWithValue("@UserID", userId);
                cmd.Parameters.AddWithValue("@InviteEmail", email);
                cmd.Parameters.AddWithValue("@InviteToken", token);
                cmd.Parameters.AddWithValue("@ExpiresAt", expiresAt);
                cmd.Parameters.AddWithValue("@DeliveryStatus", "Pending");
                cmd.Parameters.AddWithValue("@DeliveryChannel", "Invite");
                inviteId = Convert.ToInt32(cmd.ExecuteScalar());
            }

            string subject = $"{settings.PlatformName}: set up your account";
            string body = BuildInviteHtml(settings, userName, companyName, userRole, inviteLink, expiresAt);

            string deliveryStatus;
            string deliveryChannel;
            string previewPath = string.Empty;

            if (CanSendEmail(settings))
            {
                try
                {
                    SendEmail(settings, email, userName, subject, body);
                    deliveryStatus = "Sent";
                    deliveryChannel = "SMTP";
                }
                catch
                {
                    previewPath = SavePreview(email, subject, body);
                    deliveryStatus = "PreviewGenerated";
                    deliveryChannel = "Preview";
                }
            }
            else
            {
                previewPath = SavePreview(email, subject, body);
                deliveryStatus = "PreviewGenerated";
                deliveryChannel = "Preview";
            }

            using (SqlCommand updateCmd = new SqlCommand(@"
                UPDATE MST_UserInvite
                SET DeliveryStatus = @DeliveryStatus,
                    DeliveryChannel = @DeliveryChannel,
                    PreviewPath = @PreviewPath,
                    SentAt = GETDATE(),
                    Modified = GETDATE()
                WHERE UserInviteID = @UserInviteID", con))
            {
                updateCmd.Parameters.AddWithValue("@DeliveryStatus", deliveryStatus);
                updateCmd.Parameters.AddWithValue("@DeliveryChannel", deliveryChannel);
                updateCmd.Parameters.AddWithValue("@PreviewPath", string.IsNullOrWhiteSpace(previewPath) ? DBNull.Value : previewPath);
                updateCmd.Parameters.AddWithValue("@UserInviteID", inviteId);
                updateCmd.ExecuteNonQuery();
            }

            AuditLogService.Log(companyName, null, createdBy, RoleAccessService.SuperAdminRole, "InviteSend", "UserInvite", inviteId.ToString(), "User invite created", $"Invite for {email} was delivered by {deliveryChannel}.");

            return new UserInviteDeliveryResult
            {
                UserInviteID = inviteId,
                InviteToken = token,
                InviteLink = inviteLink,
                DeliveryStatus = deliveryStatus,
                DeliveryChannel = deliveryChannel,
                PreviewPath = previewPath,
                ExpiresAt = expiresAt
            };
        }

        public static InviteOnboardingModel? GetInviteForOnboarding(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            using SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            using SqlCommand cmd = new SqlCommand(@"
                SELECT TOP 1 i.UserInviteID, i.UserID, i.InviteEmail, i.InviteToken, i.ExpiresAt, i.IsUsed,
                             u.UserName, u.CompanyName, u.UserRole
                FROM MST_UserInvite i
                INNER JOIN MST_User u ON u.UserID = i.UserID
                WHERE i.InviteToken = @InviteToken", con);
            cmd.Parameters.AddWithValue("@InviteToken", token);
            con.Open();

            using SqlDataReader reader = cmd.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            bool isUsed = reader["IsUsed"] != DBNull.Value && Convert.ToBoolean(reader["IsUsed"]);
            DateTime expiresAt = Convert.ToDateTime(reader["ExpiresAt"]);
            if (isUsed || expiresAt < DateTime.Now)
            {
                return null;
            }

            return new InviteOnboardingModel
            {
                InviteToken = token,
                UserID = Convert.ToInt32(reader["UserID"]),
                UserName = reader["UserName"].ToString() ?? string.Empty,
                Email = reader["InviteEmail"].ToString() ?? string.Empty,
                CompanyName = reader["CompanyName"].ToString() ?? string.Empty,
                UserRole = reader["UserRole"].ToString() ?? string.Empty,
                ExpiresAt = expiresAt
            };
        }

        public static bool CompleteInvite(InviteOnboardingModel model)
        {
            using SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            con.Open();

            using SqlTransaction transaction = con.BeginTransaction();
            try
            {
                int userId;
                using (SqlCommand validateCmd = new SqlCommand(@"
                    SELECT TOP 1 UserID
                    FROM MST_UserInvite
                    WHERE InviteToken = @InviteToken
                      AND IsUsed = 0
                      AND ExpiresAt >= GETDATE()", con, transaction))
                {
                    validateCmd.Parameters.AddWithValue("@InviteToken", model.InviteToken);
                    object? result = validateCmd.ExecuteScalar();
                    if (result == null || result == DBNull.Value)
                    {
                        transaction.Rollback();
                        return false;
                    }

                    userId = Convert.ToInt32(result);
                }

                using (SqlCommand userCmd = new SqlCommand(@"
                    UPDATE MST_User
                    SET Password = '',
                        PasswordHash = @PasswordHash,
                        IsAutoPassword = 0,
                        IsActive = 1,
                        Modified = GETDATE()
                    WHERE UserID = @UserID", con, transaction))
                {
                    userCmd.Parameters.AddWithValue("@PasswordHash", PasswordSecurity.HashPassword(model.Password));
                    userCmd.Parameters.AddWithValue("@UserID", userId);
                    userCmd.ExecuteNonQuery();
                }

                using (SqlCommand inviteCmd = new SqlCommand(@"
                    UPDATE MST_UserInvite
                    SET IsUsed = 1,
                        UsedAt = GETDATE(),
                        Modified = GETDATE()
                    WHERE InviteToken = @InviteToken", con, transaction))
                {
                    inviteCmd.Parameters.AddWithValue("@InviteToken", model.InviteToken);
                    inviteCmd.ExecuteNonQuery();
                }

                transaction.Commit();
                AuditLogService.Log(model.CompanyName, userId, model.UserName, model.UserRole, "InviteAccepted", "UserInvite", userId.ToString(), "User onboarding completed", $"{model.UserName} completed invite onboarding.");
                return true;
            }
            catch
            {
                transaction.Rollback();
                return false;
            }
        }

        public static List<InviteHistoryEntryModel> GetInviteHistory(string? searchtext, string? statusFilter)
        {
            List<InviteHistoryEntryModel> items = new List<InviteHistoryEntryModel>();

            using SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            using SqlCommand cmd = new SqlCommand(@"
                SELECT i.UserInviteID, i.UserID, i.InviteEmail, i.DeliveryStatus, i.DeliveryChannel, i.PreviewPath,
                       i.IsUsed, i.ExpiresAt, i.Created, i.UsedAt,
                       u.UserName, u.UserRole, u.CompanyName
                FROM MST_UserInvite i
                INNER JOIN MST_User u ON u.UserID = i.UserID
                ORDER BY i.Created DESC", con);
            con.Open();

            using SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                InviteHistoryEntryModel item = new InviteHistoryEntryModel
                {
                    UserInviteID = Convert.ToInt32(reader["UserInviteID"]),
                    UserID = Convert.ToInt32(reader["UserID"]),
                    InviteEmail = reader["InviteEmail"].ToString() ?? string.Empty,
                    DeliveryStatus = reader["DeliveryStatus"].ToString() ?? string.Empty,
                    DeliveryChannel = reader["DeliveryChannel"].ToString() ?? string.Empty,
                    PreviewPath = reader["PreviewPath"].ToString() ?? string.Empty,
                    IsUsed = reader["IsUsed"] != DBNull.Value && Convert.ToBoolean(reader["IsUsed"]),
                    ExpiresAt = Convert.ToDateTime(reader["ExpiresAt"]),
                    Created = Convert.ToDateTime(reader["Created"]),
                    UsedAt = reader["UsedAt"] == DBNull.Value ? null : Convert.ToDateTime(reader["UsedAt"]),
                    UserName = reader["UserName"].ToString() ?? string.Empty,
                    UserRole = reader["UserRole"].ToString() ?? string.Empty,
                    CompanyName = reader["CompanyName"].ToString() ?? string.Empty
                };

                if (!string.IsNullOrWhiteSpace(searchtext))
                {
                    string search = searchtext.Trim();
                    bool matches = item.UserName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                   item.InviteEmail.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                   item.CompanyName.Contains(search, StringComparison.OrdinalIgnoreCase);
                    if (!matches)
                    {
                        continue;
                    }
                }

                if (!string.IsNullOrWhiteSpace(statusFilter))
                {
                    if (string.Equals(statusFilter, "used", StringComparison.OrdinalIgnoreCase) && !item.IsUsed)
                    {
                        continue;
                    }

                    if (string.Equals(statusFilter, "open", StringComparison.OrdinalIgnoreCase) && item.IsUsed)
                    {
                        continue;
                    }

                    if (string.Equals(statusFilter, "expired", StringComparison.OrdinalIgnoreCase) && (item.IsUsed || item.ExpiresAt >= DateTime.Now))
                    {
                        continue;
                    }
                }

                items.Add(item);
            }

            return items;
        }

        public static string GetPreviewPathByInviteId(int userInviteId)
        {
            using SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            using SqlCommand cmd = new SqlCommand("SELECT TOP 1 PreviewPath FROM MST_UserInvite WHERE UserInviteID = @UserInviteID", con);
            cmd.Parameters.AddWithValue("@UserInviteID", userInviteId);
            con.Open();
            return Convert.ToString(cmd.ExecuteScalar()) ?? string.Empty;
        }

        private static void ExpireOutstandingInvites(SqlConnection con, int userId)
        {
            using SqlCommand cmd = new SqlCommand(@"
                UPDATE MST_UserInvite
                SET IsUsed = 1,
                    UsedAt = GETDATE(),
                    Modified = GETDATE()
                WHERE UserID = @UserID
                  AND IsUsed = 0", con);
            cmd.Parameters.AddWithValue("@UserID", userId);
            cmd.ExecuteNonQuery();
        }

        private static bool CanSendEmail(SystemSettingsModel settings)
        {
            return !string.IsNullOrWhiteSpace(settings.SmtpHost) &&
                   !string.IsNullOrWhiteSpace(settings.SmtpFromEmail);
        }

        private static void SendEmail(SystemSettingsModel settings, string toEmail, string toName, string subject, string htmlBody)
        {
            using SmtpClient client = new SmtpClient(settings.SmtpHost, settings.SmtpPort)
            {
                EnableSsl = settings.SmtpUseSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            if (!string.IsNullOrWhiteSpace(settings.SmtpUsername))
            {
                client.Credentials = new NetworkCredential(settings.SmtpUsername, settings.SmtpPassword);
            }

            using MailMessage message = new MailMessage
            {
                From = new MailAddress(settings.SmtpFromEmail, string.IsNullOrWhiteSpace(settings.SmtpFromName) ? settings.PlatformName : settings.SmtpFromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            message.To.Add(new MailAddress(toEmail, toName));
            client.Send(message);
        }

        private static string SavePreview(string email, string subject, string htmlBody)
        {
            Directory.CreateDirectory(MailPreviewDirectory);
            string safeEmail = string.Concat(email.Where(ch => char.IsLetterOrDigit(ch) || ch == '@' || ch == '.')).Replace("@", "_at_");
            string fileName = $"{DateTime.Now:yyyyMMddHHmmss}_{safeEmail}.html";
            string path = Path.Combine(MailPreviewDirectory, fileName);

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>Invite Preview</title></head><body>");
            builder.AppendLine($"<h2>{WebUtility.HtmlEncode(subject)}</h2>");
            builder.AppendLine(htmlBody);
            builder.AppendLine("</body></html>");
            File.WriteAllText(path, builder.ToString());

            return path;
        }

        private static string ResolveBaseUrl(SystemSettingsModel settings)
        {
            if (!string.IsNullOrWhiteSpace(settings.InviteBaseUrl))
            {
                return settings.InviteBaseUrl;
            }

            return "http://localhost:7289";
        }

        private static string BuildInviteHtml(SystemSettingsModel settings, string userName, string companyName, string userRole, string inviteLink, DateTime expiresAt)
        {
            string platformName = string.IsNullOrWhiteSpace(settings.PlatformName) ? "Meeting Of Minutes" : settings.PlatformName;
            return $@"
                <div style=""font-family:Inter,Segoe UI,Arial,sans-serif;background:#f5f7fb;padding:32px;"">
                    <div style=""max-width:640px;margin:0 auto;background:#ffffff;border:1px solid #e6ebf3;border-radius:24px;padding:32px;"">
                        <div style=""font-size:12px;letter-spacing:.14em;text-transform:uppercase;color:#6d7b99;font-weight:700;margin-bottom:14px;"">{platformName}</div>
                        <h1 style=""margin:0 0 12px;font-size:30px;line-height:1.05;color:#17203f;"">Finish setting up your account</h1>
                        <p style=""margin:0 0 18px;color:#5f6d8a;font-size:16px;"">Hi {WebUtility.HtmlEncode(userName)}, you were invited to join <strong>{WebUtility.HtmlEncode(companyName)}</strong> as <strong>{WebUtility.HtmlEncode(userRole)}</strong>.</p>
                        <a href=""{inviteLink}"" style=""display:inline-block;padding:14px 22px;border-radius:999px;background:linear-gradient(135deg,#5c68ff,#7568ff);color:#fff;text-decoration:none;font-weight:700;"">Accept invite</a>
                        <p style=""margin:18px 0 8px;color:#5f6d8a;font-size:14px;"">This link expires on {expiresAt:dd MMM yyyy hh:mm tt}.</p>
                        <p style=""margin:0;color:#8a94ab;font-size:13px;"">If the button does not open, copy this link: <br/><span style=""color:#33415f;"">{WebUtility.HtmlEncode(inviteLink)}</span></p>
                    </div>
                </div>";
        }
    }
}
