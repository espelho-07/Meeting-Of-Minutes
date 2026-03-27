using Meeting_Of_Minutes.Models;
using Meeting_Of_Minutes.Security;
using Meeting_Of_Minutes.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Meeting_Of_Minutes.Controllers
{
    [AllowAnonymous]
    public class AuthController : Controller
    {
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("UserName") != null)
            {
                return RedirectToAction("DashBoard", "DashBoard");
            }

            ViewBag.CompanyDropDown = FillCompanyDropDown();
            ViewBag.DepartmentDropDown = FillDepartmentDropDown();
            return View(new UserModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(UserModel model)
        {
            model.Email = model.Email?.Trim();
            model.CompanyName = model.CompanyName?.Trim();
            model.UserRole = model.UserRole?.Trim();

            string throttleKey = BuildThrottleKey(model.Email);
            if (LoginThrottleService.IsBlocked(throttleKey, out TimeSpan retryAfter))
            {
                ModelState.AddModelError(string.Empty, $"Too many failed attempts. Try again in {Math.Max(1, (int)Math.Ceiling(retryAfter.TotalMinutes))} minute(s).");
                ViewBag.CompanyDropDown = FillCompanyDropDown();
                ViewBag.DepartmentDropDown = FillDepartmentDropDown(model.CompanyName);
                return View(model);
            }

            if (string.Equals(model.UserRole, RoleAccessService.SuperAdminRole, StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace(model.CompanyName))
            {
                model.CompanyName = ResolveCompanyNameByEmail(model.Email, _configuration.GetConnectionString("DefaultConnection")
                    ?? Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            }

            ValidateCompany(model.UserRole, model.CompanyName);
            ValidateDepartment(model.UserRole, model.DepartmentID);

            if (!ModelState.IsValid)
            {
                ViewBag.CompanyDropDown = FillCompanyDropDown();
                ViewBag.DepartmentDropDown = FillDepartmentDropDown(model.CompanyName);
                return View(model);
            }

            try
            {
                string sqlConnString = _configuration.GetConnectionString("DefaultConnection")
                    ?? Meeting_Of_Minutes.DbConnectionHelper.ConnectionString;

                bool isValidUser = false;
                int userId = 0;
                string userName = string.Empty;
                string email = string.Empty;
                string contactNo = string.Empty;
                string city = string.Empty;
                string userRole = string.Empty;
                string companyName = string.Empty;
                int? staffId = null;
                int? departmentId = null;
                string departmentName = string.Empty;
                string storedPassword = string.Empty;
                string storedPasswordHash = string.Empty;
                bool isAutoPassword = false;

                using (SqlConnection sqlConnection = new SqlConnection(sqlConnString))
                using (SqlCommand sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.CommandText = "PR_MST_User_SelectForLogin";
                    sqlCommand.Parameters.AddWithValue("@Email", model.Email ?? string.Empty);
                    sqlCommand.Parameters.AddWithValue("@Password", model.Password ?? string.Empty);
                    sqlCommand.Parameters.AddWithValue("@UserRole", model.UserRole ?? string.Empty);
                    sqlCommand.Parameters.AddWithValue("@CompanyName", model.CompanyName ?? string.Empty);
                    sqlCommand.Parameters.AddWithValue("@DepartmentID", model.DepartmentID.HasValue ? model.DepartmentID.Value : DBNull.Value);

                    sqlConnection.Open();
                    using (SqlDataReader reader = sqlCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            isValidUser = true;
                            userId = Convert.ToInt32(reader["UserID"]);
                            userName = reader["UserName"].ToString() ?? string.Empty;
                            email = reader["Email"].ToString() ?? string.Empty;
                            contactNo = reader["ContactNo"].ToString() ?? string.Empty;
                            city = reader["City"].ToString() ?? string.Empty;
                            userRole = reader["UserRole"].ToString() ?? string.Empty;
                            companyName = reader["CompanyName"].ToString() ?? string.Empty;
                            staffId = reader["StaffID"] == DBNull.Value ? null : Convert.ToInt32(reader["StaffID"]);
                            departmentId = reader["DepartmentID"] == DBNull.Value ? null : Convert.ToInt32(reader["DepartmentID"]);
                            departmentName = reader["DepartmentName"].ToString() ?? string.Empty;
                            storedPassword = reader["Password"].ToString() ?? string.Empty;
                            storedPasswordHash = reader["PasswordHash"].ToString() ?? string.Empty;
                            isAutoPassword = reader["IsAutoPassword"] != DBNull.Value && Convert.ToBoolean(reader["IsAutoPassword"]);
                        }
                    }
                }

                if (isValidUser)
                {
                    bool needsUpgrade;
                    bool passwordValid = PasswordSecurity.VerifyPassword(storedPasswordHash, storedPassword, model.Password ?? string.Empty, out needsUpgrade);
                    if (!passwordValid)
                    {
                        AuditLogService.Log(
                            companyName,
                            userId,
                            userName,
                            userRole,
                            "LoginFailed",
                            "Auth",
                            userId.ToString(),
                            "Login failed",
                            $"{model.Email} failed to sign in because of an invalid password.");
                        LoginThrottleService.RegisterFailure(throttleKey);
                        ModelState.AddModelError(string.Empty, "Invalid email, password, or role.");
                        ViewBag.CompanyDropDown = FillCompanyDropDown();
                        ViewBag.DepartmentDropDown = FillDepartmentDropDown(model.CompanyName);
                        return View(model);
                    }

                    if (needsUpgrade)
                    {
                        UpgradePasswordStorage(userId, model.Password ?? string.Empty, sqlConnString);
                    }

                    TempData.Remove("SuccessMessage");
                    TempData.Remove("SucessMessage");
                    HttpContext.Session.Clear();
                    LoginThrottleService.Clear(throttleKey);

                    HttpContext.Session.SetInt32("UserID", userId);
                    HttpContext.Session.SetString("UserName", userName);
                    HttpContext.Session.SetString("Email", email);
                    HttpContext.Session.SetString("ContactNo", contactNo);
                    HttpContext.Session.SetString("City", city);
                    HttpContext.Session.SetString("UserRole", userRole);
                    HttpContext.Session.SetString("CompanyName", companyName);

                    if (staffId.HasValue)
                    {
                        HttpContext.Session.SetInt32("StaffID", staffId.Value);
                    }
                    else
                    {
                        HttpContext.Session.Remove("StaffID");
                    }

                    if (departmentId.HasValue)
                    {
                        HttpContext.Session.SetInt32("DepartmentID", departmentId.Value);
                    }
                    else
                    {
                        HttpContext.Session.Remove("DepartmentID");
                    }

                    HttpContext.Session.SetString("DepartmentName", departmentName);
                    if (isAutoPassword)
                    {
                        HttpContext.Session.SetString("ForcePasswordReset", "true");
                        TempData["SuccessMessage"] = "Temporary password detected. Please change your password before using the workspace.";
                    }
                    else
                    {
                        HttpContext.Session.Remove("ForcePasswordReset");
                    }
                    AuditLogService.Log(companyName, userId, userName, userRole, "Login", "Auth", userId.ToString(), "User signed in", $"{userName} logged into the workspace.");
                    return isAutoPassword
                        ? RedirectToAction("Profile", "Profile")
                        : RedirectToAction("DashBoard", "DashBoard");
                }

                AuditLogService.Log(
                    model.CompanyName,
                    null,
                    model.Email,
                    model.UserRole,
                    "LoginFailed",
                    "Auth",
                    null,
                    "Login failed",
                    $"{model.Email} failed to sign in because no matching account was found.");
                LoginThrottleService.RegisterFailure(throttleKey);
                ModelState.AddModelError(string.Empty, "Invalid email, password, or role.");
                ViewBag.CompanyDropDown = FillCompanyDropDown();
                ViewBag.DepartmentDropDown = FillDepartmentDropDown(model.CompanyName);
                return View(model);
            }
            catch
            {
                LoginThrottleService.RegisterFailure(throttleKey);
                ModelState.AddModelError(string.Empty, "Login failed. Please try again.");
                ViewBag.CompanyDropDown = FillCompanyDropDown();
                ViewBag.DepartmentDropDown = FillDepartmentDropDown(model.CompanyName);
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (HttpContext.Session.GetString("UserName") != null)
            {
                return RedirectToAction("DashBoard", "DashBoard");
            }

            ViewBag.CompanyDropDown = FillCompanyDropDown();
            ViewBag.DepartmentDropDown = FillDepartmentDropDown();
            return View(new RegisterModel { UserRole = RoleAccessService.SuperAdminRole });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(RegisterModel model)
        {
            model.UserRole = RoleAccessService.SuperAdminRole;
            model.DepartmentID = null;
            ValidateDepartment(model.UserRole, model.DepartmentID);
            ValidateCompany(model.UserRole, model.CompanyName);

            if (!ModelState.IsValid)
            {
                ViewBag.CompanyDropDown = FillCompanyDropDown();
                ViewBag.DepartmentDropDown = FillDepartmentDropDown(model.CompanyName);
                return View(model);
            }

            try
            {
                if (HasSuperAdminForCompany(model.CompanyName))
                {
                    ModelState.AddModelError("CompanyName", "This company already has a Super Admin.");
                    ViewBag.CompanyDropDown = FillCompanyDropDown();
                    ViewBag.DepartmentDropDown = FillDepartmentDropDown(model.CompanyName);
                    return View(model);
                }

                string sqlConnString = _configuration.GetConnectionString("DefaultConnection")
                    ?? Meeting_Of_Minutes.DbConnectionHelper.ConnectionString;

                using (SqlConnection sqlConnection = new SqlConnection(sqlConnString))
                {
                    sqlConnection.Open();

                    using (SqlCommand checkCommand = sqlConnection.CreateCommand())
                    {
                        checkCommand.CommandType = CommandType.StoredProcedure;
                        checkCommand.CommandText = "PR_MST_User_SelectByEmail";
                        checkCommand.Parameters.AddWithValue("@Email", model.Email ?? string.Empty);

                        using (SqlDataReader reader = checkCommand.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                ModelState.AddModelError("Email", "Email already exists.");
                                ViewBag.CompanyDropDown = FillCompanyDropDown();
                                ViewBag.DepartmentDropDown = FillDepartmentDropDown(model.CompanyName);
                                return View(model);
                            }
                        }
                    }

                    using (SqlCommand nameCheckCommand = sqlConnection.CreateCommand())
                    {
                        nameCheckCommand.CommandType = CommandType.Text;
                        nameCheckCommand.CommandText = "SELECT COUNT(*) FROM MST_User WHERE UserName = @UserName";
                        nameCheckCommand.Parameters.AddWithValue("@UserName", model.UserName ?? string.Empty);

                        int userCount = Convert.ToInt32(nameCheckCommand.ExecuteScalar());
                        if (userCount > 0)
                        {
                            ModelState.AddModelError("UserName", "Username already exists.");
                            ViewBag.CompanyDropDown = FillCompanyDropDown();
                            ViewBag.DepartmentDropDown = FillDepartmentDropDown(model.CompanyName);
                            return View(model);
                        }
                    }

                    using (SqlCommand sqlCommand = sqlConnection.CreateCommand())
                    {
                        sqlCommand.CommandType = CommandType.StoredProcedure;
                        sqlCommand.CommandText = "PR_MST_User_Insert";
                        sqlCommand.Parameters.AddWithValue("@UserName", model.UserName ?? string.Empty);
                        sqlCommand.Parameters.AddWithValue("@Email", model.Email ?? string.Empty);
                        sqlCommand.Parameters.AddWithValue("@Password", string.Empty);
                        sqlCommand.Parameters.AddWithValue("@PasswordHash", PasswordSecurity.HashPassword(model.Password ?? string.Empty));
                        sqlCommand.Parameters.AddWithValue("@ContactNo", model.ContactNo ?? string.Empty);
                        sqlCommand.Parameters.AddWithValue("@City", model.City ?? string.Empty);
                        sqlCommand.Parameters.AddWithValue("@UserRole", model.UserRole ?? RoleAccessService.SuperAdminRole);
                        sqlCommand.Parameters.AddWithValue("@CompanyName", model.CompanyName ?? string.Empty);
                        sqlCommand.Parameters.AddWithValue("@StaffID", DBNull.Value);
                        sqlCommand.Parameters.AddWithValue("@DepartmentID", model.DepartmentID.HasValue ? model.DepartmentID.Value : DBNull.Value);
                        sqlCommand.Parameters.AddWithValue("@IsActive", true);
                        sqlCommand.Parameters.AddWithValue("@Modified", DateTime.Now);
                        sqlCommand.ExecuteNonQuery();
                    }
                }

                AuditLogService.Log(model.CompanyName, null, model.UserName, RoleAccessService.SuperAdminRole, "Register", "Auth", null, "Company super admin account created", $"{model.UserName} created the Super Admin account for {model.CompanyName}.");
                TempData["SuccessMessage"] = "Company Super Admin account created successfully. Please sign in.";
                return RedirectToAction("Login");
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "Signup failed. Please review the form and try again.");
                ViewBag.CompanyDropDown = FillCompanyDropDown();
                ViewBag.DepartmentDropDown = FillDepartmentDropDown(model.CompanyName);
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        [HttpGet]
        [ActionName("Logout")]
        public IActionResult LogoutFallback()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        [HttpGet]
        public JsonResult GetDepartmentsByCompany(string companyName)
        {
            List<object> departments = FillDepartmentDropDown(companyName)
                .Select(d => new
                {
                    value = d.Value,
                    text = d.Text
                })
                .Cast<object>()
                .ToList();

            return Json(departments);
        }

        [HttpGet]
        public JsonResult GetUserContextByEmail(string email)
        {
            email = email?.Trim();
            if (string.IsNullOrWhiteSpace(email))
            {
                return Json(new { found = false });
            }

            string sqlConnString = _configuration.GetConnectionString("DefaultConnection")
                ?? Meeting_Of_Minutes.DbConnectionHelper.ConnectionString;

            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(sqlConnString))
                using (SqlCommand sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.CommandText = "PR_MST_User_SelectByEmail";
                    sqlCommand.Parameters.AddWithValue("@Email", email);
                    sqlConnection.Open();

                    using (SqlDataReader reader = sqlCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return Json(new
                            {
                                found = true,
                                userRole = reader["UserRole"].ToString(),
                                companyName = reader["CompanyName"].ToString(),
                                departmentId = reader["DepartmentID"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["DepartmentID"])
                            });
                        }
                    }
                }
            }
            catch
            {
                return Json(new { found = false });
            }

            return Json(new { found = false });
        }

        [HttpGet]
        public IActionResult AcceptInvite(string token)
        {
            InviteOnboardingModel? invite = UserInviteService.GetInviteForOnboarding(token);
            if (invite == null)
            {
                TempData["ErrorMessage"] = "This invite is invalid or has expired. Ask your Super Admin to resend the onboarding link.";
                return RedirectToAction(nameof(Login));
            }

            return View(invite);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AcceptInvite(InviteOnboardingModel model)
        {
            InviteOnboardingModel? invite = UserInviteService.GetInviteForOnboarding(model.InviteToken);
            if (invite == null)
            {
                TempData["ErrorMessage"] = "This invite is invalid or has expired. Ask your Super Admin to resend the onboarding link.";
                return RedirectToAction(nameof(Login));
            }

            model.UserID = invite.UserID;
            model.UserName = invite.UserName;
            model.Email = invite.Email;
            model.CompanyName = invite.CompanyName;
            model.UserRole = invite.UserRole;
            model.ExpiresAt = invite.ExpiresAt;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (!UserInviteService.CompleteInvite(model))
            {
                ModelState.AddModelError(string.Empty, "Invite completion failed. The link may have expired.");
                return View(model);
            }

            TempData["SuccessMessage"] = "Account setup completed successfully. Please sign in with your new password.";
            return RedirectToAction(nameof(Login));
        }

        public void ValidateDepartment(string? userRole, int? departmentId)
        {
            if (string.Equals(userRole, "User", StringComparison.OrdinalIgnoreCase) && (!departmentId.HasValue || departmentId.Value == 0))
            {
                ModelState.AddModelError("DepartmentID", "Please select department.");
            }
        }

        public void ValidateCompany(string? userRole, string? companyName)
        {
            if (string.IsNullOrWhiteSpace(companyName))
            {
                ModelState.AddModelError("CompanyName", "Please select company.");
            }
        }

        public void UpgradePasswordStorage(int userId, string plainPassword, string connectionString)
        {
            using SqlConnection sqlConnection = new SqlConnection(connectionString);
            using SqlCommand sqlCommand = sqlConnection.CreateCommand();
            sqlCommand.CommandType = CommandType.Text;
            sqlCommand.CommandText = "UPDATE MST_User SET Password = '', PasswordHash = @PasswordHash, Modified = GETDATE() WHERE UserID = @UserID";
            sqlCommand.Parameters.AddWithValue("@UserID", userId);
            sqlCommand.Parameters.AddWithValue("@PasswordHash", PasswordSecurity.HashPassword(plainPassword));
            sqlConnection.Open();
            sqlCommand.ExecuteNonQuery();
        }

        public List<SelectListItem> FillCompanyDropDown()
        {
            List<SelectListItem> list = new List<SelectListItem>();
            string sqlConnString = _configuration.GetConnectionString("DefaultConnection")
                ?? Meeting_Of_Minutes.DbConnectionHelper.ConnectionString;

            using (SqlConnection sqlConnection = new SqlConnection(sqlConnString))
            using (SqlCommand sqlCommand = sqlConnection.CreateCommand())
            {
                sqlCommand.CommandType = CommandType.StoredProcedure;
                sqlCommand.CommandText = "PR_MST_Company_DDL";
                sqlConnection.Open();

                using (SqlDataReader reader = sqlCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new SelectListItem
                        {
                            Value = reader["CompanyName"].ToString(),
                            Text = reader["CompanyName"].ToString()
                        });
                    }
                }
            }

            return list;
        }

        public List<SelectListItem> FillDepartmentDropDown(string? companyName = null)
        {
            List<SelectListItem> list = new List<SelectListItem>();
            string sqlConnString = _configuration.GetConnectionString("DefaultConnection")
                ?? Meeting_Of_Minutes.DbConnectionHelper.ConnectionString;

            using (SqlConnection sqlConnection = new SqlConnection(sqlConnString))
            using (SqlCommand sqlCommand = sqlConnection.CreateCommand())
            {
                sqlCommand.CommandType = CommandType.StoredProcedure;
                sqlCommand.CommandText = "PR_MOM_DEPARTMENT_DDL";
                sqlCommand.Parameters.AddWithValue("@CompanyName", string.IsNullOrWhiteSpace(companyName) ? DBNull.Value : companyName);
                sqlConnection.Open();

                using (SqlDataReader reader = sqlCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new SelectListItem
                        {
                            Value = reader["DepartmentID"].ToString(),
                            Text = reader["DepartmentName"].ToString()
                        });
                    }
                }
            }

            return list;
        }

        private bool HasSuperAdminForCompany(string? companyName)
        {
            if (string.IsNullOrWhiteSpace(companyName))
            {
                return false;
            }

            string sqlConnString = _configuration.GetConnectionString("DefaultConnection")
                ?? Meeting_Of_Minutes.DbConnectionHelper.ConnectionString;

            using SqlConnection sqlConnection = new SqlConnection(sqlConnString);
            using SqlCommand sqlCommand = sqlConnection.CreateCommand();
            sqlCommand.CommandType = CommandType.Text;
            sqlCommand.CommandText = "SELECT COUNT(*) FROM MST_User WHERE UserRole = @UserRole AND CompanyName = @CompanyName AND IsActive = 1";
            sqlCommand.Parameters.AddWithValue("@UserRole", RoleAccessService.SuperAdminRole);
            sqlCommand.Parameters.AddWithValue("@CompanyName", companyName);
            sqlConnection.Open();
            return Convert.ToInt32(sqlCommand.ExecuteScalar()) > 0;
        }

        private string ResolveCompanyNameByEmail(string? email, string connectionString)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return string.Empty;
            }

            using SqlConnection sqlConnection = new SqlConnection(connectionString);
            using SqlCommand sqlCommand = sqlConnection.CreateCommand();
            sqlCommand.CommandType = CommandType.Text;
            sqlCommand.CommandText = "SELECT TOP 1 CompanyName FROM MST_User WHERE Email = @Email";
            sqlCommand.Parameters.AddWithValue("@Email", email);
            sqlConnection.Open();
            return Convert.ToString(sqlCommand.ExecuteScalar()) ?? string.Empty;
        }

        private string BuildThrottleKey(string? email)
        {
            string normalizedEmail = (email ?? string.Empty).Trim().ToLowerInvariant();
            string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return $"{ipAddress}|{normalizedEmail}";
        }
    }
}
