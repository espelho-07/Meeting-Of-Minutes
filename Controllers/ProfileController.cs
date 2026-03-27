using Meeting_Of_Minutes.Models;
using Meeting_Of_Minutes.Security;
using Meeting_Of_Minutes.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Net.Mail;
using System.Data;

namespace Meeting_Of_Minutes.Controllers
{
    public class ProfileController : Controller
    {
        private readonly IWebHostEnvironment _environment;

        public ProfileController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        #region Actions
        public IActionResult Profile()
        {
            UserProfileModel model = GetProfileModel();
            if (model.UserID == 0)
            {
                return RedirectToAction("Login", "Auth");
            }

            model.UpdateRequests = GetProfileUpdateRequestsByUserID(model.UserID);
            model.RequestedUserName = model.UserName;
            model.RequestedEmail = model.Email;
            model.RequestedContactNo = model.ContactNo;
            model.RequestedCity = model.City;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SubmitUpdateRequest(UserProfileModel formModel, IFormFile? proofDocument)
        {
            UserProfileModel model = GetProfileModel();
            if (model.UserID == 0)
            {
                return RedirectToAction("Login", "Auth");
            }

            if (IsAdminUser())
            {
                TempData["ErrorMessage"] = "Admin profile updates do not require approval request.";
                return RedirectToAction("Profile");
            }

            string requestedUserName = formModel.RequestedUserName?.Trim() ?? string.Empty;
            string requestedEmail = formModel.RequestedEmail?.Trim() ?? string.Empty;
            string requestedContactNo = formModel.RequestedContactNo?.Trim() ?? string.Empty;
            string requestedCity = formModel.RequestedCity?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(requestedUserName) || string.IsNullOrWhiteSpace(requestedEmail))
            {
                TempData["ErrorMessage"] = "Name and email are required.";
                return RedirectToAction("Profile");
            }

            if (!IsValidEmailAddress(requestedEmail))
            {
                TempData["ErrorMessage"] = "Enter a valid email address.";
                return RedirectToAction("Profile");
            }

            bool hasChange =
                !string.Equals(model.UserName, requestedUserName, StringComparison.Ordinal) ||
                !string.Equals(model.Email, requestedEmail, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(model.ContactNo ?? string.Empty, requestedContactNo, StringComparison.Ordinal) ||
                !string.Equals(model.City ?? string.Empty, requestedCity, StringComparison.Ordinal);

            if (!hasChange)
            {
                TempData["ErrorMessage"] = "Please change at least one profile field.";
                return RedirectToAction("Profile");
            }

            if (proofDocument == null || proofDocument.Length == 0)
            {
                TempData["ErrorMessage"] = "Please upload proof document.";
                return RedirectToAction("Profile");
            }

            if (!FileSecurityService.TrySaveDocument(_environment, proofDocument, "profile-documents", $"profile_{model.UserID}", out string documentPath, out string uploadError))
            {
                TempData["ErrorMessage"] = uploadError;
                return RedirectToAction("Profile");
            }

            SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            con.Open();

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_MST_ProfileUpdateRequest_Insert";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@UserID", model.UserID);
            cmd.Parameters.AddWithValue("@StaffID", HttpContext.Session.GetInt32("StaffID").HasValue ? HttpContext.Session.GetInt32("StaffID")!.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@RequestedUserName", requestedUserName);
            cmd.Parameters.AddWithValue("@RequestedEmail", requestedEmail);
            cmd.Parameters.AddWithValue("@RequestedContactNo", requestedContactNo);
            cmd.Parameters.AddWithValue("@RequestedCity", requestedCity);
            cmd.Parameters.AddWithValue("@DocumentPath", documentPath);
            cmd.Parameters.AddWithValue("@Modified", DateTime.Now);
            cmd.ExecuteNonQuery();
            con.Close();

            InsertAdminNotification(
                model.CompanyName,
                "ProfileUpdateRequest",
                "Profile update requested",
                $"{model.UserName} requested profile update approval.",
                model.UserID);
            AuditLogService.Log(model.CompanyName, model.UserID, model.UserName, model.UserRole, "Submit", "ProfileUpdateRequest", model.UserID.ToString(), "Profile update request submitted", $"{model.UserName} submitted a profile update request.");

            TempData["SuccessMessage"] = "Profile update request sent successfully.";
            return RedirectToAction("Profile");
        }

        public IActionResult ProfileUpdateRequestList()
        {
            if (!IsAdminUser())
            {
                return RedirectToAction("Profile");
            }

            List<ProfileUpdateRequestModel> requests = GetAllProfileUpdateRequests();
            return View(requests);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MarkAllNotificationsRead()
        {
            if (!IsAdminUser())
            {
                return RedirectToAction("Profile");
            }

            SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_MST_AdminNotification_MarkAllRead";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@CompanyName", HttpContext.Session.GetString("CompanyName") ?? string.Empty);
            con.Open();
            cmd.ExecuteNonQuery();
            con.Close();

            TempData["SuccessMessage"] = "Notifications marked as read.";
            return RedirectToAction("DashBoard", "DashBoard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ClearAllNotifications()
        {
            if (!IsAdminUser())
            {
                return RedirectToAction("Profile");
            }

            SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_MST_AdminNotification_ClearAll";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@CompanyName", HttpContext.Session.GetString("CompanyName") ?? string.Empty);
            con.Open();
            cmd.ExecuteNonQuery();
            con.Close();

            TempData["SuccessMessage"] = "Notifications cleared successfully.";
            return RedirectToAction("DashBoard", "DashBoard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApproveProfileUpdateRequest(int profileUpdateRequestID, string? adminRemarks, string? returnUrl)
        {
            if (!IsAdminUser())
            {
                return RedirectToAction("Profile");
            }

            int? adminUserId = HttpContext.Session.GetInt32("UserID");
            if (!adminUserId.HasValue)
            {
                return RedirectToAction("Login", "Auth");
            }

            try
            {
                SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
                con.Open();

                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandText = "PR_MST_ProfileUpdateRequest_Approve";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ProfileUpdateRequestID", profileUpdateRequestID);
                cmd.Parameters.AddWithValue("@AdminUserID", adminUserId.Value);
                cmd.Parameters.AddWithValue("@AdminRemarks", string.IsNullOrWhiteSpace(adminRemarks) ? DBNull.Value : adminRemarks);
                cmd.ExecuteNonQuery();
                con.Close();
                AuditLogService.Log(HttpContext.Session.GetString("CompanyName"), adminUserId.Value, HttpContext.Session.GetString("UserName"), HttpContext.Session.GetString("UserRole"), "Approve", "ProfileUpdateRequest", profileUpdateRequestID.ToString(), "Profile request approved", $"Profile update request #{profileUpdateRequestID} was approved.");

                TempData["SuccessMessage"] = "Profile update approved successfully.";
            }
            catch
            {
                TempData["ErrorMessage"] = "Approve failed. Please try again.";
            }

            return RedirectToLocal(returnUrl, "ProfileUpdateRequestList");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RejectProfileUpdateRequest(int profileUpdateRequestID, string? adminRemarks, string? returnUrl)
        {
            if (!IsAdminUser())
            {
                return RedirectToAction("Profile");
            }

            int? adminUserId = HttpContext.Session.GetInt32("UserID");
            if (!adminUserId.HasValue)
            {
                return RedirectToAction("Login", "Auth");
            }

            SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            con.Open();

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_MST_ProfileUpdateRequest_Reject";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@ProfileUpdateRequestID", profileUpdateRequestID);
            cmd.Parameters.AddWithValue("@AdminUserID", adminUserId.Value);
            cmd.Parameters.AddWithValue("@AdminRemarks", string.IsNullOrWhiteSpace(adminRemarks) ? DBNull.Value : adminRemarks);
            cmd.ExecuteNonQuery();
            con.Close();
            AuditLogService.Log(HttpContext.Session.GetString("CompanyName"), adminUserId.Value, HttpContext.Session.GetString("UserName"), HttpContext.Session.GetString("UserRole"), "Reject", "ProfileUpdateRequest", profileUpdateRequestID.ToString(), "Profile request rejected", $"Profile update request #{profileUpdateRequestID} was rejected.");

            TempData["SuccessMessage"] = "Profile update rejected.";
            return RedirectToLocal(returnUrl, "ProfileUpdateRequestList");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BulkApproveProfileUpdateRequests(List<int>? selectedRequestIds, string? adminRemarks, string? returnUrl)
        {
            if (!IsAdminUser())
            {
                return RedirectToAction("Profile");
            }

            int? adminUserId = HttpContext.Session.GetInt32("UserID");
            if (!adminUserId.HasValue)
            {
                return RedirectToAction("Login", "Auth");
            }

            if (selectedRequestIds == null || selectedRequestIds.Count == 0)
            {
                TempData["ErrorMessage"] = "Select at least one profile request.";
                return RedirectToLocal(returnUrl, "ProfileUpdateRequestList");
            }

            int successCount = 0;

            foreach (int requestId in selectedRequestIds.Distinct())
            {
                try
                {
                    using SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
                    using SqlCommand cmd = new SqlCommand("PR_MST_ProfileUpdateRequest_Approve", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ProfileUpdateRequestID", requestId);
                    cmd.Parameters.AddWithValue("@AdminUserID", adminUserId.Value);
                    cmd.Parameters.AddWithValue("@AdminRemarks", string.IsNullOrWhiteSpace(adminRemarks) ? DBNull.Value : adminRemarks);
                    con.Open();
                    cmd.ExecuteNonQuery();

                    AuditLogService.Log(HttpContext.Session.GetString("CompanyName"), adminUserId.Value, HttpContext.Session.GetString("UserName"), HttpContext.Session.GetString("UserRole"), "Approve", "ProfileUpdateRequest", requestId.ToString(), "Profile request approved", $"Profile update request #{requestId} was approved in bulk.");
                    successCount++;
                }
                catch
                {
                }
            }

            TempData[successCount > 0 ? "SuccessMessage" : "ErrorMessage"] = successCount > 0
                ? $"{successCount} profile request(s) approved successfully."
                : "Bulk approve failed for the selected profile requests.";

            return RedirectToLocal(returnUrl, "ProfileUpdateRequestList");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BulkRejectProfileUpdateRequests(List<int>? selectedRequestIds, string? adminRemarks, string? returnUrl)
        {
            if (!IsAdminUser())
            {
                return RedirectToAction("Profile");
            }

            int? adminUserId = HttpContext.Session.GetInt32("UserID");
            if (!adminUserId.HasValue)
            {
                return RedirectToAction("Login", "Auth");
            }

            if (selectedRequestIds == null || selectedRequestIds.Count == 0)
            {
                TempData["ErrorMessage"] = "Select at least one profile request.";
                return RedirectToLocal(returnUrl, "ProfileUpdateRequestList");
            }

            int successCount = 0;

            foreach (int requestId in selectedRequestIds.Distinct())
            {
                try
                {
                    using SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
                    using SqlCommand cmd = new SqlCommand("PR_MST_ProfileUpdateRequest_Reject", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ProfileUpdateRequestID", requestId);
                    cmd.Parameters.AddWithValue("@AdminUserID", adminUserId.Value);
                    cmd.Parameters.AddWithValue("@AdminRemarks", string.IsNullOrWhiteSpace(adminRemarks) ? DBNull.Value : adminRemarks);
                    con.Open();
                    cmd.ExecuteNonQuery();

                    AuditLogService.Log(HttpContext.Session.GetString("CompanyName"), adminUserId.Value, HttpContext.Session.GetString("UserName"), HttpContext.Session.GetString("UserRole"), "Reject", "ProfileUpdateRequest", requestId.ToString(), "Profile request rejected", $"Profile update request #{requestId} was rejected in bulk.");
                    successCount++;
                }
                catch
                {
                }
            }

            TempData[successCount > 0 ? "SuccessMessage" : "ErrorMessage"] = successCount > 0
                ? $"{successCount} profile request(s) rejected."
                : "Bulk reject failed for the selected profile requests.";

            return RedirectToLocal(returnUrl, "ProfileUpdateRequestList");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdatePassword(UserProfileModel formModel)
        {
            UserProfileModel model = GetProfileModel();
            if (model.UserID == 0)
            {
                return RedirectToAction("Login", "Auth");
            }

            if (string.IsNullOrWhiteSpace(formModel.CurrentPassword))
            {
                TempData["ErrorMessage"] = "Current password is required.";
                return RedirectToAction("Profile");
            }

            if (string.IsNullOrWhiteSpace(formModel.NewPassword))
            {
                TempData["ErrorMessage"] = "New password is required.";
                return RedirectToAction("Profile");
            }

            if (formModel.NewPassword != formModel.ConfirmPassword)
            {
                TempData["ErrorMessage"] = "New password and confirm password must match.";
                return RedirectToAction("Profile");
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(formModel.NewPassword, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,}$"))
            {
                TempData["ErrorMessage"] = "Password must be at least 8 characters and include uppercase, lowercase, number, and special character.";
                return RedirectToAction("Profile");
            }

            SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            con.Open();

            SqlCommand checkCmd = new SqlCommand();
            checkCmd.Connection = con;
            checkCmd.CommandText = "SELECT Password, PasswordHash FROM MST_User WHERE UserID = @UserID";
            checkCmd.CommandType = CommandType.Text;
            checkCmd.Parameters.AddWithValue("@UserID", model.UserID);

            string storedPassword = string.Empty;
            string storedPasswordHash = string.Empty;
            SqlDataReader reader = checkCmd.ExecuteReader();
            if (reader.Read())
            {
                storedPassword = reader["Password"].ToString() ?? string.Empty;
                storedPasswordHash = reader["PasswordHash"].ToString() ?? string.Empty;
            }
            reader.Close();

            bool needsUpgrade;
            bool currentPasswordValid = PasswordSecurity.VerifyPassword(storedPasswordHash, storedPassword, formModel.CurrentPassword, out needsUpgrade);
            if (!currentPasswordValid)
            {
                con.Close();
                TempData["ErrorMessage"] = "Current password is incorrect.";
                return RedirectToAction("Profile");
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_MST_User_UpdatePasswordByPK";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@UserID", model.UserID);
            cmd.Parameters.AddWithValue("@NewPassword", string.Empty);
            cmd.Parameters.AddWithValue("@NewPasswordHash", PasswordSecurity.HashPassword(formModel.NewPassword));
            cmd.ExecuteNonQuery();
            con.Close();

            InsertAdminNotification(
                model.CompanyName,
                "PasswordChanged",
                "Password changed",
                $"{model.UserName} changed account password.",
                model.UserID);
            HttpContext.Session.Remove("ForcePasswordReset");
            AuditLogService.Log(model.CompanyName, model.UserID, model.UserName, model.UserRole, "PasswordUpdate", "UserAccount", model.UserID.ToString(), "Password updated", $"{model.UserName} changed account password.");

            TempData["SuccessMessage"] = "Password updated successfully.";
            return RedirectToAction("Profile");
        }

        public UserProfileModel GetProfileModel()
        {
            UserProfileModel model = new UserProfileModel();
            int? userId = HttpContext.Session.GetInt32("UserID");

            if (!userId.HasValue)
            {
                return model;
            }

            SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_MST_User_SelectByPK";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@UserID", userId.Value);

            con.Open();
            SqlDataReader reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                model.UserID = Convert.ToInt32(reader["UserID"]);
                model.UserName = reader["UserName"].ToString() ?? string.Empty;
                model.Email = reader["Email"].ToString() ?? string.Empty;
                model.ContactNo = reader["ContactNo"].ToString() ?? string.Empty;
                model.City = reader["City"].ToString() ?? string.Empty;
                model.UserRole = reader["UserRole"].ToString() ?? string.Empty;
                model.CompanyName = reader["CompanyName"].ToString() ?? string.Empty;
                model.DepartmentID = reader["DepartmentID"] == DBNull.Value ? null : Convert.ToInt32(reader["DepartmentID"]);
                model.DepartmentName = reader["DepartmentName"].ToString() ?? string.Empty;
                model.IsAutoPassword = reader["IsAutoPassword"] != DBNull.Value && Convert.ToBoolean(reader["IsAutoPassword"]);
            }
            reader.Close();
            con.Close();

            if (model.UserID != 0)
            {
                HttpContext.Session.SetString("UserName", model.UserName);
                HttpContext.Session.SetString("Email", model.Email);
                HttpContext.Session.SetString("ContactNo", model.ContactNo);
                HttpContext.Session.SetString("City", model.City);
                HttpContext.Session.SetString("CompanyName", model.CompanyName);
                HttpContext.Session.SetString("DepartmentName", model.DepartmentName);
                if (model.DepartmentID.HasValue)
                {
                    HttpContext.Session.SetInt32("DepartmentID", model.DepartmentID.Value);
                }
                else
                {
                    HttpContext.Session.Remove("DepartmentID");
                }
            }

            return model;
        }

        public List<ProfileUpdateRequestModel> GetProfileUpdateRequestsByUserID(int userId)
        {
            List<ProfileUpdateRequestModel> requests = new List<ProfileUpdateRequestModel>();
            SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_MST_ProfileUpdateRequest_SelectByUserID";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@UserID", userId);

            con.Open();
            SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                ProfileUpdateRequestModel request = new ProfileUpdateRequestModel();
                request.ProfileUpdateRequestID = Convert.ToInt32(reader["ProfileUpdateRequestID"]);
                request.UserID = Convert.ToInt32(reader["UserID"]);
                request.StaffID = reader["StaffID"] == DBNull.Value ? null : Convert.ToInt32(reader["StaffID"]);
                request.CurrentUserName = reader["CurrentUserName"].ToString() ?? string.Empty;
                request.CurrentEmail = reader["CurrentEmail"].ToString() ?? string.Empty;
                request.CurrentContactNo = reader["CurrentContactNo"].ToString() ?? string.Empty;
                request.CurrentCity = reader["CurrentCity"].ToString() ?? string.Empty;
                request.RequestedUserName = reader["RequestedUserName"].ToString() ?? string.Empty;
                request.RequestedEmail = reader["RequestedEmail"].ToString() ?? string.Empty;
                request.RequestedContactNo = reader["RequestedContactNo"].ToString() ?? string.Empty;
                request.RequestedCity = reader["RequestedCity"].ToString() ?? string.Empty;
                request.DocumentPath = reader["DocumentPath"].ToString() ?? string.Empty;
                request.RequestStatus = reader["RequestStatus"].ToString() ?? string.Empty;
                request.AdminRemarks = reader["AdminRemarks"].ToString() ?? string.Empty;
                request.Created = Convert.ToDateTime(reader["Created"]);
                request.DecisionDate = reader["DecisionDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["DecisionDate"]);
                requests.Add(request);
            }
            reader.Close();
            con.Close();

            return requests;
        }

        public List<ProfileUpdateRequestModel> GetAllProfileUpdateRequests()
        {
            List<ProfileUpdateRequestModel> requests = new List<ProfileUpdateRequestModel>();
            SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_MST_ProfileUpdateRequest_SelectAll";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@CompanyName", HttpContext.Session.GetString("CompanyName") ?? string.Empty);

            con.Open();
            SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                ProfileUpdateRequestModel request = new ProfileUpdateRequestModel();
                request.ProfileUpdateRequestID = Convert.ToInt32(reader["ProfileUpdateRequestID"]);
                request.UserID = Convert.ToInt32(reader["UserID"]);
                request.StaffID = reader["StaffID"] == DBNull.Value ? null : Convert.ToInt32(reader["StaffID"]);
                request.CurrentUserName = reader["CurrentUserName"].ToString() ?? string.Empty;
                request.CurrentEmail = reader["CurrentEmail"].ToString() ?? string.Empty;
                request.CurrentContactNo = reader["CurrentContactNo"].ToString() ?? string.Empty;
                request.CurrentCity = reader["CurrentCity"].ToString() ?? string.Empty;
                request.RequestedUserName = reader["RequestedUserName"].ToString() ?? string.Empty;
                request.RequestedEmail = reader["RequestedEmail"].ToString() ?? string.Empty;
                request.RequestedContactNo = reader["RequestedContactNo"].ToString() ?? string.Empty;
                request.RequestedCity = reader["RequestedCity"].ToString() ?? string.Empty;
                request.CompanyName = reader["CompanyName"].ToString() ?? string.Empty;
                request.DepartmentName = reader["DepartmentName"].ToString() ?? string.Empty;
                request.DocumentPath = reader["DocumentPath"].ToString() ?? string.Empty;
                request.RequestStatus = reader["RequestStatus"].ToString() ?? string.Empty;
                request.AdminRemarks = reader["AdminRemarks"].ToString() ?? string.Empty;
                request.Created = Convert.ToDateTime(reader["Created"]);
                request.DecisionDate = reader["DecisionDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["DecisionDate"]);
                requests.Add(request);
            }
            reader.Close();
            con.Close();

            return requests;
        }

        public bool IsAdminUser()
        {
            return RoleAccessService.IsAdminOrHigher(HttpContext.Session.GetString("UserRole"));
        }

        public void InsertAdminNotification(string? companyName, string notificationType, string title, string message, int userId)
        {
            SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            con.Open();

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_MST_AdminNotification_Insert";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@CompanyName", string.IsNullOrWhiteSpace(companyName) ? DBNull.Value : companyName);
            cmd.Parameters.AddWithValue("@NotificationType", notificationType);
            cmd.Parameters.AddWithValue("@Title", title);
            cmd.Parameters.AddWithValue("@Message", message);
            cmd.Parameters.AddWithValue("@RelatedUserID", userId);
            cmd.Parameters.AddWithValue("@Modified", DateTime.Now);
            cmd.ExecuteNonQuery();
            con.Close();
        }

        public IActionResult RedirectToLocal(string? returnUrl, string fallbackAction)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(fallbackAction);
        }

        private bool IsValidEmailAddress(string email)
        {
            try
            {
                _ = new MailAddress(email);
                return true;
            }
            catch
            {
                return false;
            }
        }
        #endregion
    }
}


