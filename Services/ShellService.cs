using Meeting_Of_Minutes.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Meeting_Of_Minutes.Services
{
    public static class ShellService
    {
        public static AppShellViewModel Build(HttpContext httpContext, RouteData routeData)
        {
            string currentController = routeData.Values["controller"]?.ToString() ?? string.Empty;
            string currentAction = routeData.Values["action"]?.ToString() ?? string.Empty;
            string currentUserRole = httpContext.Session.GetString("UserRole") ?? string.Empty;
            bool isSuperAdminUser = RoleAccessService.IsSuperAdmin(currentUserRole);
            bool isAdminUser = RoleAccessService.IsAdminOrHigher(currentUserRole);
            string companyName = httpContext.Session.GetString("CompanyName") ?? string.Empty;
            int? userId = httpContext.Session.GetInt32("UserID");
            int? staffId = httpContext.Session.GetInt32("StaffID");
            SystemSettingsModel platformSettings = SystemSettingsService.GetSettings();

            ActionCenterSummaryModel actionCenterSummary = ActionCenterService.BuildSummary(isAdminUser, companyName, userId, staffId);
            List<AdminNotificationModel> notifications = isAdminUser ? GetNotifications(companyName, isSuperAdminUser) : new List<AdminNotificationModel>();
            int unreadNotificationCount = notifications.Count(x => !x.IsRead);

            return new AppShellViewModel
            {
                CurrentController = currentController,
                CurrentAction = currentAction,
                IsAdminUser = isAdminUser,
                IsSuperAdminUser = isSuperAdminUser,
                CompanyName = companyName,
                UserName = httpContext.Session.GetString("UserName") ?? string.Empty,
                UserRole = currentUserRole,
                UnreadNotificationCount = unreadNotificationCount,
                OpenActionCount = actionCenterSummary.OpenActionCount,
                CriticalActionCount = actionCenterSummary.CriticalActionCount,
                Notifications = notifications,
                ActionCenterSummary = actionCenterSummary,
                PlatformSettings = platformSettings
            };
        }

        private static List<AdminNotificationModel> GetNotifications(string companyName, bool isSuperAdminUser)
        {
            List<AdminNotificationModel> notifications = new List<AdminNotificationModel>();

            using SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            using SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;

            if (isSuperAdminUser)
            {
                cmd.CommandText = @"
                    SELECT TOP 20 AdminNotificationID, CompanyName, NotificationType, Title, Message, RelatedUserID, IsRead, Created
                    FROM MST_AdminNotification
                    ORDER BY Created DESC";
                cmd.CommandType = CommandType.Text;
            }
            else
            {
                cmd.CommandText = "PR_MST_AdminNotification_SelectAll";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@CompanyName", companyName);
            }

            con.Open();
            using SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                notifications.Add(new AdminNotificationModel
                {
                    AdminNotificationID = Convert.ToInt32(reader["AdminNotificationID"]),
                    CompanyName = reader["CompanyName"].ToString() ?? string.Empty,
                    NotificationType = reader["NotificationType"].ToString() ?? string.Empty,
                    Title = reader["Title"].ToString() ?? string.Empty,
                    Message = reader["Message"].ToString() ?? string.Empty,
                    RelatedUserID = reader["RelatedUserID"] == DBNull.Value ? null : Convert.ToInt32(reader["RelatedUserID"]),
                    IsRead = reader["IsRead"] != DBNull.Value && Convert.ToBoolean(reader["IsRead"]),
                    Created = Convert.ToDateTime(reader["Created"])
                });
            }

            return notifications;
        }
    }
}
