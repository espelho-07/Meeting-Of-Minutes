using System.Diagnostics;
using Meeting_Of_Minutes.Models;
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
            List<MeetingsModel> allMeetings = new List<MeetingsModel>();
            List<MeetingsModel> recentMeetings = new List<MeetingsModel>();
            List<AdminNotificationModel> adminNotifications = new List<AdminNotificationModel>();
            Dictionary<string, int> typeCounts = new Dictionary<string, int>();
            Dictionary<string, int> departmentCounts = new Dictionary<string, int>();
            HashSet<int> allowedDepartmentIds = GetAllowedDepartmentIdsForCompany();
            List<ProfileUpdateRequestModel> pendingProfileRequests = new List<ProfileUpdateRequestModel>();
            List<StaffTransferRequestModel> pendingTransferRequests = new List<StaffTransferRequestModel>();
            int totalAttendees = 0;
            int totalMeetings = 0;
            int completedMeetings = 0;
            int cancelledMeetings = 0;
            int activeMeetings = 0;

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
                bool canAccessMeeting = IsAdmin()
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
                if (IsAdmin() || IsMeetingInUserDepartment(meetingId))
                {
                    totalAttendees++;
                }
            }

            reader.Close();

            if (IsAdmin())
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
                pendingProfileRequests = GetPendingProfileRequestsForCompany();
                pendingTransferRequests = GetPendingTransferRequestsForCompany();
            }
            else
            {
                pendingProfileRequests = GetPendingProfileRequestsForUser();
                pendingTransferRequests = GetPendingTransferRequestsForUser();
            }

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
            return View();
        }

        public bool IsAdmin()
        {
            return string.Equals(HttpContext.Session.GetString("UserRole"), "Admin", StringComparison.OrdinalIgnoreCase);
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

        public string GetCurrentCompanyName()
        {
            return HttpContext.Session.GetString("CompanyName") ?? string.Empty;
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







