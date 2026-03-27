using System.Diagnostics;
using Meeting_Of_Minutes.Models;
using Meeting_Of_Minutes.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Meeting_Of_Minutes.Controllers
{
    public class DashBoardController : Controller
    {
        #region Actions
        public IActionResult DashBoard()
        {
            bool isSuperAdmin = RoleAccessService.IsSuperAdmin(HttpContext.Session.GetString("UserRole"));
            List<MeetingsModel> allMeetings = new List<MeetingsModel>();
            List<MeetingsModel> recentMeetings = new List<MeetingsModel>();
            List<AdminNotificationModel> adminNotifications = new List<AdminNotificationModel>();
            List<AuditLogModel> recentActivityLogs = new List<AuditLogModel>();
            List<AuditLogModel> failedLoginLogs = new List<AuditLogModel>();
            Dictionary<string, int> typeCounts = new Dictionary<string, int>();
            Dictionary<string, int> departmentCounts = new Dictionary<string, int>();
            HashSet<int> allowedDepartmentIds = isSuperAdmin ? GetAllDepartmentIds() : GetAllowedDepartmentIdsForCompany();
            List<ProfileUpdateRequestModel> pendingProfileRequests = new List<ProfileUpdateRequestModel>();
            List<StaffTransferRequestModel> pendingTransferRequests = new List<StaffTransferRequestModel>();
            List<AuditLogModel> recentPasswordResetLogs = new List<AuditLogModel>();
            int totalAttendees = 0;
            int totalMeetings = 0;
            int completedMeetings = 0;
            int cancelledMeetings = 0;
            int activeMeetings = 0;
            int activeUserCount = 0;
            int inactiveUserCount = 0;
            int tempPasswordUserCount = 0;
            int activeAdminCount = 0;

            SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_Meetings_SelectAll";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@searchtext", DBNull.Value);

            con.Open();
            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                MeetingsModel meeting = new MeetingsModel();
                meeting.MeetingID = Convert.ToInt32(reader["MeetingID"]);
                meeting.MeetingDate = reader["MeetingDate"] as DateTime?;
                meeting.DepartmentID = reader["DepartmentID"] == DBNull.Value ? null : Convert.ToInt32(reader["DepartmentID"]);
                meeting.MeetingTypeName = reader["MeetingTypeName"].ToString();
                meeting.DepartmentName = reader["DepartmentName"].ToString();
                meeting.MeetingDescription = reader["MeetingDescription"].ToString();
                meeting.IsCancelled = reader["IsCancelled"] == DBNull.Value ? false : Convert.ToBoolean(reader["IsCancelled"]);
                bool canAccessMeeting = isSuperAdmin
                    ? true
                    : IsAdmin()
                    ? allowedDepartmentIds.Contains(meeting.DepartmentID ?? 0) && CanAccessDepartmentMeeting(meeting.DepartmentID)
                    : IsMeetingAssignedToCurrentUser(meeting.MeetingID);

                if (canAccessMeeting)
                {
                    allMeetings.Add(meeting);
                }
            }

            reader.Close();

            foreach (MeetingsModel meeting in allMeetings.OrderByDescending(m => m.MeetingDate))
            {
                if (recentMeetings.Count < 5)
                {
                    recentMeetings.Add(meeting);
                }

                totalMeetings++;

                if (meeting.IsCancelled)
                {
                    cancelledMeetings++;
                }
                else if (meeting.MeetingDate.HasValue && meeting.MeetingDate.Value > DateTime.Now)
                {
                    activeMeetings++;
                }
                else
                {
                    completedMeetings++;
                }

                string typeName = string.IsNullOrWhiteSpace(meeting.MeetingTypeName) ? "Other" : meeting.MeetingTypeName;
                if (!typeCounts.ContainsKey(typeName))
                {
                    typeCounts[typeName] = 0;
                }
                typeCounts[typeName]++;

                string departmentName = string.IsNullOrWhiteSpace(meeting.DepartmentName) ? "Other" : meeting.DepartmentName;
                if (!departmentCounts.ContainsKey(departmentName))
                {
                    departmentCounts[departmentName] = 0;
                }
                departmentCounts[departmentName]++;
            }

            cmd.Parameters.Clear();
            cmd.CommandText = "PR_MeetingMember_SelectAll";
            reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                int meetingId = Convert.ToInt32(reader["MeetingID"]);
                if (isSuperAdmin || IsAdmin() || IsMeetingInUserDepartment(meetingId))
                {
                    totalAttendees++;
                }
            }

            reader.Close();

            if (IsAdmin() && !isSuperAdmin)
            {
                cmd.Parameters.Clear();
                cmd.CommandText = "PR_MST_AdminNotification_SelectAll";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@CompanyName", GetCurrentCompanyName());
                reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    AdminNotificationModel notification = new AdminNotificationModel();
                    notification.AdminNotificationID = Convert.ToInt32(reader["AdminNotificationID"]);
                    notification.CompanyName = reader["CompanyName"].ToString() ?? string.Empty;
                    notification.NotificationType = reader["NotificationType"].ToString() ?? string.Empty;
                    notification.Title = reader["Title"].ToString() ?? string.Empty;
                    notification.Message = reader["Message"].ToString() ?? string.Empty;
                    notification.RelatedUserID = reader["RelatedUserID"] == DBNull.Value ? null : Convert.ToInt32(reader["RelatedUserID"]);
                    notification.Created = Convert.ToDateTime(reader["Created"]);
                    adminNotifications.Add(notification);
                }

                reader.Close();
            }

            con.Close();

            if (IsAdmin())
            {
                if (isSuperAdmin)
                {
                    pendingProfileRequests = GetPendingProfileRequestsGlobal();
                    pendingTransferRequests = GetPendingTransferRequestsGlobal();
                    List<AuditLogModel> globalLogs = AuditLogService.GetRecentGlobal(20);
                    recentActivityLogs = globalLogs.Take(8).ToList();
                    failedLoginLogs = globalLogs.Where(log => string.Equals(log.ActionType, "LoginFailed", StringComparison.OrdinalIgnoreCase)).Take(5).ToList();
                    recentPasswordResetLogs = globalLogs.Where(log => string.Equals(log.ActionType, "PasswordReset", StringComparison.OrdinalIgnoreCase)).Take(4).ToList();
                    (activeUserCount, inactiveUserCount, tempPasswordUserCount, activeAdminCount) = GetGlobalAccountHealthSummary();
                }
                else
                {
                    pendingProfileRequests = GetPendingProfileRequestsForCompany();
                    pendingTransferRequests = GetPendingTransferRequestsForCompany();
                    List<AuditLogModel> companyLogs = AuditLogService.GetRecentByCompany(GetCurrentCompanyName(), 20);
                    recentActivityLogs = companyLogs.Take(8).ToList();
                    failedLoginLogs = companyLogs.Where(log => string.Equals(log.ActionType, "LoginFailed", StringComparison.OrdinalIgnoreCase)).Take(5).ToList();
                    recentPasswordResetLogs = companyLogs.Where(log => string.Equals(log.ActionType, "PasswordReset", StringComparison.OrdinalIgnoreCase)).Take(4).ToList();
                    (activeUserCount, inactiveUserCount, tempPasswordUserCount, activeAdminCount) = GetAccountHealthSummary();
                }
            }
            else
            {
                pendingProfileRequests = GetPendingProfileRequestsForUser();
                pendingTransferRequests = GetPendingTransferRequestsForUser();
                int? userId = HttpContext.Session.GetInt32("UserID");
                recentActivityLogs = userId.HasValue ? AuditLogService.GetRecentByUser(userId.Value, 10) : new List<AuditLogModel>();
            }

            ActionCenterSummaryModel actionCenterSummary = ActionCenterService.BuildSummary(
                IsAdmin(),
                GetCurrentCompanyName(),
                HttpContext.Session.GetInt32("UserID"),
                HttpContext.Session.GetInt32("StaffID"),
                3);

            MeetingsModel? nextMeeting = allMeetings
                .Where(m => !m.IsCancelled && m.MeetingDate.HasValue && m.MeetingDate.Value >= DateTime.Now)
                .OrderBy(m => m.MeetingDate)
                .FirstOrDefault();

            ViewBag.TotalMeetings = totalMeetings;
            ViewBag.ActiveMeetings = activeMeetings;
            ViewBag.CompletedMeetings = completedMeetings;
            ViewBag.CancelledMeetings = cancelledMeetings;
            ViewBag.TotalAttendees = totalAttendees;
            ViewBag.TypeLabels = typeCounts.Keys.ToList();
            ViewBag.TypeValues = typeCounts.Values.ToList();
            ViewBag.DepartmentLabels = departmentCounts.Keys.ToList();
            ViewBag.DepartmentValues = departmentCounts.Values.ToList();
            ViewBag.RecentMeetings = recentMeetings;
            ViewBag.AdminNotifications = adminNotifications;
            ViewBag.PendingProfileRequestCount = pendingProfileRequests.Count;
            ViewBag.PendingTransferRequestCount = pendingTransferRequests.Count;
            ViewBag.PendingProfileRequests = pendingProfileRequests;
            ViewBag.PendingTransferRequests = pendingTransferRequests;
            ViewBag.NextMeeting = nextMeeting;
            ViewBag.RecentActivityLogs = recentActivityLogs;
            ViewBag.FailedLoginLogs = failedLoginLogs;
            ViewBag.RecentPasswordResetLogs = recentPasswordResetLogs;
            ViewBag.ActionInboxCount = pendingProfileRequests.Count + pendingTransferRequests.Count + failedLoginLogs.Count;
            ViewBag.ActionCenterSummary = actionCenterSummary;
            ViewBag.ActiveUserCount = activeUserCount;
            ViewBag.InactiveUserCount = inactiveUserCount;
            ViewBag.TempPasswordUserCount = tempPasswordUserCount;
            ViewBag.ActiveAdminCount = activeAdminCount;
            ViewBag.IsSuperAdminDashboard = isSuperAdmin;
            ViewBag.PlatformCompanyCount = isSuperAdmin ? GetPlatformCompanyCount() : 1;
            return View();
        }

        public bool IsAdmin()
        {
            return RoleAccessService.IsAdminOrHigher(HttpContext.Session.GetString("UserRole"));
        }

        public bool CanAccessDepartmentMeeting(int? departmentId)
        {
            if (!departmentId.HasValue || !GetAllowedDepartmentIdsForCompany().Contains(departmentId.Value))
            {
                return false;
            }

            if (IsAdmin())
            {
                return true;
            }

            int? userDepartmentId = HttpContext.Session.GetInt32("DepartmentID");
            if (!userDepartmentId.HasValue)
            {
                return false;
            }

            return userDepartmentId.Value == departmentId.Value;
        }

        public bool IsMeetingAssignedToCurrentUser(int meetingId)
        {
            if (IsAdmin())
            {
                return true;
            }

            int? staffId = HttpContext.Session.GetInt32("StaffID");
            if (!staffId.HasValue)
            {
                return false;
            }

            SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = @"SELECT COUNT(*)
                                FROM MOM_MeetingMember mm
                                INNER JOIN MOM_Meetings m ON mm.MeetingID = m.MeetingID
                                INNER JOIN MOM_Department d ON m.DepartmentID = d.DepartmentID
                                WHERE mm.MeetingID = @MeetingID
                                  AND mm.StaffID = @StaffID
                                  AND d.CompanyName = @CompanyName";
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.AddWithValue("@MeetingID", meetingId);
            cmd.Parameters.AddWithValue("@StaffID", staffId.Value);
            cmd.Parameters.AddWithValue("@CompanyName", GetCurrentCompanyName());

            con.Open();
            int count = Convert.ToInt32(cmd.ExecuteScalar());
            con.Close();

            return count > 0;
        }

        public bool IsMeetingInUserDepartment(int meetingId)
        {
            if (!IsAdmin())
            {
                return IsMeetingAssignedToCurrentUser(meetingId);
            }

            SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "SELECT COUNT(*) FROM MOM_Meetings m INNER JOIN MOM_Department d ON m.DepartmentID = d.DepartmentID WHERE m.MeetingID = @MeetingID AND d.CompanyName = @CompanyName";
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.AddWithValue("@MeetingID", meetingId);
            cmd.Parameters.AddWithValue("@CompanyName", GetCurrentCompanyName());

            con.Open();
            int count = Convert.ToInt32(cmd.ExecuteScalar());
            con.Close();

            return count > 0;
        }

        public HashSet<int> GetAllowedDepartmentIdsForCompany()
        {
            HashSet<int> departmentIds = new HashSet<int>();
            string companyName = GetCurrentCompanyName();

            if (string.IsNullOrWhiteSpace(companyName))
            {
                return departmentIds;
            }

            SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "SELECT DepartmentID FROM MOM_Department WHERE CompanyName = @CompanyName";
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.AddWithValue("@CompanyName", companyName);

            con.Open();
            SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                departmentIds.Add(Convert.ToInt32(reader["DepartmentID"]));
            }
            reader.Close();
            con.Close();

            return departmentIds;
        }

        public HashSet<int> GetAllDepartmentIds()
        {
            HashSet<int> departmentIds = new HashSet<int>();

            using SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            using SqlCommand cmd = new SqlCommand("SELECT DepartmentID FROM MOM_Department", con);
            con.Open();
            using SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                departmentIds.Add(Convert.ToInt32(reader["DepartmentID"]));
            }

            return departmentIds;
        }

        public string GetCurrentCompanyName()
        {
            return HttpContext.Session.GetString("CompanyName") ?? string.Empty;
        }

        public (int ActiveUsers, int InactiveUsers, int TemporaryPasswordUsers, int ActiveAdmins) GetAccountHealthSummary()
        {
            int activeUsers = 0;
            int inactiveUsers = 0;
            int temporaryPasswordUsers = 0;
            int activeAdmins = 0;

            using SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            using SqlCommand cmd = new SqlCommand(@"
                SELECT
                    SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END) AS ActiveUsers,
                    SUM(CASE WHEN IsActive = 0 THEN 1 ELSE 0 END) AS InactiveUsers,
                    SUM(CASE WHEN IsAutoPassword = 1 AND IsActive = 1 THEN 1 ELSE 0 END) AS TemporaryPasswordUsers,
                    SUM(CASE WHEN UserRole = 'Admin' AND IsActive = 1 THEN 1 ELSE 0 END) AS ActiveAdmins
                FROM MST_User
                WHERE CompanyName = @CompanyName", con);
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.AddWithValue("@CompanyName", GetCurrentCompanyName());

            con.Open();
            using SqlDataReader reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                activeUsers = reader["ActiveUsers"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ActiveUsers"]);
                inactiveUsers = reader["InactiveUsers"] == DBNull.Value ? 0 : Convert.ToInt32(reader["InactiveUsers"]);
                temporaryPasswordUsers = reader["TemporaryPasswordUsers"] == DBNull.Value ? 0 : Convert.ToInt32(reader["TemporaryPasswordUsers"]);
                activeAdmins = reader["ActiveAdmins"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ActiveAdmins"]);
            }

            return (activeUsers, inactiveUsers, temporaryPasswordUsers, activeAdmins);
        }

        public (int ActiveUsers, int InactiveUsers, int TemporaryPasswordUsers, int ActiveAdmins) GetGlobalAccountHealthSummary()
        {
            int activeUsers = 0;
            int inactiveUsers = 0;
            int temporaryPasswordUsers = 0;
            int activeAdmins = 0;

            using SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            using SqlCommand cmd = new SqlCommand(@"
                SELECT
                    SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END) AS ActiveUsers,
                    SUM(CASE WHEN IsActive = 0 THEN 1 ELSE 0 END) AS InactiveUsers,
                    SUM(CASE WHEN IsAutoPassword = 1 AND IsActive = 1 THEN 1 ELSE 0 END) AS TemporaryPasswordUsers,
                    SUM(CASE WHEN UserRole IN ('Admin', 'SuperAdmin') AND IsActive = 1 THEN 1 ELSE 0 END) AS ActiveAdmins
                FROM MST_User", con);
            con.Open();
            using SqlDataReader reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                activeUsers = reader["ActiveUsers"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ActiveUsers"]);
                inactiveUsers = reader["InactiveUsers"] == DBNull.Value ? 0 : Convert.ToInt32(reader["InactiveUsers"]);
                temporaryPasswordUsers = reader["TemporaryPasswordUsers"] == DBNull.Value ? 0 : Convert.ToInt32(reader["TemporaryPasswordUsers"]);
                activeAdmins = reader["ActiveAdmins"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ActiveAdmins"]);
            }

            return (activeUsers, inactiveUsers, temporaryPasswordUsers, activeAdmins);
        }

        public List<ProfileUpdateRequestModel> GetPendingProfileRequestsForCompany()
        {
            List<ProfileUpdateRequestModel> requests = new List<ProfileUpdateRequestModel>();
            SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_MST_ProfileUpdateRequest_SelectAll";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@CompanyName", GetCurrentCompanyName());
            con.Open();
            SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                string status = reader["RequestStatus"].ToString() ?? string.Empty;
                if (!string.Equals(status, "Pending", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                ProfileUpdateRequestModel request = new ProfileUpdateRequestModel();
                request.ProfileUpdateRequestID = Convert.ToInt32(reader["ProfileUpdateRequestID"]);
                request.CurrentUserName = reader["CurrentUserName"].ToString() ?? string.Empty;
                request.RequestedEmail = reader["RequestedEmail"].ToString() ?? string.Empty;
                request.CompanyName = reader["CompanyName"].ToString() ?? string.Empty;
                request.DepartmentName = reader["DepartmentName"].ToString() ?? string.Empty;
                request.RequestStatus = status;
                request.Created = Convert.ToDateTime(reader["Created"]);
                requests.Add(request);
            }
            reader.Close();
            con.Close();
            return requests;
        }

        public List<ProfileUpdateRequestModel> GetPendingProfileRequestsGlobal()
        {
            List<ProfileUpdateRequestModel> requests = new List<ProfileUpdateRequestModel>();
            using SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            using SqlCommand cmd = new SqlCommand("PR_MST_ProfileUpdateRequest_SelectAll", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@CompanyName", DBNull.Value);
            con.Open();
            using SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                string status = reader["RequestStatus"].ToString() ?? string.Empty;
                if (!string.Equals(status, "Pending", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                requests.Add(new ProfileUpdateRequestModel
                {
                    ProfileUpdateRequestID = Convert.ToInt32(reader["ProfileUpdateRequestID"]),
                    CurrentUserName = reader["CurrentUserName"].ToString() ?? string.Empty,
                    RequestedEmail = reader["RequestedEmail"].ToString() ?? string.Empty,
                    CompanyName = reader["CompanyName"].ToString() ?? string.Empty,
                    DepartmentName = reader["DepartmentName"].ToString() ?? string.Empty,
                    RequestStatus = status,
                    Created = Convert.ToDateTime(reader["Created"])
                });
            }

            return requests;
        }

        public List<ProfileUpdateRequestModel> GetPendingProfileRequestsForUser()
        {
            List<ProfileUpdateRequestModel> requests = new List<ProfileUpdateRequestModel>();
            int? userId = HttpContext.Session.GetInt32("UserID");
            if (!userId.HasValue)
            {
                return requests;
            }

            SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_MST_ProfileUpdateRequest_SelectByUserID";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@UserID", userId.Value);
            con.Open();
            SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                string status = reader["RequestStatus"].ToString() ?? string.Empty;
                if (!string.Equals(status, "Pending", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                ProfileUpdateRequestModel request = new ProfileUpdateRequestModel();
                request.ProfileUpdateRequestID = Convert.ToInt32(reader["ProfileUpdateRequestID"]);
                request.RequestedUserName = reader["RequestedUserName"].ToString() ?? string.Empty;
                request.RequestedEmail = reader["RequestedEmail"].ToString() ?? string.Empty;
                request.RequestStatus = status;
                request.Created = Convert.ToDateTime(reader["Created"]);
                requests.Add(request);
            }
            reader.Close();
            con.Close();
            return requests;
        }

        public List<StaffTransferRequestModel> GetPendingTransferRequestsForCompany()
        {
            List<StaffTransferRequestModel> requests = new List<StaffTransferRequestModel>();
            SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_MST_StaffTransferRequest_SelectIncoming";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@TargetCompanyName", GetCurrentCompanyName());
            con.Open();
            SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                string status = reader["RequestStatus"].ToString() ?? string.Empty;
                if (!string.Equals(status, "Pending", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                StaffTransferRequestModel request = new StaffTransferRequestModel();
                request.StaffTransferRequestID = Convert.ToInt32(reader["StaffTransferRequestID"]);
                request.StaffName = reader["StaffName"].ToString() ?? string.Empty;
                request.SourceCompanyName = reader["SourceCompanyName"].ToString() ?? string.Empty;
                request.TargetCompanyName = reader["TargetCompanyName"].ToString() ?? string.Empty;
                request.TargetDepartmentName = reader["TargetDepartmentName"].ToString() ?? string.Empty;
                request.RequestStatus = status;
                request.Created = Convert.ToDateTime(reader["Created"]);
                requests.Add(request);
            }
            reader.Close();
            con.Close();
            return requests;
        }

        public List<StaffTransferRequestModel> GetPendingTransferRequestsGlobal()
        {
            List<StaffTransferRequestModel> requests = new List<StaffTransferRequestModel>();
            using SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            using SqlCommand cmd = new SqlCommand(@"
                SELECT r.StaffTransferRequestID, s.StaffName, r.SourceCompanyName, r.TargetCompanyName,
                       d.DepartmentName AS TargetDepartmentName, r.RequestStatus, r.Created
                FROM MST_StaffTransferRequest r
                LEFT JOIN MOM_Staff s ON s.StaffID = r.StaffID
                LEFT JOIN MOM_Department d ON d.DepartmentID = r.TargetDepartmentID
                WHERE r.RequestStatus = 'Pending'
                ORDER BY r.Created DESC", con);
            con.Open();
            using SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                requests.Add(new StaffTransferRequestModel
                {
                    StaffTransferRequestID = Convert.ToInt32(reader["StaffTransferRequestID"]),
                    StaffName = reader["StaffName"].ToString() ?? string.Empty,
                    SourceCompanyName = reader["SourceCompanyName"].ToString() ?? string.Empty,
                    TargetCompanyName = reader["TargetCompanyName"].ToString() ?? string.Empty,
                    TargetDepartmentName = reader["TargetDepartmentName"].ToString() ?? string.Empty,
                    RequestStatus = reader["RequestStatus"].ToString() ?? string.Empty,
                    Created = Convert.ToDateTime(reader["Created"])
                });
            }

            return requests;
        }

        public int GetPlatformCompanyCount()
        {
            using SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            using SqlCommand cmd = new SqlCommand("SELECT COUNT(DISTINCT CompanyName) FROM MST_User WHERE CompanyName IS NOT NULL AND LTRIM(RTRIM(CompanyName)) <> ''", con);
            con.Open();
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public List<StaffTransferRequestModel> GetPendingTransferRequestsForUser()
        {
            List<StaffTransferRequestModel> requests = new List<StaffTransferRequestModel>();
            int? staffId = HttpContext.Session.GetInt32("StaffID");
            if (!staffId.HasValue)
            {
                return requests;
            }

            SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_MST_StaffTransferRequest_SelectByStaffID";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@StaffID", staffId.Value);
            con.Open();
            SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                string status = reader["RequestStatus"].ToString() ?? string.Empty;
                if (!string.Equals(status, "Pending", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                StaffTransferRequestModel request = new StaffTransferRequestModel();
                request.StaffTransferRequestID = Convert.ToInt32(reader["StaffTransferRequestID"]);
                request.TargetCompanyName = reader["TargetCompanyName"].ToString() ?? string.Empty;
                request.TargetDepartmentName = reader["TargetDepartmentName"].ToString() ?? string.Empty;
                request.RequestStatus = status;
                request.Created = Convert.ToDateTime(reader["Created"]);
                requests.Add(request);
            }
            reader.Close();
            con.Close();
            return requests;
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        #endregion
    }
}







