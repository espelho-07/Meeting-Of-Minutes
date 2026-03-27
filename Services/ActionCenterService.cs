using Meeting_Of_Minutes.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Meeting_Of_Minutes.Services
{
    public static class ActionCenterService
    {
        public static ActionCenterViewModel BuildViewModel(bool isAdminUser, string companyName, int? userId, int? staffId)
        {
            List<ProfileUpdateRequestModel> pendingProfileRequests = isAdminUser
                ? GetPendingProfileRequestsForCompany(companyName)
                : GetPendingProfileRequestsForUser(userId);
            List<StaffTransferRequestModel> pendingTransferRequests = isAdminUser
                ? GetPendingTransferRequestsForCompany(companyName)
                : GetPendingTransferRequestsForUser(staffId);
            List<AuditLogModel> recentActivity = isAdminUser
                ? AuditLogService.GetRecentByCompany(companyName, 20)
                : userId.HasValue
                    ? AuditLogService.GetRecentByUser(userId.Value, 20)
                    : new List<AuditLogModel>();

            List<AuditLogModel> criticalActivity = recentActivity
                .Where(log => log.ActionType is "Delete" or "Cancel" or "Reject" or "LoginFailed")
                .Take(8)
                .ToList();

            ActionCenterViewModel viewModel = new ActionCenterViewModel
            {
                IsAdminUser = isAdminUser,
                PendingProfileRequests = pendingProfileRequests,
                PendingTransferRequests = pendingTransferRequests,
                CriticalActivity = criticalActivity,
                RecentActivity = recentActivity.Take(10).ToList()
            };

            viewModel.PriorityQueue = BuildPriorityQueue(viewModel);
            viewModel.FilteredPriorityQueue = viewModel.PriorityQueue;
            viewModel.ActionCount = pendingProfileRequests.Count + pendingTransferRequests.Count + criticalActivity.Count;
            viewModel.Overdue24Count = viewModel.PriorityQueue.Count(x => x.IsStale24Hours);
            viewModel.Overdue72Count = viewModel.PriorityQueue.Count(x => x.IsStale72Hours);
            return viewModel;
        }

        public static ActionCenterSummaryModel BuildSummary(bool isAdminUser, string companyName, int? userId, int? staffId, int previewCount = 4)
        {
            ActionCenterViewModel viewModel = BuildViewModel(isAdminUser, companyName, userId, staffId);
            return new ActionCenterSummaryModel
            {
                OpenActionCount = viewModel.ActionCount,
                CriticalActionCount = viewModel.CriticalActivity.Count,
                Overdue24Count = viewModel.Overdue24Count,
                Overdue72Count = viewModel.Overdue72Count,
                PreviewItems = viewModel.PriorityQueue.Take(previewCount).ToList()
            };
        }

        public static List<ActionCenterItemModel> ApplyQueueFilters(List<ActionCenterItemModel> items, string scope, string searchtext)
        {
            IEnumerable<ActionCenterItemModel> filtered = items;

            filtered = scope switch
            {
                "approvals" => filtered.Where(item => item.ItemType is "ProfileUpdateRequest" or "StaffTransferRequest"),
                "critical" => filtered.Where(item => item.IsCritical),
                "stale" => filtered.Where(item => item.IsStale24Hours),
                "overdue72" => filtered.Where(item => item.IsStale72Hours),
                _ => filtered
            };

            if (!string.IsNullOrWhiteSpace(searchtext))
            {
                filtered = filtered.Where(item =>
                    item.Title.Contains(searchtext, StringComparison.OrdinalIgnoreCase) ||
                    item.Description.Contains(searchtext, StringComparison.OrdinalIgnoreCase) ||
                    item.Meta.Contains(searchtext, StringComparison.OrdinalIgnoreCase) ||
                    item.StatusLabel.Contains(searchtext, StringComparison.OrdinalIgnoreCase));
            }

            return filtered.ToList();
        }

        private static List<ProfileUpdateRequestModel> GetPendingProfileRequestsForCompany(string companyName)
        {
            List<ProfileUpdateRequestModel> requests = new List<ProfileUpdateRequestModel>();
            using SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            using SqlCommand cmd = new SqlCommand("PR_MST_ProfileUpdateRequest_SelectAll", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@CompanyName", companyName);
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

        private static List<ProfileUpdateRequestModel> GetPendingProfileRequestsForUser(int? userId)
        {
            List<ProfileUpdateRequestModel> requests = new List<ProfileUpdateRequestModel>();
            if (!userId.HasValue)
            {
                return requests;
            }

            using SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            using SqlCommand cmd = new SqlCommand("PR_MST_ProfileUpdateRequest_SelectByUserID", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@UserID", userId.Value);
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
                    RequestedUserName = reader["RequestedUserName"].ToString() ?? string.Empty,
                    RequestedEmail = reader["RequestedEmail"].ToString() ?? string.Empty,
                    RequestStatus = status,
                    Created = Convert.ToDateTime(reader["Created"])
                });
            }
            return requests;
        }

        private static List<StaffTransferRequestModel> GetPendingTransferRequestsForCompany(string companyName)
        {
            List<StaffTransferRequestModel> requests = new List<StaffTransferRequestModel>();
            using SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            using SqlCommand cmd = new SqlCommand("PR_MST_StaffTransferRequest_SelectIncoming", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@TargetCompanyName", companyName);
            con.Open();
            using SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                string status = reader["RequestStatus"].ToString() ?? string.Empty;
                if (!string.Equals(status, "Pending", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                requests.Add(new StaffTransferRequestModel
                {
                    StaffTransferRequestID = Convert.ToInt32(reader["StaffTransferRequestID"]),
                    StaffName = reader["StaffName"].ToString() ?? string.Empty,
                    SourceCompanyName = reader["SourceCompanyName"].ToString() ?? string.Empty,
                    TargetCompanyName = reader["TargetCompanyName"].ToString() ?? string.Empty,
                    TargetDepartmentName = reader["TargetDepartmentName"].ToString() ?? string.Empty,
                    RequestStatus = status,
                    Created = Convert.ToDateTime(reader["Created"])
                });
            }
            return requests;
        }

        private static List<StaffTransferRequestModel> GetPendingTransferRequestsForUser(int? staffId)
        {
            List<StaffTransferRequestModel> requests = new List<StaffTransferRequestModel>();
            if (!staffId.HasValue)
            {
                return requests;
            }

            using SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            using SqlCommand cmd = new SqlCommand("PR_MST_StaffTransferRequest_SelectByStaffID", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@StaffID", staffId.Value);
            con.Open();
            using SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                string status = reader["RequestStatus"].ToString() ?? string.Empty;
                if (!string.Equals(status, "Pending", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                requests.Add(new StaffTransferRequestModel
                {
                    StaffTransferRequestID = Convert.ToInt32(reader["StaffTransferRequestID"]),
                    TargetCompanyName = reader["TargetCompanyName"].ToString() ?? string.Empty,
                    TargetDepartmentName = reader["TargetDepartmentName"].ToString() ?? string.Empty,
                    RequestStatus = status,
                    Created = Convert.ToDateTime(reader["Created"])
                });
            }
            return requests;
        }

        private static List<ActionCenterItemModel> BuildPriorityQueue(ActionCenterViewModel viewModel)
        {
            List<ActionCenterItemModel> items = new List<ActionCenterItemModel>();

            foreach (ProfileUpdateRequestModel request in viewModel.PendingProfileRequests)
            {
                double ageHours = (DateTime.Now - request.Created).TotalHours;
                items.Add(new ActionCenterItemModel
                {
                    ItemType = "ProfileUpdateRequest",
                    ItemID = request.ProfileUpdateRequestID,
                    Title = viewModel.IsAdminUser ? $"Profile update for {request.CurrentUserName}" : "Profile update request pending",
                    Description = viewModel.IsAdminUser
                        ? $"{request.RequestedEmail} | {(string.IsNullOrWhiteSpace(request.DepartmentName) ? "No department" : request.DepartmentName)}"
                        : $"Requested email: {request.RequestedEmail}",
                    Meta = request.Created.ToString("dd MMM yyyy hh:mm tt"),
                    Created = request.Created,
                    StatusLabel = request.RequestStatus,
                    PriorityLabel = GetPriorityLabel(ageHours),
                    PriorityRank = GetPriorityRank(ageHours, false),
                    IsStale24Hours = ageHours >= 24,
                    IsStale72Hours = ageHours >= 72,
                    IsCritical = ageHours >= 72,
                    TargetUrl = viewModel.IsAdminUser ? "/Profile/ProfileUpdateRequestList" : "/Profile/Profile"
                });
            }

            foreach (StaffTransferRequestModel request in viewModel.PendingTransferRequests)
            {
                double ageHours = (DateTime.Now - request.Created).TotalHours;
                items.Add(new ActionCenterItemModel
                {
                    ItemType = "StaffTransferRequest",
                    ItemID = request.StaffTransferRequestID,
                    Title = viewModel.IsAdminUser ? $"{request.StaffName} transfer request" : $"Transfer request to {request.TargetCompanyName}",
                    Description = viewModel.IsAdminUser
                        ? $"{request.SourceCompanyName} -> {request.TargetDepartmentName}"
                        : $"Target department: {request.TargetDepartmentName}",
                    Meta = request.Created.ToString("dd MMM yyyy hh:mm tt"),
                    Created = request.Created,
                    StatusLabel = request.RequestStatus,
                    PriorityLabel = GetPriorityLabel(ageHours),
                    PriorityRank = GetPriorityRank(ageHours, true),
                    IsStale24Hours = ageHours >= 24,
                    IsStale72Hours = ageHours >= 72,
                    IsCritical = ageHours >= 72,
                    TargetUrl = viewModel.IsAdminUser ? "/Staff/StaffTransferRequestList" : "/Profile/Profile"
                });
            }

            foreach (AuditLogModel log in viewModel.CriticalActivity)
            {
                double ageHours = (DateTime.Now - log.Created).TotalHours;
                items.Add(new ActionCenterItemModel
                {
                    ItemType = "CriticalActivity",
                    ItemID = log.AuditLogID,
                    Title = log.Title,
                    Description = log.Description,
                    Meta = log.Created.ToString("dd MMM yyyy hh:mm tt"),
                    Created = log.Created,
                    StatusLabel = log.ActionType,
                    PriorityLabel = log.ActionType == "LoginFailed" ? "Risk" : "Critical",
                    PriorityRank = log.ActionType == "LoginFailed" ? 100 : 90,
                    IsStale24Hours = ageHours >= 24,
                    IsStale72Hours = ageHours >= 72,
                    IsCritical = true,
                    TargetUrl = "/Activity/ActivityLog"
                });
            }

            return items
                .OrderByDescending(item => item.PriorityRank)
                .ThenBy(item => item.Created)
                .Take(8)
                .ToList();
        }

        private static int GetPriorityRank(double ageHours, bool transferBias)
        {
            if (ageHours >= 72)
            {
                return transferBias ? 85 : 80;
            }

            if (ageHours >= 24)
            {
                return transferBias ? 70 : 65;
            }

            return transferBias ? 55 : 50;
        }

        private static string GetPriorityLabel(double ageHours)
        {
            if (ageHours >= 72)
            {
                return "Overdue 72h";
            }

            if (ageHours >= 24)
            {
                return "Overdue 24h";
            }

            return "Active";
        }
    }
}
