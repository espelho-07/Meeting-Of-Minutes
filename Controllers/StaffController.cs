using Meeting_Of_Minutes.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Mvc.Rendering;
using ClosedXML.Excel;

namespace Meeting_Of_Minutes.Controllers
{
    public class StaffController : Controller
    {
        #region Actions
        public IActionResult StaffAddEdit(int? id)
        {
            if (!IsAdminUser())
            {
                return RedirectToAction("DashBoard", "DashBoard");
            }

            ViewBag.DepartmentDropDown = FillDepartmentDropDown();
            StaffModel model = new StaffModel();
            HashSet<int> allowedDepartmentIds = GetAllowedDepartmentIdsForCompany();

            if (id.HasValue)
            {
                SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandText = "PR_Staff_SelectByPK";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@StaffID", id.Value);

                con.Open();
                SqlDataReader sdr = cmd.ExecuteReader();

                if (sdr.Read())
                {
                    model.StaffID = Convert.ToInt32(sdr["StaffID"]);
                    model.DepartmentID = Convert.ToInt32(sdr["DepartmentID"]);
                    model.DepartmentName = sdr["DepartmentName"].ToString();
                    model.CompanyName = sdr["CompanyName"].ToString();
                    model.StaffName = sdr["StaffName"].ToString();
                    model.MobileNo = sdr["MobileNo"].ToString();
                    model.EmailAddress = sdr["EmailAddress"].ToString();
                    model.Remarks = sdr["Remarks"].ToString();
                }

                sdr.Close();
                con.Close();
            }

            if (model.StaffID != 0 && !allowedDepartmentIds.Contains(model.DepartmentID))
            {
                return RedirectToAction("StaffList");
            }

            return View(model);
        }


        [HttpGet]
        public IActionResult StaffList()
        {
            if (!IsAdminUser())
            {
                return RedirectToAction("DashBoard", "DashBoard");
            }

            List<StaffModel> staffList = GetAllStaff(null);
            return View(staffList);
        }

        [HttpPost]
        public IActionResult StaffList(IFormCollection formdata)
        {
            if (!IsAdminUser())
            {
                return RedirectToAction("DashBoard", "DashBoard");
            }

            string? searchtext = formdata["searchtext"].ToString();

            if (string.IsNullOrWhiteSpace(searchtext))
            {
                searchtext = null;
            }

            ViewBag.searchtext = searchtext;

            List<StaffModel> staffList = GetAllStaff(searchtext);
            return View(staffList);
        }

        public List<StaffModel> GetAllStaff(string? searchtext)
        {
            List<StaffModel> staffList = new List<StaffModel>();
            HashSet<int> allowedDepartmentIds = GetAllowedDepartmentIdsForCompany();

            SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_Staff_SelectAll";
            cmd.CommandType = CommandType.StoredProcedure;

            if (searchtext != null)
            {
                cmd.Parameters.AddWithValue("@searchtext", searchtext);
            }
            else
            {
                cmd.Parameters.AddWithValue("@searchtext", DBNull.Value);
            }

            con.Open();
            SqlDataReader sdr = cmd.ExecuteReader();

            while (sdr.Read())
            {
                StaffModel model = new StaffModel();
                model.StaffID = Convert.ToInt32(sdr["StaffID"]);
                model.DepartmentID = Convert.ToInt32(sdr["DepartmentID"]);
                model.DepartmentName = sdr["DepartmentName"].ToString();
                model.CompanyName = sdr["CompanyName"].ToString();
                model.StaffName = sdr["StaffName"].ToString();
                model.MobileNo = sdr["MobileNo"].ToString();
                model.EmailAddress = sdr["EmailAddress"].ToString();
                model.Remarks = sdr["Remarks"].ToString();
                if (allowedDepartmentIds.Contains(model.DepartmentID))
                {
                    staffList.Add(model);
                }
            }

            sdr.Close();
            con.Close();

            return staffList;
        }

        public IActionResult StaffView(int id)
        {
            if (!IsAdminUser())
            {
                return RedirectToAction("DashBoard", "DashBoard");
            }

            StaffModel model = new StaffModel();
            HashSet<int> allowedDepartmentIds = GetAllowedDepartmentIdsForCompany();

            SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            con.Open();

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_Staff_SelectByPKForView";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@StaffID", id);

            SqlDataReader reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                model.StaffID = Convert.ToInt32(reader["StaffID"]);
                model.DepartmentID = Convert.ToInt32(reader["DepartmentID"]);
                model.DepartmentName = reader["DepartmentName"].ToString();
                model.CompanyName = reader["CompanyName"].ToString();
                model.StaffName = reader["StaffName"].ToString();
                model.MobileNo = reader["MobileNo"].ToString();
                model.EmailAddress = reader["EmailAddress"].ToString();
                model.Remarks = reader["Remarks"].ToString();
                model.LoginUserName = reader["LoginUserName"].ToString();
                model.LoginPassword = reader["LoginPassword"].ToString();
                model.IsAutoPassword = reader["IsAutoPassword"] != DBNull.Value && Convert.ToBoolean(reader["IsAutoPassword"]);
                model.EnrolledMeetingsCount = Convert.ToInt32(reader["EnrolledMeetingsCount"]);
            }
            reader.Close();

            if (model.StaffID == 0 || !allowedDepartmentIds.Contains(model.DepartmentID))
            {
                con.Close();
                return RedirectToAction("StaffList");
            }

            cmd.Parameters.Clear();
            cmd.CommandText = "PR_MeetingMember_SelectByStaffID";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@StaffID", id);

            reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                MeetingsModel meeting = new MeetingsModel();
                meeting.MeetingID = Convert.ToInt32(reader["MeetingID"]);
                meeting.MeetingDate = reader["MeetingDate"] as DateTime?;
                meeting.MeetingDescription = reader["MeetingDescription"].ToString();
                meeting.DepartmentName = reader["DepartmentName"].ToString();
                meeting.MeetingTypeName = reader["MeetingTypeName"].ToString();
                meeting.MeetingVenueName = reader["MeetingVenueName"].ToString();
                model.EnrolledMeetings.Add(meeting);
            }
            reader.Close();

            cmd.Parameters.Clear();
            cmd.CommandText = "PR_MST_StaffTransferRequest_SelectByStaffID";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@StaffID", id);

            reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                StaffTransferRequestModel request = new StaffTransferRequestModel();
                request.StaffTransferRequestID = Convert.ToInt32(reader["StaffTransferRequestID"]);
                request.StaffID = Convert.ToInt32(reader["StaffID"]);
                request.UserID = reader["UserID"] == DBNull.Value ? null : Convert.ToInt32(reader["UserID"]);
                request.StaffName = reader["StaffName"].ToString() ?? string.Empty;
                request.EmailAddress = reader["EmailAddress"].ToString() ?? string.Empty;
                request.MobileNo = reader["MobileNo"].ToString() ?? string.Empty;
                request.SourceCompanyName = reader["SourceCompanyName"].ToString() ?? string.Empty;
                request.SourceDepartmentID = Convert.ToInt32(reader["SourceDepartmentID"]);
                request.SourceDepartmentName = reader["SourceDepartmentName"].ToString() ?? string.Empty;
                request.TargetCompanyName = reader["TargetCompanyName"].ToString() ?? string.Empty;
                request.TargetDepartmentID = Convert.ToInt32(reader["TargetDepartmentID"]);
                request.TargetDepartmentName = reader["TargetDepartmentName"].ToString() ?? string.Empty;
                request.RequestedByUserID = Convert.ToInt32(reader["RequestedByUserID"]);
                request.RequestedByUserName = reader["RequestedByUserName"].ToString() ?? string.Empty;
                request.TransferReason = reader["TransferReason"].ToString() ?? string.Empty;
                request.DocumentPath = reader["DocumentPath"].ToString() ?? string.Empty;
                request.RequestStatus = reader["RequestStatus"].ToString() ?? string.Empty;
                request.AdminRemarks = reader["AdminRemarks"].ToString() ?? string.Empty;
                request.Created = Convert.ToDateTime(reader["Created"]);
                request.DecisionDate = reader["DecisionDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["DecisionDate"]);
                model.TransferRequests.Add(request);
            }
            reader.Close();
            con.Close();

            ViewBag.TransferCompanyDropDown = FillCompanyDropDown();
            return View(model);
        }

        public IActionResult StaffTransferRequestList()
        {
            if (!IsAdminUser())
            {
                return RedirectToAction("StaffList");
            }

            List<StaffTransferRequestModel> requests = GetIncomingTransferRequests();
            return View(requests);
        }

        public IActionResult ExportToExcel()
        {
            if (!IsAdminUser())
            {
                return RedirectToAction("DashBoard", "DashBoard");
            }

            try
            {
                DataTable dt = new DataTable();

                SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "PR_Staff_SelectAll";
                cmd.Parameters.AddWithValue("@searchtext", DBNull.Value);

                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                dt.Load(dr);
                dr.Close();
                con.Close();

                using (XLWorkbook workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Staff");

                    for (int i = 0; i < dt.Columns.Count; i++)
                    {
                        worksheet.Cell(1, i + 1).Value = dt.Columns[i].ColumnName;
                        worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                    }

                    for (int row = 0; row < dt.Rows.Count; row++)
                    {
                        for (int col = 0; col < dt.Columns.Count; col++)
                        {
                            worksheet.Cell(row + 2, col + 1).Value = dt.Rows[row][col]?.ToString();
                        }
                    }

                    worksheet.Columns().AdjustToContents();

                    using (MemoryStream stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        byte[] content = stream.ToArray();

                        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "StaffList.xlsx");
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error exporting data: " + ex.Message;
                return RedirectToAction("StaffList");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]

        public IActionResult Save(StaffModel model)
        {
            if (!IsAdminUser())
            {
                return RedirectToAction("DashBoard", "DashBoard");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.DepartmentDropDown = FillDepartmentDropDown();
                return View("StaffAddEdit", model);
            }

            SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            con.Open();
            SqlTransaction transaction = con.BeginTransaction();

            try
            {
                if (model.StaffID == 0)
                {
                    SqlCommand checkCmd = new SqlCommand();
                    checkCmd.Connection = con;
                    checkCmd.Transaction = transaction;
                    checkCmd.CommandText = "SELECT COUNT(*) FROM MOM_Staff WHERE EmailAddress = @EmailAddress";
                    checkCmd.CommandType = CommandType.Text;
                    checkCmd.Parameters.AddWithValue("@EmailAddress", model.EmailAddress);

                    int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                    if (count > 0)
                    {
                        transaction.Rollback();
                        con.Close();
                        ModelState.AddModelError("EmailAddress", "Email already exists.");
                        ViewBag.DepartmentDropDown = FillDepartmentDropDown();
                        return View("StaffAddEdit", model);
                    }
                }
                else
                {
                    SqlCommand checkCmd = new SqlCommand();
                    checkCmd.Connection = con;
                    checkCmd.Transaction = transaction;
                    checkCmd.CommandText = "SELECT COUNT(*) FROM MOM_Staff WHERE EmailAddress = @EmailAddress AND StaffID <> @StaffID";
                    checkCmd.CommandType = CommandType.Text;
                    checkCmd.Parameters.AddWithValue("@EmailAddress", model.EmailAddress);
                    checkCmd.Parameters.AddWithValue("@StaffID", model.StaffID);

                    int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                    if (count > 0)
                    {
                        transaction.Rollback();
                        con.Close();
                        ModelState.AddModelError("EmailAddress", "Email already exists.");
                        ViewBag.DepartmentDropDown = FillDepartmentDropDown();
                        return View("StaffAddEdit", model);
                    }
                }

                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.Transaction = transaction;
                cmd.CommandType = CommandType.StoredProcedure;

                if (model.StaffID == 0)
                {
                    cmd.CommandText = "PR_Staff_Insert";
                    cmd.Parameters.AddWithValue("@DepartmentID", model.DepartmentID);
                    cmd.Parameters.AddWithValue("@StaffName", model.StaffName);
                    cmd.Parameters.AddWithValue("@MobileNo", model.MobileNo);
                    cmd.Parameters.AddWithValue("@EmailAddress", model.EmailAddress);
                    cmd.Parameters.AddWithValue("@Remarks", model.Remarks ?? string.Empty);
                    cmd.Parameters.AddWithValue("@Modified", DateTime.Now);
                    model.StaffID = Convert.ToInt32(cmd.ExecuteScalar());
                }
                else
                {
                    cmd.CommandText = "PR_Staff_UpdateByPK";
                    cmd.Parameters.AddWithValue("@StaffID", model.StaffID);
                    cmd.Parameters.AddWithValue("@DepartmentID", model.DepartmentID);
                    cmd.Parameters.AddWithValue("@StaffName", model.StaffName);
                    cmd.Parameters.AddWithValue("@MobileNo", model.MobileNo);
                    cmd.Parameters.AddWithValue("@EmailAddress", model.EmailAddress);
                    cmd.Parameters.AddWithValue("@Remarks", model.Remarks ?? string.Empty);
                    cmd.ExecuteNonQuery();
                }

                string companyName = GetCurrentCompanyName();
                string password = GenerateStaffPassword(companyName, model.StaffID, model.StaffName);
                string loginUserName = GenerateStaffUserName(model.StaffName, model.StaffID);

                SqlCommand userCmd = new SqlCommand();
                userCmd.Connection = con;
                userCmd.Transaction = transaction;
                userCmd.CommandType = CommandType.StoredProcedure;
                userCmd.CommandText = "PR_MST_User_UpsertForStaff";
                userCmd.Parameters.AddWithValue("@StaffID", model.StaffID);
                userCmd.Parameters.AddWithValue("@UserName", loginUserName);
                userCmd.Parameters.AddWithValue("@Email", model.EmailAddress);
                userCmd.Parameters.AddWithValue("@Password", password);
                userCmd.Parameters.AddWithValue("@ContactNo", model.MobileNo);
                userCmd.Parameters.AddWithValue("@City", string.Empty);
                userCmd.Parameters.AddWithValue("@CompanyName", companyName);
                userCmd.Parameters.AddWithValue("@DepartmentID", model.DepartmentID);
                userCmd.Parameters.AddWithValue("@Modified", DateTime.Now);
                userCmd.ExecuteNonQuery();

                transaction.Commit();
                con.Close();

                TempData["SuccessMessage"] = model.StaffID == 0
                    ? $"Staff added successfully. Auto password: {password}"
                    : "Staff updated successfully. User account updated automatically.";

                return RedirectToAction("StaffList");
            }
            catch
            {
                transaction.Rollback();
                con.Close();
                TempData["ErrorMessage"] = "Staff save failed.";
                ViewBag.DepartmentDropDown = FillDepartmentDropDown();
                return View("StaffAddEdit", model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int StaffID)
        {
            if (!IsAdminUser())
            {
                return RedirectToAction("DashBoard", "DashBoard");
            }

            try
            {
                SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandText = "PR_Staff_DeleteByPK";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@StaffID", StaffID);

                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();
                TempData["SuccessMessage"] = "Staff deleted successfully.";
            }
            catch
            {
                TempData["DeleteError"] = "FK violation: linked data exists.";
            }

            return RedirectToAction("StaffList");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SubmitTransferRequest(int staffID, string targetCompanyName, int? targetDepartmentID, string? transferReason, IFormFile? transferDocument)
        {
            if (!IsAdminUser())
            {
                return RedirectToAction("StaffList");
            }

            StaffModel? staff = GetStaffForTransfer(staffID);
            if (staff == null || string.IsNullOrWhiteSpace(staff.CompanyName) || !string.Equals(staff.CompanyName, GetCurrentCompanyName(), StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Staff record not found.";
                return RedirectToAction("StaffList");
            }

            if (string.IsNullOrWhiteSpace(targetCompanyName) || !targetDepartmentID.HasValue)
            {
                TempData["ErrorMessage"] = "Target company and department are required.";
                return RedirectToAction("StaffView", new { id = staffID });
            }

            if (string.Equals(staff.CompanyName, targetCompanyName, StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Please choose another company for transfer.";
                return RedirectToAction("StaffView", new { id = staffID });
            }

            if (!IsDepartmentInCompany(targetDepartmentID.Value, targetCompanyName))
            {
                TempData["ErrorMessage"] = "Target department does not belong to selected company.";
                return RedirectToAction("StaffView", new { id = staffID });
            }

            if (transferDocument == null || transferDocument.Length == 0)
            {
                TempData["ErrorMessage"] = "Please upload transfer document.";
                return RedirectToAction("StaffView", new { id = staffID });
            }

            string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "transfer-documents");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            string extension = Path.GetExtension(transferDocument.FileName);
            string fileName = $"transfer_{staffID}_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid():N}{extension}";
            string filePath = Path.Combine(uploadsFolder, fileName);

            using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
            {
                transferDocument.CopyTo(fileStream);
            }

            string documentPath = "/transfer-documents/" + fileName;
            int requestedByUserId = HttpContext.Session.GetInt32("UserID") ?? 0;
            int? userId = GetUserIdByStaffId(staffID);

            try
            {
                SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandText = "PR_MST_StaffTransferRequest_Insert";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@StaffID", staffID);
                cmd.Parameters.AddWithValue("@UserID", userId.HasValue ? userId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@SourceCompanyName", staff.CompanyName ?? string.Empty);
                cmd.Parameters.AddWithValue("@SourceDepartmentID", staff.DepartmentID);
                cmd.Parameters.AddWithValue("@TargetCompanyName", targetCompanyName);
                cmd.Parameters.AddWithValue("@TargetDepartmentID", targetDepartmentID.Value);
                cmd.Parameters.AddWithValue("@RequestedByUserID", requestedByUserId);
                cmd.Parameters.AddWithValue("@TransferReason", transferReason ?? string.Empty);
                cmd.Parameters.AddWithValue("@DocumentPath", documentPath);
                cmd.Parameters.AddWithValue("@Modified", DateTime.Now);
                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();

                InsertAdminNotification(
                    targetCompanyName,
                    "StaffTransferRequest",
                    "Staff transfer requested",
                    $"{staff.StaffName} transfer request arrived from {staff.CompanyName}.",
                    requestedByUserId);

                TempData["SuccessMessage"] = "Staff transfer request sent successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Transfer request failed: " + ex.Message;
            }

            return RedirectToAction("StaffView", new { id = staffID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApproveStaffTransferRequest(int staffTransferRequestID, string? adminRemarks)
        {
            if (!IsAdminUser())
            {
                return RedirectToAction("StaffList");
            }

            int? adminUserId = HttpContext.Session.GetInt32("UserID");
            if (!adminUserId.HasValue)
            {
                return RedirectToAction("Login", "Auth");
            }

            try
            {
                SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandText = "PR_MST_StaffTransferRequest_Approve";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@StaffTransferRequestID", staffTransferRequestID);
                cmd.Parameters.AddWithValue("@AdminUserID", adminUserId.Value);
                cmd.Parameters.AddWithValue("@AdminRemarks", string.IsNullOrWhiteSpace(adminRemarks) ? DBNull.Value : adminRemarks);
                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();

                TempData["SuccessMessage"] = "Staff transfer approved successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Approve failed: " + ex.Message;
            }

            return RedirectToAction("StaffTransferRequestList");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RejectStaffTransferRequest(int staffTransferRequestID, string? adminRemarks)
        {
            if (!IsAdminUser())
            {
                return RedirectToAction("StaffList");
            }

            int? adminUserId = HttpContext.Session.GetInt32("UserID");
            if (!adminUserId.HasValue)
            {
                return RedirectToAction("Login", "Auth");
            }

            try
            {
                SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandText = "PR_MST_StaffTransferRequest_Reject";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@StaffTransferRequestID", staffTransferRequestID);
                cmd.Parameters.AddWithValue("@AdminUserID", adminUserId.Value);
                cmd.Parameters.AddWithValue("@AdminRemarks", string.IsNullOrWhiteSpace(adminRemarks) ? DBNull.Value : adminRemarks);
                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();

                TempData["SuccessMessage"] = "Staff transfer rejected.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Reject failed: " + ex.Message;
            }

            return RedirectToAction("StaffTransferRequestList");
        }


        public List<SelectListItem> FillDepartmentDropDown()
        {
            List<SelectListItem> list = new List<SelectListItem>();
            SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_MOM_DEPARTMENT_DDL";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@CompanyName", GetCurrentCompanyName());
            con.Open();
            SqlDataReader sdr = cmd.ExecuteReader();
            while (sdr.Read())
            {
                string value = Convert.ToString(sdr["DepartmentID"]) ?? string.Empty;
                bool exists = false;
                foreach (var item in list)
                {
                    if (item.Value == value)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    list.Add(new SelectListItem
                    {
                        Text = sdr["DepartmentName"].ToString(),
                        Value = value
                    });
                }
            }
            sdr.Close();
            con.Close();
            return list;
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

        public bool IsAdminUser()
        {
            return string.Equals(HttpContext.Session.GetString("UserRole"), "Admin", StringComparison.OrdinalIgnoreCase);
        }

        public List<SelectListItem> FillCompanyDropDown()
        {
            List<SelectListItem> list = new List<SelectListItem>();
            SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_MST_Company_DDL";
            cmd.CommandType = CommandType.StoredProcedure;
            con.Open();
            SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                string companyName = reader["CompanyName"].ToString() ?? string.Empty;
                list.Add(new SelectListItem
                {
                    Text = companyName,
                    Value = companyName
                });
            }
            reader.Close();
            con.Close();
            return list;
        }

        public StaffModel? GetStaffForTransfer(int staffId)
        {
            StaffModel model = new StaffModel();
            SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_Staff_SelectByPK";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@StaffID", staffId);
            con.Open();
            SqlDataReader reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                model.StaffID = Convert.ToInt32(reader["StaffID"]);
                model.DepartmentID = Convert.ToInt32(reader["DepartmentID"]);
                model.DepartmentName = reader["DepartmentName"].ToString();
                model.CompanyName = reader["CompanyName"].ToString();
                model.StaffName = reader["StaffName"].ToString();
                model.MobileNo = reader["MobileNo"].ToString();
                model.EmailAddress = reader["EmailAddress"].ToString();
                model.Remarks = reader["Remarks"].ToString();
            }
            reader.Close();
            con.Close();
            return model.StaffID == 0 ? null : model;
        }

        public bool IsDepartmentInCompany(int departmentId, string companyName)
        {
            SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "SELECT COUNT(*) FROM MOM_Department WHERE DepartmentID = @DepartmentID AND CompanyName = @CompanyName";
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.AddWithValue("@DepartmentID", departmentId);
            cmd.Parameters.AddWithValue("@CompanyName", companyName);
            con.Open();
            int count = Convert.ToInt32(cmd.ExecuteScalar());
            con.Close();
            return count > 0;
        }

        public int? GetUserIdByStaffId(int staffId)
        {
            SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "SELECT TOP 1 UserID FROM MST_User WHERE StaffID = @StaffID";
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.AddWithValue("@StaffID", staffId);
            con.Open();
            object? result = cmd.ExecuteScalar();
            con.Close();
            return result == null || result == DBNull.Value ? null : Convert.ToInt32(result);
        }

        public void InsertAdminNotification(string? companyName, string notificationType, string title, string message, int? userId)
        {
            SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_MST_AdminNotification_Insert";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@CompanyName", string.IsNullOrWhiteSpace(companyName) ? DBNull.Value : companyName);
            cmd.Parameters.AddWithValue("@NotificationType", notificationType);
            cmd.Parameters.AddWithValue("@Title", title);
            cmd.Parameters.AddWithValue("@Message", message);
            cmd.Parameters.AddWithValue("@RelatedUserID", userId.HasValue ? userId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@Modified", DateTime.Now);
            con.Open();
            cmd.ExecuteNonQuery();
            con.Close();
        }

        public List<StaffTransferRequestModel> GetIncomingTransferRequests()
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
                StaffTransferRequestModel request = new StaffTransferRequestModel();
                request.StaffTransferRequestID = Convert.ToInt32(reader["StaffTransferRequestID"]);
                request.StaffID = Convert.ToInt32(reader["StaffID"]);
                request.UserID = reader["UserID"] == DBNull.Value ? null : Convert.ToInt32(reader["UserID"]);
                request.StaffName = reader["StaffName"].ToString() ?? string.Empty;
                request.EmailAddress = reader["EmailAddress"].ToString() ?? string.Empty;
                request.MobileNo = reader["MobileNo"].ToString() ?? string.Empty;
                request.SourceCompanyName = reader["SourceCompanyName"].ToString() ?? string.Empty;
                request.SourceDepartmentID = Convert.ToInt32(reader["SourceDepartmentID"]);
                request.SourceDepartmentName = reader["SourceDepartmentName"].ToString() ?? string.Empty;
                request.TargetCompanyName = reader["TargetCompanyName"].ToString() ?? string.Empty;
                request.TargetDepartmentID = Convert.ToInt32(reader["TargetDepartmentID"]);
                request.TargetDepartmentName = reader["TargetDepartmentName"].ToString() ?? string.Empty;
                request.RequestedByUserID = Convert.ToInt32(reader["RequestedByUserID"]);
                request.RequestedByUserName = reader["RequestedByUserName"].ToString() ?? string.Empty;
                request.TransferReason = reader["TransferReason"].ToString() ?? string.Empty;
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

        public string GenerateStaffPassword(string companyName, int staffId, string? staffName)
        {
            string cleanCompany = new string((companyName ?? string.Empty).Where(char.IsLetterOrDigit).ToArray());
            string cleanStaff = new string((staffName ?? string.Empty).Where(char.IsLetterOrDigit).ToArray());

            if (string.IsNullOrWhiteSpace(cleanCompany))
            {
                cleanCompany = "MOM";
            }

            if (string.IsNullOrWhiteSpace(cleanStaff))
            {
                cleanStaff = "Staff";
            }

            string companyPart = cleanCompany.Length > 4 ? cleanCompany.Substring(0, 4) : cleanCompany;
            string staffPart = cleanStaff.Length > 4 ? cleanStaff.Substring(0, 4) : cleanStaff;
            return $"{char.ToUpper(companyPart[0])}{companyPart.Substring(1)}{staffId}{char.ToUpper(staffPart[0])}{staffPart.Substring(1)}@1a";
        }

        public string GenerateStaffUserName(string? staffName, int staffId)
        {
            string cleanStaff = new string((staffName ?? string.Empty).Where(char.IsLetterOrDigit).ToArray()).ToLower();
            if (string.IsNullOrWhiteSpace(cleanStaff))
            {
                cleanStaff = "staff";
            }

            string staffPart = cleanStaff.Length > 10 ? cleanStaff.Substring(0, 10) : cleanStaff;
            return $"{staffPart}{staffId}";
        }
        #endregion
    }
}










