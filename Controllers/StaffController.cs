using Meeting_Of_Minutes.Models;
using Meeting_Of_Minutes.Security;
using Meeting_Of_Minutes.Services;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Mvc.Rendering;
using ClosedXML.Excel;

namespace Meeting_Of_Minutes.Controllers
{
    public class StaffController : Controller
    {
        private readonly IWebHostEnvironment _environment;

        public StaffController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

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
        public IActionResult StaffList(string? searchtext, int page = 1, string sortBy = "name", string sortDirection = "asc")
        {
            if (!IsAdminUser())
            {
                return RedirectToAction("DashBoard", "DashBoard");
            }

            searchtext = string.IsNullOrWhiteSpace(searchtext) ? null : searchtext;
            ViewBag.searchtext = searchtext;
            return View(BuildStaffPage(searchtext, page, sortBy, sortDirection));
        }

        [HttpPost]
        public IActionResult StaffList(IFormCollection formdata)
        {
            if (!IsAdminUser())
            {
                return RedirectToAction("DashBoard", "DashBoard");
            }

            string? searchtext = formdata["searchtext"].ToString();
            return RedirectToAction(nameof(StaffList), new { searchtext });
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

        public PagedListViewModel<StaffModel> BuildStaffPage(string? searchtext, int page, string? sortBy, string? sortDirection)
        {
            IEnumerable<StaffModel> query = GetAllStaff(searchtext);
            bool isDesc = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

            query = (sortBy ?? "name").ToLowerInvariant() switch
            {
                "department" => isDesc ? query.OrderByDescending(x => x.DepartmentName).ThenBy(x => x.StaffName) : query.OrderBy(x => x.DepartmentName).ThenBy(x => x.StaffName),
                "email" => isDesc ? query.OrderByDescending(x => x.EmailAddress).ThenBy(x => x.StaffName) : query.OrderBy(x => x.EmailAddress).ThenBy(x => x.StaffName),
                _ => isDesc ? query.OrderByDescending(x => x.StaffName) : query.OrderBy(x => x.StaffName)
            };

            List<StaffModel> ordered = query.ToList();
            const int pageSize = 10;
            page = Math.Max(page, 1);

            return new PagedListViewModel<StaffModel>
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
                List<StaffModel> staffItems = GetAllStaff(null);

                using (XLWorkbook workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Staff");
                    string[] headers = { "StaffID", "DepartmentName", "CompanyName", "StaffName", "MobileNo", "EmailAddress", "Remarks" };
                    for (int i = 0; i < headers.Length; i++)
                    {
                        worksheet.Cell(1, i + 1).Value = headers[i];
                        worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                    }

                    for (int row = 0; row < staffItems.Count; row++)
                    {
                        StaffModel item = staffItems[row];
                        worksheet.Cell(row + 2, 1).Value = item.StaffID;
                        worksheet.Cell(row + 2, 2).Value = item.DepartmentName;
                        worksheet.Cell(row + 2, 3).Value = item.CompanyName;
                        worksheet.Cell(row + 2, 4).Value = item.StaffName;
                        worksheet.Cell(row + 2, 5).Value = item.MobileNo;
                        worksheet.Cell(row + 2, 6).Value = item.EmailAddress;
                        worksheet.Cell(row + 2, 7).Value = item.Remarks;
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
            catch
            {
                TempData["ErrorMessage"] = "Error exporting data. Please try again.";
                return RedirectToAction("StaffList");
            }
        }

        public IActionResult DownloadImportTemplate()
        {
            byte[] content = ExcelImportService.BuildTemplate(
                "StaffImport",
                new[] { "DepartmentName", "StaffName", "MobileNo", "EmailAddress", "Remarks" },
                new List<IReadOnlyList<string>>
                {
                    new[] { "Human Resources", "Neel Patel", "9876543210", "neel@example.com", "Core team member" }
                });

            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "StaffImportTemplate.xlsx");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult ValidateImportTemplate(IFormFile? excelFile)
        {
            bool isValid = ExcelImportService.ValidateImportFile(excelFile, new[] { "DepartmentName", "StaffName", "MobileNo", "EmailAddress", "Remarks" }, out string message);
            return Json(new { ok = isValid, message });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ImportFromExcel(IFormFile? excelFile)
        {
            if (!IsAdminUser())
            {
                return RedirectToAction("DashBoard", "DashBoard");
            }

            if (!ExcelImportService.IsExcelFile(excelFile))
            {
                TempData["ErrorMessage"] = $"Please upload a valid Excel file up to {ExcelImportService.MaxFileSizeBytes / (1024 * 1024)} MB.";
                return RedirectToAction("StaffList");
            }

            if (!ExcelImportService.HasRequiredHeaders(excelFile!, new[] { "DepartmentName", "StaffName", "MobileNo", "EmailAddress", "Remarks" }, out string headerMessage))
            {
                TempData["ErrorMessage"] = headerMessage;
                return RedirectToAction("StaffList");
            }

            List<Dictionary<string, string>> rows = ExcelImportService.ReadRows(excelFile!);
            if (rows.Count == 0)
            {
                TempData["ErrorMessage"] = "Excel file is empty.";
                return RedirectToAction("StaffList");
            }

            int importedCount = 0;
            int skippedCount = 0;
            string companyName = GetCurrentCompanyName();
            List<ImportReportRowModel> reportRows = new List<ImportReportRowModel>();

            using SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            con.Open();

            for (int index = 0; index < rows.Count; index++)
            {
                Dictionary<string, string> row = rows[index];
                string departmentName = ExcelImportService.GetValue(row, "DepartmentName");
                string staffName = ExcelImportService.GetValue(row, "StaffName");
                string mobileNo = ExcelImportService.GetValue(row, "MobileNo");
                string emailAddress = ExcelImportService.GetValue(row, "EmailAddress");
                string remarks = ExcelImportService.GetValue(row, "Remarks");
                string summary = $"{staffName} | {emailAddress}";

                if (string.IsNullOrWhiteSpace(departmentName) ||
                    string.IsNullOrWhiteSpace(staffName) ||
                    string.IsNullOrWhiteSpace(mobileNo) ||
                    string.IsNullOrWhiteSpace(emailAddress))
                {
                    skippedCount++;
                    reportRows.Add(new ImportReportRowModel { RowNumber = index + 2, Status = "Skipped", Message = "DepartmentName, StaffName, MobileNo and EmailAddress are required.", DataSummary = summary });
                    continue;
                }

                int? departmentId = GetDepartmentIdByNameForImport(con, departmentName, companyName);
                if (!departmentId.HasValue)
                {
                    skippedCount++;
                    reportRows.Add(new ImportReportRowModel { RowNumber = index + 2, Status = "Skipped", Message = "Department was not found in current company.", DataSummary = summary });
                    continue;
                }

                using SqlCommand emailCheckCmd = new SqlCommand("SELECT COUNT(*) FROM MOM_Staff WHERE EmailAddress = @EmailAddress", con);
                emailCheckCmd.Parameters.AddWithValue("@EmailAddress", emailAddress);
                if (Convert.ToInt32(emailCheckCmd.ExecuteScalar()) > 0)
                {
                    skippedCount++;
                    reportRows.Add(new ImportReportRowModel { RowNumber = index + 2, Status = "Skipped", Message = "Email already exists.", DataSummary = summary });
                    continue;
                }

                using SqlTransaction transaction = con.BeginTransaction();
                try
                {
                    using SqlCommand staffCmd = new SqlCommand("PR_Staff_Insert", con, transaction);
                    staffCmd.CommandType = CommandType.StoredProcedure;
                    staffCmd.Parameters.AddWithValue("@DepartmentID", departmentId.Value);
                    staffCmd.Parameters.AddWithValue("@StaffName", staffName);
                    staffCmd.Parameters.AddWithValue("@MobileNo", mobileNo);
                    staffCmd.Parameters.AddWithValue("@EmailAddress", emailAddress);
                    staffCmd.Parameters.AddWithValue("@Remarks", remarks);
                    staffCmd.Parameters.AddWithValue("@Modified", DateTime.Now);
                    int staffId = Convert.ToInt32(staffCmd.ExecuteScalar());

                    string password = GenerateStaffPassword(companyName, staffId, staffName);
                    string loginUserName = GenerateStaffUserName(staffName, staffId);
                    string passwordHash = PasswordSecurity.HashPassword(password);

                    using SqlCommand userCmd = new SqlCommand("PR_MST_User_UpsertForStaff", con, transaction);
                    userCmd.CommandType = CommandType.StoredProcedure;
                    userCmd.Parameters.AddWithValue("@StaffID", staffId);
                    userCmd.Parameters.AddWithValue("@UserName", loginUserName);
                    userCmd.Parameters.AddWithValue("@Email", emailAddress);
                    userCmd.Parameters.AddWithValue("@Password", string.Empty);
                    userCmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
                    userCmd.Parameters.AddWithValue("@ContactNo", mobileNo);
                    userCmd.Parameters.AddWithValue("@City", string.Empty);
                    userCmd.Parameters.AddWithValue("@CompanyName", companyName);
                    userCmd.Parameters.AddWithValue("@DepartmentID", departmentId.Value);
                    userCmd.Parameters.AddWithValue("@Modified", DateTime.Now);
                    userCmd.ExecuteNonQuery();

                    transaction.Commit();
                    importedCount++;
                    reportRows.Add(new ImportReportRowModel { RowNumber = index + 2, Status = "Imported", Message = "Staff and linked user account created successfully.", DataSummary = summary });
                }
                catch
                {
                    transaction.Rollback();
                    skippedCount++;
                    reportRows.Add(new ImportReportRowModel { RowNumber = index + 2, Status = "Skipped", Message = "Staff import failed during save.", DataSummary = summary });
                }
            }

            AuditLogService.Log(
                companyName,
                HttpContext.Session.GetInt32("UserID"),
                HttpContext.Session.GetString("UserName"),
                HttpContext.Session.GetString("UserRole"),
                "Import",
                "Staff",
                null,
                "Staff imported from Excel",
                $"{importedCount} staff records imported and {skippedCount} skipped.");

            TempData["ImportReportPath"] = ImportReportService.SaveReport("StaffImport", companyName, HttpContext.Session.GetInt32("UserID"), HttpContext.Session.GetString("UserName"), importedCount, skippedCount, reportRows);
            TempData["SuccessMessage"] = $"Staff import completed. Imported: {importedCount}, Skipped: {skippedCount}. User accounts were auto-created for imported staff.";
            return RedirectToAction("StaffList");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]

        public IActionResult Save(StaffModel model)
        {
            if (!IsAdminUser())
            {
                return RedirectToAction("DashBoard", "DashBoard");
            }

            bool isNewStaff = model.StaffID == 0;

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
                string passwordHash = PasswordSecurity.HashPassword(password);

                SqlCommand userCmd = new SqlCommand();
                userCmd.Connection = con;
                userCmd.Transaction = transaction;
                userCmd.CommandType = CommandType.StoredProcedure;
                userCmd.CommandText = "PR_MST_User_UpsertForStaff";
                userCmd.Parameters.AddWithValue("@StaffID", model.StaffID);
                userCmd.Parameters.AddWithValue("@UserName", loginUserName);
                userCmd.Parameters.AddWithValue("@Email", model.EmailAddress);
                userCmd.Parameters.AddWithValue("@Password", string.Empty);
                userCmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
                userCmd.Parameters.AddWithValue("@ContactNo", model.MobileNo);
                userCmd.Parameters.AddWithValue("@City", string.Empty);
                userCmd.Parameters.AddWithValue("@CompanyName", companyName);
                userCmd.Parameters.AddWithValue("@DepartmentID", model.DepartmentID);
                userCmd.Parameters.AddWithValue("@Modified", DateTime.Now);
                userCmd.ExecuteNonQuery();

                transaction.Commit();
                con.Close();

                AuditLogService.Log(
                    companyName,
                    HttpContext.Session.GetInt32("UserID"),
                    HttpContext.Session.GetString("UserName"),
                    HttpContext.Session.GetString("UserRole"),
                    isNewStaff ? "Create" : "Update",
                    "Staff",
                    model.StaffID.ToString(),
                    isNewStaff ? "Staff created" : "Staff updated",
                    $"{model.StaffName} staff record was {(isNewStaff ? "created" : "updated")}.");

                TempData["SuccessMessage"] = isNewStaff
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

            if (!FileSecurityService.TrySaveDocument(_environment, transferDocument, "transfer-documents", $"transfer_{staffID}", out string documentPath, out string uploadError))
            {
                TempData["ErrorMessage"] = uploadError;
                return RedirectToAction("StaffView", new { id = staffID });
            }
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
                AuditLogService.Log(
                    staff.CompanyName,
                    requestedByUserId,
                    HttpContext.Session.GetString("UserName"),
                    HttpContext.Session.GetString("UserRole"),
                    "Submit",
                    "StaffTransferRequest",
                    staffID.ToString(),
                    "Staff transfer request submitted",
                    $"{staff.StaffName} transfer was requested to {targetCompanyName}.");

                TempData["SuccessMessage"] = "Staff transfer request sent successfully.";
            }
            catch
            {
                TempData["ErrorMessage"] = "Transfer request failed. Please try again.";
            }

            return RedirectToAction("StaffView", new { id = staffID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApproveStaffTransferRequest(int staffTransferRequestID, string? adminRemarks, string? returnUrl)
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
                AuditLogService.Log(
                    HttpContext.Session.GetString("CompanyName"),
                    adminUserId.Value,
                    HttpContext.Session.GetString("UserName"),
                    HttpContext.Session.GetString("UserRole"),
                    "Approve",
                    "StaffTransferRequest",
                    staffTransferRequestID.ToString(),
                    "Staff transfer approved",
                    $"Staff transfer request #{staffTransferRequestID} was approved.");

                TempData["SuccessMessage"] = "Staff transfer approved successfully.";
            }
            catch
            {
                TempData["ErrorMessage"] = "Approve failed. Please try again.";
            }

            return RedirectToLocal(returnUrl, "StaffTransferRequestList");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RejectStaffTransferRequest(int staffTransferRequestID, string? adminRemarks, string? returnUrl)
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
                AuditLogService.Log(
                    HttpContext.Session.GetString("CompanyName"),
                    adminUserId.Value,
                    HttpContext.Session.GetString("UserName"),
                    HttpContext.Session.GetString("UserRole"),
                    "Reject",
                    "StaffTransferRequest",
                    staffTransferRequestID.ToString(),
                    "Staff transfer rejected",
                    $"Staff transfer request #{staffTransferRequestID} was rejected.");

                TempData["SuccessMessage"] = "Staff transfer rejected.";
            }
            catch
            {
                TempData["ErrorMessage"] = "Reject failed. Please try again.";
            }

            return RedirectToLocal(returnUrl, "StaffTransferRequestList");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BulkApproveStaffTransferRequests(List<int>? selectedRequestIds, string? adminRemarks, string? returnUrl)
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

            if (selectedRequestIds == null || selectedRequestIds.Count == 0)
            {
                TempData["ErrorMessage"] = "Select at least one transfer request.";
                return RedirectToLocal(returnUrl, "StaffTransferRequestList");
            }

            int successCount = 0;

            foreach (int requestId in selectedRequestIds.Distinct())
            {
                try
                {
                    using SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
                    using SqlCommand cmd = new SqlCommand("PR_MST_StaffTransferRequest_Approve", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@StaffTransferRequestID", requestId);
                    cmd.Parameters.AddWithValue("@AdminUserID", adminUserId.Value);
                    cmd.Parameters.AddWithValue("@AdminRemarks", string.IsNullOrWhiteSpace(adminRemarks) ? DBNull.Value : adminRemarks);
                    con.Open();
                    cmd.ExecuteNonQuery();

                    AuditLogService.Log(HttpContext.Session.GetString("CompanyName"), adminUserId.Value, HttpContext.Session.GetString("UserName"), HttpContext.Session.GetString("UserRole"), "Approve", "StaffTransferRequest", requestId.ToString(), "Transfer request approved", $"Staff transfer request #{requestId} was approved in bulk.");
                    successCount++;
                }
                catch
                {
                }
            }

            TempData[successCount > 0 ? "SuccessMessage" : "ErrorMessage"] = successCount > 0
                ? $"{successCount} transfer request(s) approved successfully."
                : "Bulk approve failed for the selected transfer requests.";

            return RedirectToLocal(returnUrl, "StaffTransferRequestList");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BulkRejectStaffTransferRequests(List<int>? selectedRequestIds, string? adminRemarks, string? returnUrl)
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

            if (selectedRequestIds == null || selectedRequestIds.Count == 0)
            {
                TempData["ErrorMessage"] = "Select at least one transfer request.";
                return RedirectToLocal(returnUrl, "StaffTransferRequestList");
            }

            int successCount = 0;

            foreach (int requestId in selectedRequestIds.Distinct())
            {
                try
                {
                    using SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
                    using SqlCommand cmd = new SqlCommand("PR_MST_StaffTransferRequest_Reject", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@StaffTransferRequestID", requestId);
                    cmd.Parameters.AddWithValue("@AdminUserID", adminUserId.Value);
                    cmd.Parameters.AddWithValue("@AdminRemarks", string.IsNullOrWhiteSpace(adminRemarks) ? DBNull.Value : adminRemarks);
                    con.Open();
                    cmd.ExecuteNonQuery();

                    AuditLogService.Log(HttpContext.Session.GetString("CompanyName"), adminUserId.Value, HttpContext.Session.GetString("UserName"), HttpContext.Session.GetString("UserRole"), "Reject", "StaffTransferRequest", requestId.ToString(), "Transfer request rejected", $"Staff transfer request #{requestId} was rejected in bulk.");
                    successCount++;
                }
                catch
                {
                }
            }

            TempData[successCount > 0 ? "SuccessMessage" : "ErrorMessage"] = successCount > 0
                ? $"{successCount} transfer request(s) rejected."
                : "Bulk reject failed for the selected transfer requests.";

            return RedirectToLocal(returnUrl, "StaffTransferRequestList");
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
            int? currentDepartmentId = HttpContext.Session.GetInt32("DepartmentID");
            bool isSuperAdmin = RoleAccessService.IsSuperAdmin(HttpContext.Session.GetString("UserRole"));

            if (string.IsNullOrWhiteSpace(companyName))
            {
                return departmentIds;
            }

            if (!isSuperAdmin && currentDepartmentId.HasValue)
            {
                departmentIds.Add(currentDepartmentId.Value);
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

        public int? GetDepartmentIdByNameForImport(SqlConnection con, string departmentName, string companyName)
        {
            using SqlCommand cmd = new SqlCommand("SELECT TOP 1 DepartmentID FROM MOM_Department WHERE DepartmentName = @DepartmentName AND CompanyName = @CompanyName", con);
            cmd.Parameters.AddWithValue("@DepartmentName", departmentName);
            cmd.Parameters.AddWithValue("@CompanyName", companyName);
            object? result = cmd.ExecuteScalar();
            if (result == null || result == DBNull.Value)
            {
                return null;
            }

            int departmentId = Convert.ToInt32(result);
            return GetAllowedDepartmentIdsForCompany().Contains(departmentId) ? departmentId : null;
        }

        public string GetCurrentCompanyName()
        {
            return HttpContext.Session.GetString("CompanyName") ?? string.Empty;
        }

        public bool IsAdminUser()
        {
            return RoleAccessService.IsAdminOrHigher(HttpContext.Session.GetString("UserRole"));
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
            HashSet<int> allowedDepartmentIds = GetAllowedDepartmentIdsForCompany();
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
                if (allowedDepartmentIds.Contains(request.TargetDepartmentID))
                {
                    requests.Add(request);
                }
            }
            reader.Close();
            con.Close();
            return requests;
        }

        public string GenerateStaffPassword(string companyName, int staffId, string? staffName)
        {
            return PasswordSecurity.GenerateTemporaryPassword();
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

        public IActionResult RedirectToLocal(string? returnUrl, string fallbackAction)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(fallbackAction);
        }
        #endregion
    }
}












