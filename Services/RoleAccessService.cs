using Meeting_Of_Minutes.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;

namespace Meeting_Of_Minutes.Services
{
    public static class RoleAccessService
    {
        public const string SuperAdminRole = "SuperAdmin";
        public const string AdminRole = "Admin";
        public const string UserRole = "User";
        private static readonly HashSet<string> CompanySuperAdminPermissions = new(StringComparer.OrdinalIgnoreCase)
        {
            "dashboard.company","analytics.company","actioncenter.manage","activity.company",
            "departments.manage","staff.manage","meetingtypes.manage","venues.manage","meetings.manage",
            "attendance.manage","profile.self","profile.requests.manage","transfers.manage",
            "imports.manage","importhistory.view","users.manage.company","users.assign.admin",
            "users.resetpassword.global"
        };

        private static readonly object CacheLock = new();
        private static DateTime _cacheExpiresAt = DateTime.MinValue;
        private static Dictionary<string, HashSet<string>>? _cachedPermissionMap;
        private static List<(string PermissionKey, string ModuleName, string Description)>? _cachedCatalog;

        private static readonly Dictionary<string, (string Module, string Description)> DefaultCatalog = new(StringComparer.OrdinalIgnoreCase)
        {
            ["dashboard.global"] = ("Dashboard", "Global operational dashboard across all platform accounts."),
            ["dashboard.company"] = ("Dashboard", "Company-scoped dashboard and operations visibility."),
            ["dashboard.self"] = ("Dashboard", "Personal meeting dashboard and request view."),
            ["analytics.global"] = ("Analytics", "Cross-company analytics and reporting."),
            ["analytics.company"] = ("Analytics", "Company analytics and performance charts."),
            ["actioncenter.manage"] = ("Workflow", "Manage approval queue and operational actions."),
            ["actioncenter.self"] = ("Workflow", "Review personal action queue."),
            ["activity.global"] = ("Audit", "Read global activity and access-risk logs."),
            ["activity.company"] = ("Audit", "Read company audit trail and risk logs."),
            ["activity.self"] = ("Audit", "Read personal activity history."),
            ["departments.manage"] = ("Master Data", "Create, update, import and remove departments."),
            ["staff.manage"] = ("Master Data", "Manage staff records and transfer workflows."),
            ["meetingtypes.manage"] = ("Master Data", "Manage meeting type catalog."),
            ["venues.manage"] = ("Master Data", "Manage meeting venues."),
            ["meetings.manage"] = ("Meetings", "Schedule meetings, edit, cancel and manage attendees."),
            ["attendance.manage"] = ("Meetings", "Track and update attendance records."),
            ["profile.self"] = ("Account", "Update own profile and password."),
            ["profile.requests.manage"] = ("Account", "Approve or reject profile update requests."),
            ["transfers.manage"] = ("Account", "Approve or reject staff transfer requests."),
            ["imports.manage"] = ("Operations", "Bulk import records using Excel templates."),
            ["importhistory.view"] = ("Operations", "Review import history and generated reports."),
            ["users.manage.global"] = ("Access Control", "Manage all users across every company."),
            ["users.manage.company"] = ("Access Control", "Manage users inside current company only."),
            ["users.assign.admin"] = ("Access Control", "Promote, demote or create admin accounts."),
            ["users.resetpassword.global"] = ("Access Control", "Reset passwords across all users."),
            ["roles.manage"] = ("Access Control", "Review and govern system role matrix."),
            ["settings.manage"] = ("Platform", "Maintain system-wide settings and registration rules.")
        };

        public static bool IsSuperAdmin(string? role) => string.Equals(role, SuperAdminRole, StringComparison.OrdinalIgnoreCase);
        public static bool IsAdmin(string? role) => string.Equals(role, AdminRole, StringComparison.OrdinalIgnoreCase);
        public static bool IsAdminOrHigher(string? role) => IsAdmin(role) || IsSuperAdmin(role);

        public static bool HasPermission(string? role, string permissionKey)
        {
            if (string.IsNullOrWhiteSpace(role) || string.IsNullOrWhiteSpace(permissionKey))
            {
                return false;
            }

            if (IsSuperAdmin(role))
            {
                return CompanySuperAdminPermissions.Contains(permissionKey);
            }

            Dictionary<string, HashSet<string>> map = GetPermissionMap();
            return map.TryGetValue(role, out HashSet<string>? permissions) && permissions.Contains(permissionKey);
        }

        public static bool HasPermission(HttpContext httpContext, string permissionKey)
        {
            return HasPermission(httpContext.Session.GetString("UserRole"), permissionKey);
        }

        public static bool CanAccessRoute(HttpContext httpContext, string controllerName, string actionName)
        {
            string role = httpContext.Session.GetString("UserRole") ?? string.Empty;

            string? permissionKey = ResolvePermission(controllerName, actionName);
            return string.IsNullOrWhiteSpace(permissionKey) || HasPermission(role, permissionKey);
        }

        public static string? ResolvePermission(string controllerName, string actionName)
        {
            if (string.Equals(controllerName, "Department", StringComparison.OrdinalIgnoreCase))
            {
                return "departments.manage";
            }

            if (string.Equals(controllerName, "MeetingsType", StringComparison.OrdinalIgnoreCase))
            {
                return "meetingtypes.manage";
            }

            if (string.Equals(controllerName, "MeetingVenue", StringComparison.OrdinalIgnoreCase))
            {
                return "venues.manage";
            }

            if (string.Equals(controllerName, "MeetingMember", StringComparison.OrdinalIgnoreCase))
            {
                return "attendance.manage";
            }

            if (string.Equals(controllerName, "ImportHistory", StringComparison.OrdinalIgnoreCase))
            {
                return "importhistory.view";
            }

            if (string.Equals(controllerName, "RoleManagement", StringComparison.OrdinalIgnoreCase))
            {
                return "roles.manage";
            }

            if (string.Equals(controllerName, "SystemSettings", StringComparison.OrdinalIgnoreCase))
            {
                return "settings.manage";
            }

            if (string.Equals(controllerName, "InviteHistory", StringComparison.OrdinalIgnoreCase))
            {
                return "users.manage.global";
            }

            if (string.Equals(controllerName, "PlatformHealth", StringComparison.OrdinalIgnoreCase))
            {
                return "settings.manage";
            }

            if (string.Equals(controllerName, "UserManagement", StringComparison.OrdinalIgnoreCase))
            {
                return "users.manage.company";
            }

            if (string.Equals(controllerName, "Profile", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(actionName, "Profile", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(actionName, "UpdatePassword", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(actionName, "SubmitProfileUpdateRequest", StringComparison.OrdinalIgnoreCase))
            {
                return "profile.requests.manage";
            }

            if (string.Equals(controllerName, "Staff", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(actionName, "StaffTransferRequestList", StringComparison.OrdinalIgnoreCase))
            {
                return "transfers.manage";
            }

            if (string.Equals(controllerName, "Meetings", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(actionName, "MeetingsList", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(actionName, "MeetingsDetails", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                return "meetings.manage";
            }

            if (string.Equals(controllerName, "Staff", StringComparison.OrdinalIgnoreCase))
            {
                return "staff.manage";
            }

            return null;
        }

        public static RoleManagementViewModel BuildRoleManagementViewModel()
        {
            List<string> roles = GetRoles();
            Dictionary<string, HashSet<string>> permissionMap = GetPermissionMap();
            List<RolePermissionRowModel> rows = new List<RolePermissionRowModel>();

            foreach ((string PermissionKey, string ModuleName, string Description) item in GetCatalog())
            {
                RolePermissionRowModel row = new RolePermissionRowModel
                {
                    PermissionKey = item.PermissionKey,
                    ModuleName = item.ModuleName,
                    Description = item.Description
                };

                foreach (string role in roles)
                {
                    row.RoleAccess[role] = IsSuperAdmin(role)
                        ? CompanySuperAdminPermissions.Contains(item.PermissionKey)
                        : permissionMap.TryGetValue(role, out HashSet<string>? permissions) && permissions.Contains(item.PermissionKey);
                }

                rows.Add(row);
            }

            return new RoleManagementViewModel
            {
                Roles = roles,
                Permissions = rows.OrderBy(x => x.ModuleName).ThenBy(x => x.PermissionKey).ToList()
            };
        }

        public static IReadOnlyList<string> GetEditableRoles()
        {
            return new[] { AdminRole, UserRole };
        }

        public static void SaveRolePermissions(Dictionary<string, List<string>> selectedPermissions)
        {
            using SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            con.Open();
            using SqlTransaction transaction = con.BeginTransaction();

            foreach (string role in GetEditableRoles())
            {
                int roleId = GetRoleId(con, transaction, role);

                using (SqlCommand deleteCmd = new SqlCommand("DELETE FROM MST_RolePermission WHERE RoleID = @RoleID", con, transaction))
                {
                    deleteCmd.Parameters.AddWithValue("@RoleID", roleId);
                    deleteCmd.ExecuteNonQuery();
                }

                List<string> permissions = selectedPermissions.TryGetValue(role, out List<string>? selected)
                    ? selected.Distinct(StringComparer.OrdinalIgnoreCase).ToList()
                    : new List<string>();

                foreach (string permissionKey in permissions)
                {
                    int permissionId = GetPermissionId(con, transaction, permissionKey);
                    using SqlCommand insertCmd = new SqlCommand(@"
                        INSERT INTO MST_RolePermission (RoleID, PermissionID, Created)
                        VALUES (@RoleID, @PermissionID, GETDATE())", con, transaction);
                    insertCmd.Parameters.AddWithValue("@RoleID", roleId);
                    insertCmd.Parameters.AddWithValue("@PermissionID", permissionId);
                    insertCmd.ExecuteNonQuery();
                }
            }

            transaction.Commit();
            ResetPermissionCache();
        }

        public static void ResetPermissionCache()
        {
            lock (CacheLock)
            {
                _cacheExpiresAt = DateTime.MinValue;
                _cachedPermissionMap = null;
                _cachedCatalog = null;
            }
        }

        private static Dictionary<string, HashSet<string>> GetPermissionMap()
        {
            EnsureCache();
            return _cachedPermissionMap ?? BuildDefaultPermissionMap();
        }

        private static IReadOnlyList<(string PermissionKey, string ModuleName, string Description)> GetCatalog()
        {
            EnsureCache();
            return _cachedCatalog ?? BuildDefaultCatalog();
        }

        private static List<string> GetRoles()
        {
            try
            {
                using SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
                using SqlCommand cmd = new SqlCommand("SELECT RoleName FROM MST_Role ORDER BY CASE WHEN RoleName = 'SuperAdmin' THEN 0 WHEN RoleName = 'Admin' THEN 1 ELSE 2 END, RoleName", con);
                con.Open();
                using SqlDataReader reader = cmd.ExecuteReader();
                List<string> roles = new List<string>();
                while (reader.Read())
                {
                    roles.Add(reader["RoleName"].ToString() ?? string.Empty);
                }

                return roles.Count > 0 ? roles : new List<string> { SuperAdminRole, AdminRole, UserRole };
            }
            catch
            {
                return new List<string> { SuperAdminRole, AdminRole, UserRole };
            }
        }

        private static void EnsureCache()
        {
            lock (CacheLock)
            {
                if (_cachedPermissionMap != null && _cachedCatalog != null && _cacheExpiresAt > DateTime.Now)
                {
                    return;
                }

                try
                {
                    using SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
                    con.Open();

                    Dictionary<string, HashSet<string>> permissionMap = new(StringComparer.OrdinalIgnoreCase);
                    using (SqlCommand permissionCmd = new SqlCommand(@"
                        SELECT r.RoleName, p.PermissionKey
                        FROM MST_RolePermission rp
                        INNER JOIN MST_Role r ON r.RoleID = rp.RoleID
                        INNER JOIN MST_Permission p ON p.PermissionID = rp.PermissionID", con))
                    using (SqlDataReader reader = permissionCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string role = reader["RoleName"].ToString() ?? string.Empty;
                            string permission = reader["PermissionKey"].ToString() ?? string.Empty;
                            if (!permissionMap.TryGetValue(role, out HashSet<string>? set))
                            {
                                set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                                permissionMap[role] = set;
                            }

                            set.Add(permission);
                        }
                    }

                    List<(string PermissionKey, string ModuleName, string Description)> catalog = new();
                    using (SqlCommand catalogCmd = new SqlCommand("SELECT PermissionKey, ModuleName, Description FROM MST_Permission ORDER BY ModuleName, PermissionKey", con))
                    using (SqlDataReader reader = catalogCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            catalog.Add((reader["PermissionKey"].ToString() ?? string.Empty, reader["ModuleName"].ToString() ?? string.Empty, reader["Description"].ToString() ?? string.Empty));
                        }
                    }

                    _cachedPermissionMap = permissionMap.Count > 0 ? permissionMap : BuildDefaultPermissionMap();
                    _cachedCatalog = catalog.Count > 0 ? catalog : BuildDefaultCatalog();
                }
                catch
                {
                    _cachedPermissionMap = BuildDefaultPermissionMap();
                    _cachedCatalog = BuildDefaultCatalog();
                }

                _cacheExpiresAt = DateTime.Now.AddMinutes(5);
            }
        }

        private static int GetRoleId(SqlConnection con, SqlTransaction transaction, string roleName)
        {
            using SqlCommand cmd = new SqlCommand("SELECT RoleID FROM MST_Role WHERE RoleName = @RoleName", con, transaction);
            cmd.Parameters.AddWithValue("@RoleName", roleName);
            object? result = cmd.ExecuteScalar();
            if (result == null || result == DBNull.Value)
            {
                throw new InvalidOperationException($"Role '{roleName}' not found.");
            }

            return Convert.ToInt32(result);
        }

        private static int GetPermissionId(SqlConnection con, SqlTransaction transaction, string permissionKey)
        {
            using SqlCommand cmd = new SqlCommand("SELECT PermissionID FROM MST_Permission WHERE PermissionKey = @PermissionKey", con, transaction);
            cmd.Parameters.AddWithValue("@PermissionKey", permissionKey);
            object? result = cmd.ExecuteScalar();
            if (result == null || result == DBNull.Value)
            {
                throw new InvalidOperationException($"Permission '{permissionKey}' not found.");
            }

            return Convert.ToInt32(result);
        }

        private static Dictionary<string, HashSet<string>> BuildDefaultPermissionMap()
        {
            return new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
            {
                [SuperAdminRole] = new HashSet<string>(CompanySuperAdminPermissions, StringComparer.OrdinalIgnoreCase),
                [AdminRole] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "dashboard.company","analytics.company","actioncenter.manage","activity.company",
                    "departments.manage","staff.manage","meetingtypes.manage","venues.manage","meetings.manage",
                    "attendance.manage","profile.self","profile.requests.manage","transfers.manage",
                    "imports.manage","importhistory.view","users.manage.company"
                },
                [UserRole] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "dashboard.self","actioncenter.self","activity.self","profile.self","meetings.view.self"
                }
            };
        }

        private static List<(string PermissionKey, string ModuleName, string Description)> BuildDefaultCatalog()
        {
            return DefaultCatalog
                .Select(x => (x.Key, x.Value.Module, x.Value.Description))
                .OrderBy(x => x.Module)
                .ThenBy(x => x.Key)
                .ToList();
        }
    }
}
