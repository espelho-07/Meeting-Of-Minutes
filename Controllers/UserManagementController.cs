using Meeting_Of_Minutes.Models;
using Meeting_Of_Minutes.Security;
using Meeting_Of_Minutes.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Meeting_Of_Minutes.Controllers
{
    public class UserManagementController : Controller
    {
        [HttpGet]
        public IActionResult Index(string? searchtext, string? roleFilter, string? statusFilter, int page = 1, string sortBy = "name", string sortDirection = "asc")
        {
            if (!CanViewUserManagement())
            {
                return RedirectToAction("DashBoard", "DashBoard");
            }

            searchtext = string.IsNullOrWhiteSpace(searchtext) ? null : searchtext.Trim();
            roleFilter = string.IsNullOrWhiteSpace(roleFilter) ? null : roleFilter.Trim();
            statusFilter = string.IsNullOrWhiteSpace(statusFilter) ? null : statusFilter.Trim();

            ViewBag.SearchText = searchtext;
            ViewBag.RoleFilter = roleFilter ?? string.Empty;
            ViewBag.StatusFilter = statusFilter ?? string.Empty;
            ViewBag.IsSuperAdmin = IsSuperAdminUser();
            ViewBag.IsAdminWorkspace = RoleAccessService.IsAdminOrHigher(HttpContext.Session.GetString("UserRole"));
            ViewBag.AdminOptionsByDepartment = BuildAdminOptionsByDepartment();

            return View(BuildPage(searchtext, roleFilter, statusFilter, page, sortBy, sortDirection));
        }

        [HttpGet]
        public IActionResult Create()
        {
            if (!CanProvisionUsers())
            {
                return RedirectToAction(nameof(Index));
            }

            PopulateCreateDropdowns();
            return View(new ManagedUserCreateModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(ManagedUserCreateModel model)
        {
            if (!CanProvisionUsers())
            {
                return RedirectToAction(nameof(Index));
            }

            NormalizeCreateModel(model);

            if (IsAdminWorkspace())
            {
                model.UserRole = RoleAccessService.UserRole;
                model.CompanyName = GetCurrentCompanyName();
                model.DepartmentID = GetCurrentDepartmentId();
                model.ManagedByAdminUserID = HttpContext.Session.GetInt32("UserID");
            }
            else
            {
                model.CompanyName = GetCurrentCompanyName();
            }

            if (!string.Equals(model.UserRole, RoleAccessService.AdminRole, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(model.UserRole, RoleAccessService.UserRole, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(model.UserRole), "Only Admin or User accounts can be provisioned from this screen.");
            }

            if (!model.DepartmentID.HasValue)
            {
                ModelState.AddModelError(nameof(model.DepartmentID), "Please select department.");
            }

            using SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            con.Open();

            if (!IsDepartmentInCompany(con, model.DepartmentID, model.CompanyName))
            {
                ModelState.AddModelError(nameof(model.DepartmentID), "Select a valid department from your company.");
            }

            if (string.Equals(model.UserRole, RoleAccessService.AdminRole, StringComparison.OrdinalIgnoreCase) &&
                HasCompanySuperAdmin(con, model.CompanyName, null))
            {
                // no-op: admins are allowed even if super admin exists; this keeps logic explicit
            }

            if (string.Equals(model.UserRole, RoleAccessService.UserRole, StringComparison.OrdinalIgnoreCase) &&
                !IsValidManagedAdmin(con, model.CompanyName, model.DepartmentID, model.ManagedByAdminUserID, null))
            {
                ModelState.AddModelError(nameof(model.ManagedByAdminUserID), "Select an active admin from the same company and department.");
            }

            if (string.Equals(model.UserRole, RoleAccessService.AdminRole, StringComparison.OrdinalIgnoreCase) &&
                !IsSuperAdminUser())
            {
                ModelState.AddModelError(nameof(model.UserRole), "Admins can only create standard users for their own department.");
            }

            if (!ModelState.IsValid)
            {
                PopulateCreateDropdowns();
                return View(model);
            }

            if (UserExists(con, model.Email, model.UserName))
            {
                ModelState.AddModelError(string.Empty, "Email or username already exists.");
                PopulateCreateDropdowns();
                return View(model);
            }

            string temporaryPassword = PasswordSecurity.GenerateTemporaryPassword();

            using SqlCommand cmd = new SqlCommand(@"
                INSERT INTO MST_User
                (UserName, Email, Password, PasswordHash, ContactNo, City, UserRole, CompanyName, StaffID, DepartmentID, ManagedByAdminUserID, IsAutoPassword, IsActive, Created, Modified)
                VALUES
                (@UserName, @Email, @Password, @PasswordHash, @ContactNo, @City, @UserRole, @CompanyName, NULL, @DepartmentID, @ManagedByAdminUserID, 1, @IsActive, GETDATE(), @Modified);
                SELECT CAST(SCOPE_IDENTITY() AS INT);", con);
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.AddWithValue("@UserName", model.UserName);
            cmd.Parameters.AddWithValue("@Email", model.Email);
            cmd.Parameters.AddWithValue("@Password", string.Empty);
            cmd.Parameters.AddWithValue("@PasswordHash", PasswordSecurity.HashPassword(temporaryPassword));
            cmd.Parameters.AddWithValue("@ContactNo", model.ContactNo);
            cmd.Parameters.AddWithValue("@City", model.City);
            cmd.Parameters.AddWithValue("@UserRole", model.UserRole);
            cmd.Parameters.AddWithValue("@CompanyName", model.CompanyName);
            cmd.Parameters.AddWithValue("@DepartmentID", model.DepartmentID.HasValue ? model.DepartmentID.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@ManagedByAdminUserID", model.ManagedByAdminUserID.HasValue ? model.ManagedByAdminUserID.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@IsActive", model.IsActive);
            cmd.Parameters.AddWithValue("@Modified", DateTime.Now);
            int createdUserId = Convert.ToInt32(cmd.ExecuteScalar());

            string ownerMessage = string.Equals(model.UserRole, RoleAccessService.UserRole, StringComparison.OrdinalIgnoreCase) && model.ManagedByAdminUserID.HasValue
                ? $" Assigned admin #{model.ManagedByAdminUserID.Value}."
                : string.Empty;

            UserInviteDeliveryResult inviteResult = UserInviteService.CreateAndDeliverInvite(
                createdUserId,
                model.UserName,
                model.Email,
                model.CompanyName,
                model.UserRole,
                HttpContext.Session.GetString("UserName") ?? "Super Admin");

            AuditLogService.Log(
                GetActorCompanyName(),
                HttpContext.Session.GetInt32("UserID"),
                HttpContext.Session.GetString("UserName"),
                HttpContext.Session.GetString("UserRole"),
                "UserCreate",
                "User",
                null,
                "Managed user created",
                $"{model.UserRole} account created for {model.Email} in {model.CompanyName}.{ownerMessage}");

            TempData["SuccessMessage"] = $"{model.UserRole} account created and invite delivered via {inviteResult.DeliveryChannel}.";
            if (!string.IsNullOrWhiteSpace(inviteResult.PreviewPath))
            {
                TempData["InfoMessage"] = "SMTP is not configured, so an invite preview file was generated on the server.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateRole(int userId, string userRole, string? returnUrl)
        {
            if (!IsSuperAdminUser())
            {
                TempData["ErrorMessage"] = "Only Super Admin can change admin-level role assignments.";
                return RedirectToLocal(returnUrl);
            }

            if (!string.Equals(userRole, RoleAccessService.AdminRole, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(userRole, RoleAccessService.UserRole, StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Invalid role selected.";
                return RedirectToLocal(returnUrl);
            }

            int? currentUserId = HttpContext.Session.GetInt32("UserID");
            if (currentUserId.HasValue && currentUserId.Value == userId && string.Equals(userRole, RoleAccessService.UserRole, StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "You cannot downgrade your own elevated account.";
                return RedirectToLocal(returnUrl);
            }

            using SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            con.Open();

            UserManagementModel? user = GetUserById(con, userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found in current scope.";
                return RedirectToLocal(returnUrl);
            }

            if (string.Equals(user.UserRole, RoleAccessService.SuperAdminRole, StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Super Admin roles cannot be changed from this screen.";
                return RedirectToLocal(returnUrl);
            }

            if (string.Equals(userRole, RoleAccessService.UserRole, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(user.UserRole, RoleAccessService.AdminRole, StringComparison.OrdinalIgnoreCase) &&
                IsLastActiveAdmin(con, userId, user.CompanyName))
            {
                TempData["ErrorMessage"] = "At least one active admin must remain in that company.";
                return RedirectToLocal(returnUrl);
            }

            int? managedByAdminUserId = user.ManagedByAdminUserID;
            int? departmentId = user.DepartmentID;

            if (string.Equals(userRole, RoleAccessService.AdminRole, StringComparison.OrdinalIgnoreCase))
            {
                managedByAdminUserId = null;
                if (!departmentId.HasValue)
                {
                    TempData["ErrorMessage"] = "Assign the user to a department before promoting to Admin.";
                    return RedirectToLocal(returnUrl);
                }
            }
            else
            {
                managedByAdminUserId = ResolveFallbackAdminForCompany(con, user.CompanyName, user.DepartmentID, userId);
                if (!managedByAdminUserId.HasValue)
                {
                    TempData["ErrorMessage"] = "Another active admin from the same department is required before this account can be downgraded to user.";
                    return RedirectToLocal(returnUrl);
                }
            }

            using SqlCommand cmd = new SqlCommand(@"UPDATE MST_User
                                                    SET UserRole = @UserRole,
                                                        DepartmentID = @DepartmentID,
                                                        ManagedByAdminUserID = @ManagedByAdminUserID,
                                                        Modified = @Modified
                                                    WHERE UserID = @UserID", con);
            cmd.Parameters.AddWithValue("@UserRole", userRole);
            cmd.Parameters.AddWithValue("@DepartmentID", departmentId.HasValue ? departmentId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@ManagedByAdminUserID", managedByAdminUserId.HasValue ? managedByAdminUserId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@Modified", DateTime.Now);
            cmd.Parameters.AddWithValue("@UserID", userId);
            cmd.ExecuteNonQuery();

            AuditLogService.Log(user.CompanyName, HttpContext.Session.GetInt32("UserID"), HttpContext.Session.GetString("UserName"), HttpContext.Session.GetString("UserRole"), "RoleUpdate", "User", userId.ToString(), "User role updated", $"User #{userId} role changed to {userRole}.");
            TempData["SuccessMessage"] = "User role updated successfully.";
            return RedirectToLocal(returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateManagedAdmin(int userId, int? managedByAdminUserId, string? returnUrl)
        {
            if (!IsSuperAdminUser())
            {
                TempData["ErrorMessage"] = "Only Super Admin can change user ownership.";
                return RedirectToLocal(returnUrl);
            }

            using SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            con.Open();

            UserManagementModel? user = GetUserById(con, userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found in current scope.";
                return RedirectToLocal(returnUrl);
            }

            if (!string.Equals(user.UserRole, RoleAccessService.UserRole, StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Only standard users can be assigned under an admin owner.";
                return RedirectToLocal(returnUrl);
            }

            if (!IsValidManagedAdmin(con, user.CompanyName, user.DepartmentID, managedByAdminUserId, userId))
            {
                TempData["ErrorMessage"] = "Select an active admin from the same company and department.";
                return RedirectToLocal(returnUrl);
            }

            using SqlCommand cmd = new SqlCommand(@"UPDATE MST_User
                                                    SET ManagedByAdminUserID = @ManagedByAdminUserID,
                                                        Modified = @Modified
                                                    WHERE UserID = @UserID", con);
            cmd.Parameters.AddWithValue("@ManagedByAdminUserID", managedByAdminUserId.HasValue ? managedByAdminUserId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@Modified", DateTime.Now);
            cmd.Parameters.AddWithValue("@UserID", userId);
            cmd.ExecuteNonQuery();

            string managerName = GetUserNameById(con, managedByAdminUserId);
            AuditLogService.Log(user.CompanyName, HttpContext.Session.GetInt32("UserID"), HttpContext.Session.GetString("UserName"), HttpContext.Session.GetString("UserRole"), "ManagerAssign", "User", userId.ToString(), "User manager updated", $"User #{userId} assigned to admin {managerName}.");
            TempData["SuccessMessage"] = "Assigned admin updated successfully.";
            return RedirectToLocal(returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleActive(int userId, string? returnUrl)
        {
            if (!CanViewUserManagement())
            {
                return RedirectToAction("DashBoard", "DashBoard");
            }

            int? currentUserId = HttpContext.Session.GetInt32("UserID");
            if (currentUserId.HasValue && currentUserId.Value == userId)
            {
                TempData["ErrorMessage"] = "You cannot deactivate your own account from this screen.";
                return RedirectToLocal(returnUrl);
            }

            using SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            con.Open();

            UserManagementModel? user = GetUserById(con, userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found in current scope.";
                return RedirectToLocal(returnUrl);
            }

            if (!IsSuperAdminUser())
            {
                if (!string.Equals(user.UserRole, RoleAccessService.UserRole, StringComparison.OrdinalIgnoreCase))
                {
                    TempData["ErrorMessage"] = "Admins can only activate or deactivate standard users assigned to them.";
                    return RedirectToLocal(returnUrl);
                }

                if (!currentUserId.HasValue || user.ManagedByAdminUserID != currentUserId.Value || user.DepartmentID != GetCurrentDepartmentId())
                {
                    TempData["ErrorMessage"] = "You can only manage users assigned to your department workspace.";
                    return RedirectToLocal(returnUrl);
                }
            }

            bool nextActiveState = !user.IsActive;
            if (!nextActiveState && string.Equals(user.UserRole, RoleAccessService.AdminRole, StringComparison.OrdinalIgnoreCase) && IsLastActiveAdmin(con, userId, user.CompanyName))
            {
                TempData["ErrorMessage"] = "At least one active admin must remain in that company.";
                return RedirectToLocal(returnUrl);
            }

            if (!nextActiveState && string.Equals(user.UserRole, RoleAccessService.SuperAdminRole, StringComparison.OrdinalIgnoreCase) && IsLastActiveSuperAdmin(con, userId, user.CompanyName))
            {
                TempData["ErrorMessage"] = "At least one active Super Admin must remain in that company.";
                return RedirectToLocal(returnUrl);
            }

            using SqlCommand cmd = new SqlCommand(@"UPDATE MST_User
                                                    SET IsActive = @IsActive,
                                                        Modified = @Modified
                                                    WHERE UserID = @UserID", con);
            cmd.Parameters.AddWithValue("@IsActive", nextActiveState);
            cmd.Parameters.AddWithValue("@Modified", DateTime.Now);
            cmd.Parameters.AddWithValue("@UserID", userId);
            cmd.ExecuteNonQuery();

            AuditLogService.Log(user.CompanyName, HttpContext.Session.GetInt32("UserID"), HttpContext.Session.GetString("UserName"), HttpContext.Session.GetString("UserRole"), nextActiveState ? "Activate" : "Deactivate", "User", userId.ToString(), nextActiveState ? "User activated" : "User deactivated", $"User #{userId} was {(nextActiveState ? "activated" : "deactivated")}.");
            TempData["SuccessMessage"] = nextActiveState ? "User activated successfully." : "User deactivated successfully.";
            return RedirectToLocal(returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResetPassword(int userId, string? returnUrl)
        {
            if (!IsSuperAdminUser())
            {
                TempData["ErrorMessage"] = "Only Super Admin can reset passwords from this screen.";
                return RedirectToLocal(returnUrl);
            }

            using SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            con.Open();

            UserManagementModel? user = GetUserById(con, userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found in current scope.";
                return RedirectToLocal(returnUrl);
            }

            string temporaryPassword = PasswordSecurity.GenerateTemporaryPassword();

            using SqlCommand cmd = new SqlCommand(@"
                UPDATE MST_User
                SET Password = @Password,
                    PasswordHash = @PasswordHash,
                    IsAutoPassword = 1,
                    IsActive = 1,
                    Modified = @Modified
                WHERE UserID = @UserID", con);
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.AddWithValue("@Password", string.Empty);
            cmd.Parameters.AddWithValue("@PasswordHash", PasswordSecurity.HashPassword(temporaryPassword));
            cmd.Parameters.AddWithValue("@Modified", DateTime.Now);
            cmd.Parameters.AddWithValue("@UserID", userId);
            cmd.ExecuteNonQuery();

            AuditLogService.Log(
                user.CompanyName,
                HttpContext.Session.GetInt32("UserID"),
                HttpContext.Session.GetString("UserName"),
                HttpContext.Session.GetString("UserRole"),
                "PasswordReset",
                "User",
                userId.ToString(),
                "User password reset",
                $"Temporary password was generated for user #{userId}.");

            TempData["SuccessMessage"] = $"Temporary password generated for {user.UserName}: {temporaryPassword}. The user will be forced to change it on next login.";
            return RedirectToLocal(returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResendInvite(int userId, string? returnUrl)
        {
            if (!IsSuperAdminUser())
            {
                TempData["ErrorMessage"] = "Only Super Admin can resend onboarding invites.";
                return RedirectToLocal(returnUrl);
            }

            using SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            con.Open();

            UserManagementModel? user = GetUserById(con, userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found in current scope.";
                return RedirectToLocal(returnUrl);
            }

            UserInviteDeliveryResult inviteResult = UserInviteService.CreateAndDeliverInvite(
                user.UserID,
                user.UserName,
                user.Email,
                user.CompanyName,
                user.UserRole,
                HttpContext.Session.GetString("UserName") ?? "Super Admin");

            TempData["SuccessMessage"] = $"Invite resent for {user.UserName} via {inviteResult.DeliveryChannel}.";
            if (!string.IsNullOrWhiteSpace(inviteResult.PreviewPath))
            {
                TempData["InfoMessage"] = "Invite preview file generated on the server.";
            }

            return RedirectToLocal(returnUrl);
        }

        [HttpGet]
        public JsonResult GetAdminsByCompany(string? companyName, int? departmentId)
        {
            if (!IsSuperAdminUser())
            {
                return Json(Array.Empty<object>());
            }

            string company = (companyName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(company) || !string.Equals(company, GetCurrentCompanyName(), StringComparison.OrdinalIgnoreCase))
            {
                return Json(Array.Empty<object>());
            }

            return Json(FillManagedAdminDropDown(company, departmentId).Select(x => new { value = x.Value, text = x.Text }));
        }

        private PagedListViewModel<UserManagementModel> BuildPage(string? searchtext, string? roleFilter, string? statusFilter, int page, string? sortBy, string? sortDirection)
        {
            IEnumerable<UserManagementModel> query = GetUsers(searchtext, roleFilter, statusFilter);
            bool isDesc = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

            query = (sortBy ?? "name").ToLowerInvariant() switch
            {
                "role" => isDesc ? query.OrderByDescending(x => x.UserRole).ThenBy(x => x.UserName) : query.OrderBy(x => x.UserRole).ThenBy(x => x.UserName),
                "status" => isDesc ? query.OrderByDescending(x => x.IsActive).ThenBy(x => x.UserName) : query.OrderBy(x => x.IsActive).ThenBy(x => x.UserName),
                "department" => isDesc ? query.OrderByDescending(x => x.DepartmentName).ThenBy(x => x.UserName) : query.OrderBy(x => x.DepartmentName).ThenBy(x => x.UserName),
                "company" => isDesc ? query.OrderByDescending(x => x.CompanyName).ThenBy(x => x.UserName) : query.OrderBy(x => x.CompanyName).ThenBy(x => x.UserName),
                "manager" => isDesc ? query.OrderByDescending(x => x.ManagedByAdminName).ThenBy(x => x.UserName) : query.OrderBy(x => x.ManagedByAdminName).ThenBy(x => x.UserName),
                _ => isDesc ? query.OrderByDescending(x => x.UserName) : query.OrderBy(x => x.UserName)
            };

            List<UserManagementModel> ordered = query.ToList();
            const int pageSize = 12;
            page = Math.Max(page, 1);

            return new PagedListViewModel<UserManagementModel>
            {
                Items = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
                PageNumber = page,
                PageSize = pageSize,
                TotalItems = ordered.Count,
                SearchText = searchtext,
                SortBy = sortBy,
                SortDirection = sortDirection
            };
        }

        private List<UserManagementModel> GetUsers(string? searchtext, string? roleFilter, string? statusFilter)
        {
            List<UserManagementModel> users = new List<UserManagementModel>();
            int currentUserId = HttpContext.Session.GetInt32("UserID") ?? 0;
            string companyName = GetCurrentCompanyName();
            int? currentDepartmentId = GetCurrentDepartmentId();

            string sql = @"
                SELECT u.UserID, u.UserName, u.Email, u.ContactNo, u.UserRole, u.CompanyName,
                       u.DepartmentID, ISNULL(d.DepartmentName, '') AS DepartmentName,
                       u.IsActive, u.StaffID, u.IsAutoPassword, u.ManagedByAdminUserID,
                       ISNULL(adminOwner.UserName, '') AS ManagedByAdminName,
                       u.Created, u.Modified
                FROM MST_User u
                LEFT JOIN MOM_Department d ON d.DepartmentID = u.DepartmentID
                LEFT JOIN MST_User adminOwner ON adminOwner.UserID = u.ManagedByAdminUserID
                WHERE 1 = 1";

            if (IsSuperAdminUser())
            {
                sql += " AND u.CompanyName = @CompanyName";
            }
            else
            {
                sql += " AND u.CompanyName = @CompanyName AND (u.UserID = @CurrentUserID OR (u.UserRole = @UserRole AND u.DepartmentID = @DepartmentID AND u.ManagedByAdminUserID = @CurrentUserID))";
            }

            using SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            using SqlCommand cmd = new SqlCommand(sql, con);
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.AddWithValue("@CurrentUserID", currentUserId);
            cmd.Parameters.AddWithValue("@CompanyName", companyName);
            if (!IsSuperAdminUser())
            {
                cmd.Parameters.AddWithValue("@UserRole", RoleAccessService.UserRole);
                cmd.Parameters.AddWithValue("@DepartmentID", currentDepartmentId.HasValue ? currentDepartmentId.Value : DBNull.Value);
            }
            con.Open();

            using SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                UserManagementModel user = new UserManagementModel
                {
                    UserID = Convert.ToInt32(reader["UserID"]),
                    UserName = reader["UserName"].ToString() ?? string.Empty,
                    Email = reader["Email"].ToString() ?? string.Empty,
                    ContactNo = reader["ContactNo"].ToString() ?? string.Empty,
                    UserRole = reader["UserRole"].ToString() ?? string.Empty,
                    CompanyName = reader["CompanyName"].ToString() ?? string.Empty,
                    DepartmentID = reader["DepartmentID"] == DBNull.Value ? null : Convert.ToInt32(reader["DepartmentID"]),
                    DepartmentName = reader["DepartmentName"].ToString() ?? string.Empty,
                    IsActive = reader["IsActive"] != DBNull.Value && Convert.ToBoolean(reader["IsActive"]),
                    StaffID = reader["StaffID"] == DBNull.Value ? null : Convert.ToInt32(reader["StaffID"]),
                    IsAutoPassword = reader["IsAutoPassword"] != DBNull.Value && Convert.ToBoolean(reader["IsAutoPassword"]),
                    ManagedByAdminUserID = reader["ManagedByAdminUserID"] == DBNull.Value ? null : Convert.ToInt32(reader["ManagedByAdminUserID"]),
                    ManagedByAdminName = reader["ManagedByAdminName"].ToString() ?? string.Empty,
                    Created = Convert.ToDateTime(reader["Created"]),
                    Modified = Convert.ToDateTime(reader["Modified"])
                };

                if (!string.IsNullOrWhiteSpace(searchtext))
                {
                    string search = searchtext.Trim();
                    bool matchesSearch =
                        user.UserName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        user.Email.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        user.DepartmentName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        user.CompanyName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        user.ManagedByAdminName.Contains(search, StringComparison.OrdinalIgnoreCase);

                    if (!matchesSearch)
                    {
                        continue;
                    }
                }

                if (!string.IsNullOrWhiteSpace(roleFilter) && !string.Equals(user.UserRole, roleFilter, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(statusFilter))
                {
                    bool expectedStatus = string.Equals(statusFilter, "active", StringComparison.OrdinalIgnoreCase);
                    if (user.IsActive != expectedStatus)
                    {
                        continue;
                    }
                }

                users.Add(user);
            }

            return users;
        }

        private UserManagementModel? GetUserById(SqlConnection con, int userId)
        {
            int currentUserId = HttpContext.Session.GetInt32("UserID") ?? 0;
            string companyName = GetCurrentCompanyName();
            int? currentDepartmentId = GetCurrentDepartmentId();

            string sql = @"
                SELECT TOP 1 u.UserID, u.UserName, u.Email, u.ContactNo, u.UserRole, u.CompanyName,
                             u.DepartmentID, ISNULL(d.DepartmentName, '') AS DepartmentName,
                             u.IsActive, u.StaffID, u.IsAutoPassword, u.ManagedByAdminUserID,
                             ISNULL(adminOwner.UserName, '') AS ManagedByAdminName,
                             u.Created, u.Modified
                FROM MST_User u
                LEFT JOIN MOM_Department d ON d.DepartmentID = u.DepartmentID
                LEFT JOIN MST_User adminOwner ON adminOwner.UserID = u.ManagedByAdminUserID
                WHERE u.UserID = @UserID";

            if (IsSuperAdminUser())
            {
                sql += " AND u.CompanyName = @CompanyName";
            }
            else
            {
                sql += " AND u.CompanyName = @CompanyName AND (u.UserID = @CurrentUserID OR (u.UserRole = @UserRole AND u.DepartmentID = @DepartmentID AND u.ManagedByAdminUserID = @CurrentUserID))";
            }

            using SqlCommand cmd = new SqlCommand(sql, con);
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.AddWithValue("@UserID", userId);
            cmd.Parameters.AddWithValue("@CurrentUserID", currentUserId);
            cmd.Parameters.AddWithValue("@CompanyName", companyName);
            if (!IsSuperAdminUser())
            {
                cmd.Parameters.AddWithValue("@UserRole", RoleAccessService.UserRole);
                cmd.Parameters.AddWithValue("@DepartmentID", currentDepartmentId.HasValue ? currentDepartmentId.Value : DBNull.Value);
            }

            using SqlDataReader reader = cmd.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            return new UserManagementModel
            {
                UserID = Convert.ToInt32(reader["UserID"]),
                UserName = reader["UserName"].ToString() ?? string.Empty,
                Email = reader["Email"].ToString() ?? string.Empty,
                ContactNo = reader["ContactNo"].ToString() ?? string.Empty,
                UserRole = reader["UserRole"].ToString() ?? string.Empty,
                CompanyName = reader["CompanyName"].ToString() ?? string.Empty,
                DepartmentID = reader["DepartmentID"] == DBNull.Value ? null : Convert.ToInt32(reader["DepartmentID"]),
                DepartmentName = reader["DepartmentName"].ToString() ?? string.Empty,
                IsActive = reader["IsActive"] != DBNull.Value && Convert.ToBoolean(reader["IsActive"]),
                StaffID = reader["StaffID"] == DBNull.Value ? null : Convert.ToInt32(reader["StaffID"]),
                IsAutoPassword = reader["IsAutoPassword"] != DBNull.Value && Convert.ToBoolean(reader["IsAutoPassword"]),
                ManagedByAdminUserID = reader["ManagedByAdminUserID"] == DBNull.Value ? null : Convert.ToInt32(reader["ManagedByAdminUserID"]),
                ManagedByAdminName = reader["ManagedByAdminName"].ToString() ?? string.Empty,
                Created = Convert.ToDateTime(reader["Created"]),
                Modified = Convert.ToDateTime(reader["Modified"])
            };
        }

        private Dictionary<int, List<SelectListItem>> BuildAdminOptionsByDepartment()
        {
            Dictionary<int, List<SelectListItem>> map = new Dictionary<int, List<SelectListItem>>();
            using SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            using SqlCommand cmd = new SqlCommand("SELECT DepartmentID FROM MOM_Department WHERE CompanyName = @CompanyName ORDER BY DepartmentName", con);
            cmd.Parameters.AddWithValue("@CompanyName", GetCurrentCompanyName());
            con.Open();
            using SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                int departmentId = Convert.ToInt32(reader["DepartmentID"]);
                map[departmentId] = FillManagedAdminDropDown(GetCurrentCompanyName(), departmentId);
            }

            return map;
        }

        private bool IsLastActiveAdmin(SqlConnection con, int userId, string companyName)
        {
            using SqlCommand cmd = new SqlCommand(@"
                SELECT COUNT(*)
                FROM MST_User
                WHERE CompanyName = @CompanyName
                  AND UserRole = @UserRole
                  AND IsActive = 1
                  AND UserID <> @UserID", con);
            cmd.Parameters.AddWithValue("@CompanyName", companyName);
            cmd.Parameters.AddWithValue("@UserRole", RoleAccessService.AdminRole);
            cmd.Parameters.AddWithValue("@UserID", userId);
            return Convert.ToInt32(cmd.ExecuteScalar()) == 0;
        }

        private bool IsLastActiveSuperAdmin(SqlConnection con, int userId, string companyName)
        {
            using SqlCommand cmd = new SqlCommand(@"
                SELECT COUNT(*)
                FROM MST_User
                WHERE UserRole = @UserRole
                  AND CompanyName = @CompanyName
                  AND IsActive = 1
                  AND UserID <> @UserID", con);
            cmd.Parameters.AddWithValue("@UserRole", RoleAccessService.SuperAdminRole);
            cmd.Parameters.AddWithValue("@CompanyName", companyName);
            cmd.Parameters.AddWithValue("@UserID", userId);
            return Convert.ToInt32(cmd.ExecuteScalar()) == 0;
        }

        private bool CanViewUserManagement()
        {
            return RoleAccessService.HasPermission(HttpContext, "users.manage.company") ||
                   RoleAccessService.HasPermission(HttpContext, "users.manage.global");
        }

        private bool CanProvisionUsers()
        {
            return IsSuperAdminUser() || IsAdminWorkspace();
        }

        private bool IsSuperAdminUser()
        {
            return RoleAccessService.IsSuperAdmin(HttpContext.Session.GetString("UserRole"));
        }

        private bool IsAdminWorkspace()
        {
            return RoleAccessService.IsAdmin(HttpContext.Session.GetString("UserRole"));
        }

        private string GetActorCompanyName()
        {
            return HttpContext.Session.GetString("CompanyName") ?? "Platform";
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }

        private bool UserExists(SqlConnection con, string email, string userName)
        {
            using SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM MST_User WHERE Email = @Email OR UserName = @UserName", con);
            cmd.Parameters.AddWithValue("@Email", email);
            cmd.Parameters.AddWithValue("@UserName", userName);
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        private bool IsValidManagedAdmin(SqlConnection con, string companyName, int? departmentId, int? managedByAdminUserId, int? excludeUserId)
        {
            if (!managedByAdminUserId.HasValue || string.IsNullOrWhiteSpace(companyName) || !departmentId.HasValue)
            {
                return false;
            }

            using SqlCommand cmd = new SqlCommand(@"
                SELECT COUNT(*)
                FROM MST_User
                WHERE UserID = @UserID
                  AND CompanyName = @CompanyName
                  AND DepartmentID = @DepartmentID
                  AND UserRole = @UserRole
                  AND IsActive = 1
                  AND (@ExcludeUserID IS NULL OR UserID <> @ExcludeUserID)", con);
            cmd.Parameters.AddWithValue("@UserID", managedByAdminUserId.Value);
            cmd.Parameters.AddWithValue("@CompanyName", companyName);
            cmd.Parameters.AddWithValue("@DepartmentID", departmentId.Value);
            cmd.Parameters.AddWithValue("@UserRole", RoleAccessService.AdminRole);
            cmd.Parameters.AddWithValue("@ExcludeUserID", excludeUserId.HasValue ? excludeUserId.Value : DBNull.Value);
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        private int? ResolveFallbackAdminForCompany(SqlConnection con, string companyName, int? departmentId, int excludedUserId)
        {
            if (!departmentId.HasValue)
            {
                return null;
            }

            using SqlCommand cmd = new SqlCommand(@"
                SELECT TOP 1 UserID
                FROM MST_User
                WHERE CompanyName = @CompanyName
                  AND DepartmentID = @DepartmentID
                  AND UserRole = @UserRole
                  AND IsActive = 1
                  AND UserID <> @ExcludedUserID
                ORDER BY UserName", con);
            cmd.Parameters.AddWithValue("@CompanyName", companyName);
            cmd.Parameters.AddWithValue("@DepartmentID", departmentId.Value);
            cmd.Parameters.AddWithValue("@UserRole", RoleAccessService.AdminRole);
            cmd.Parameters.AddWithValue("@ExcludedUserID", excludedUserId);
            object? result = cmd.ExecuteScalar();
            return result == null || result == DBNull.Value ? null : Convert.ToInt32(result);
        }

        private int? ResolveDefaultDepartmentId(SqlConnection con, string companyName)
        {
            using SqlCommand cmd = new SqlCommand(@"
                SELECT TOP 1 DepartmentID
                FROM MOM_Department
                WHERE CompanyName = @CompanyName
                ORDER BY DepartmentName", con);
            cmd.Parameters.AddWithValue("@CompanyName", companyName);
            object? result = cmd.ExecuteScalar();
            return result == null || result == DBNull.Value ? null : Convert.ToInt32(result);
        }

        private string GetUserNameById(SqlConnection con, int? userId)
        {
            if (!userId.HasValue)
            {
                return "Unassigned";
            }

            using SqlCommand cmd = new SqlCommand("SELECT TOP 1 UserName FROM MST_User WHERE UserID = @UserID", con);
            cmd.Parameters.AddWithValue("@UserID", userId.Value);
            object? result = cmd.ExecuteScalar();
            return result?.ToString() ?? "Unknown";
        }

        private void NormalizeCreateModel(ManagedUserCreateModel model)
        {
            model.UserName = (model.UserName ?? string.Empty).Trim();
            model.Email = (model.Email ?? string.Empty).Trim();
            model.ContactNo = (model.ContactNo ?? string.Empty).Trim();
            model.City = (model.City ?? string.Empty).Trim();
            model.CompanyName = (model.CompanyName ?? string.Empty).Trim();
            model.UserRole = (model.UserRole ?? string.Empty).Trim();

            if (string.Equals(model.UserRole, RoleAccessService.AdminRole, StringComparison.OrdinalIgnoreCase))
            {
                model.ManagedByAdminUserID = null;
            }
        }

        private void PopulateCreateDropdowns(string? selectedCompany = null)
        {
            string companyName = GetCurrentCompanyName();
            int? departmentId = GetCurrentDepartmentId();
            ViewBag.CompanyDropDown = FillCompanyDropDown();
            ViewBag.DepartmentDropDown = FillDepartmentDropDown(companyName);
            ViewBag.ManagedAdminDropDown = FillManagedAdminDropDown(companyName, IsAdminWorkspace() ? departmentId : null);
        }

        private List<SelectListItem> FillCompanyDropDown()
        {
            string companyName = GetCurrentCompanyName();
            return string.IsNullOrWhiteSpace(companyName)
                ? new List<SelectListItem>()
                : new List<SelectListItem> { new SelectListItem { Value = companyName, Text = companyName } };
        }

        private List<SelectListItem> FillDepartmentDropDown(string? companyName = null)
        {
            List<SelectListItem> list = new List<SelectListItem>();
            using SqlConnection sqlConnection = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            using SqlCommand sqlCommand = sqlConnection.CreateCommand();
            sqlCommand.CommandType = CommandType.StoredProcedure;
            sqlCommand.CommandText = "PR_MOM_DEPARTMENT_DDL";
            sqlCommand.Parameters.AddWithValue("@CompanyName", string.IsNullOrWhiteSpace(companyName) ? DBNull.Value : companyName);
            sqlConnection.Open();

            using SqlDataReader reader = sqlCommand.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new SelectListItem
                {
                    Value = reader["DepartmentID"].ToString(),
                    Text = reader["DepartmentName"].ToString()
                });
            }

            return list;
        }

        private List<SelectListItem> FillManagedAdminDropDown(string? companyName = null, int? departmentId = null)
        {
            List<SelectListItem> list = new List<SelectListItem>();
            if (string.IsNullOrWhiteSpace(companyName))
            {
                return list;
            }

            using SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            using SqlCommand cmd = new SqlCommand(@"
                SELECT UserID, UserName
                FROM MST_User
                WHERE CompanyName = @CompanyName
                  AND (@DepartmentID IS NULL OR DepartmentID = @DepartmentID)
                  AND UserRole = @UserRole
                  AND IsActive = 1
                ORDER BY UserName", con);
            cmd.Parameters.AddWithValue("@CompanyName", companyName);
            cmd.Parameters.AddWithValue("@DepartmentID", departmentId.HasValue ? departmentId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@UserRole", RoleAccessService.AdminRole);
            con.Open();

            using SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new SelectListItem
                {
                    Value = reader["UserID"].ToString(),
                    Text = reader["UserName"].ToString()
                });
            }

            return list;
        }

        private string GetCurrentCompanyName()
        {
            return (HttpContext.Session.GetString("CompanyName") ?? string.Empty).Trim();
        }

        private int? GetCurrentDepartmentId()
        {
            return HttpContext.Session.GetInt32("DepartmentID");
        }

        private bool IsDepartmentInCompany(SqlConnection con, int? departmentId, string companyName)
        {
            if (!departmentId.HasValue || string.IsNullOrWhiteSpace(companyName))
            {
                return false;
            }

            using SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM MOM_Department WHERE DepartmentID = @DepartmentID AND CompanyName = @CompanyName", con);
            cmd.Parameters.AddWithValue("@DepartmentID", departmentId.Value);
            cmd.Parameters.AddWithValue("@CompanyName", companyName);
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        private bool HasCompanySuperAdmin(SqlConnection con, string companyName, int? excludeUserId)
        {
            using SqlCommand cmd = new SqlCommand(@"
                SELECT COUNT(*)
                FROM MST_User
                WHERE CompanyName = @CompanyName
                  AND UserRole = @UserRole
                  AND IsActive = 1
                  AND (@ExcludeUserID IS NULL OR UserID <> @ExcludeUserID)", con);
            cmd.Parameters.AddWithValue("@CompanyName", companyName);
            cmd.Parameters.AddWithValue("@UserRole", RoleAccessService.SuperAdminRole);
            cmd.Parameters.AddWithValue("@ExcludeUserID", excludeUserId.HasValue ? excludeUserId.Value : DBNull.Value);
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }
    }
}
