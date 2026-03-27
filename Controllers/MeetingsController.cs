using Meeting_Of_Minutes.Models;
using Meeting_Of_Minutes.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;
using Microsoft.Data.SqlClient;
using ClosedXML.Excel;

namespace Meeting_Of_Minutes.Controllers
{
    public class MeetingsController : Controller
    {
        private readonly IWebHostEnvironment _env;

        public MeetingsController(IWebHostEnvironment env)
        {
            _env = env;
        }

        #region AddEdit
        public IActionResult MeetingsAddEdit(int? id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("MeetingsList");
            }

            ViewBag.DepartmentDropDown = FillDepartmentDropDown();
            ViewBag.MeetingTypeDropDown = FillMeetingTypeDropDown();
            ViewBag.MeetingVenueDropDown = FillMeetingVenueDropdown();

            MeetingsModel model = new MeetingsModel();

            if (id.HasValue)
            {
                SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandText = "PR_Meetings_SelectByPK";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@MeetingID", id.Value);

                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    model.MeetingID = Convert.ToInt32(reader["MeetingID"]);
                    model.MeetingDate = reader["MeetingDate"] as DateTime?;
                    model.MeetingVenueID = reader["MeetingVenueID"] as int?;
                    model.MeetingTypeID = reader["MeetingTypeID"] as int?;
                    model.DepartmentID = reader["DepartmentID"] as int?;
                    model.MeetingDescription = reader["MeetingDescription"].ToString();
                    model.DocumentPath = reader["DocumentPath"].ToString();
                    model.IsCancelled = reader["IsCancelled"] == DBNull.Value ? false : Convert.ToBoolean(reader["IsCancelled"]);
                    model.CancellationDateTime = reader["CancellationDateTime"] as DateTime?;
                    model.CancellationReason = reader["CancellationReason"].ToString();
                }

                reader.Close();
                con.Close();
            }

            return View(model);
        }


        [HttpGet]
        public IActionResult MeetingsList(string? searchtext, int page = 1, string sortBy = "date", string sortDirection = "desc")
        {
            searchtext = string.IsNullOrWhiteSpace(searchtext) ? null : searchtext;
            ViewBag.searchtext = searchtext;
            return View(BuildMeetingsPage(searchtext, page, sortBy, sortDirection));
        }

        [HttpPost]
        public IActionResult MeetingsList(IFormCollection formdata)
        {
            string? searchtext = formdata["searchtext"].ToString();
            return RedirectToAction(nameof(MeetingsList), new { searchtext });
        }

        public List<MeetingsModel> GetAllMeetings(string? searchtext)
        {
            List<MeetingsModel> meetingsList = new List<MeetingsModel>();
            HashSet<int> allowedDepartmentIds = GetAllowedDepartmentIdsForCompany();

            SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_Meetings_SelectAll";
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
            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                MeetingsModel meeting = new MeetingsModel();
                meeting.MeetingID = Convert.ToInt32(reader["MeetingID"]);
                meeting.MeetingDate = reader["MeetingDate"] as DateTime?;
                meeting.DepartmentID = reader["DepartmentID"] == DBNull.Value ? null : Convert.ToInt32(reader["DepartmentID"]);
                meeting.MeetingTypeName = reader["MeetingTypeName"].ToString();
                meeting.DepartmentName = reader["DepartmentName"].ToString();
                meeting.MeetingVenueName = reader["MeetingVenueName"].ToString();
                meeting.MeetingDescription = reader["MeetingDescription"].ToString();
                meeting.DocumentPath = reader["DocumentPath"].ToString();
                meeting.IsCancelled = reader["IsCancelled"] == DBNull.Value ? false : Convert.ToBoolean(reader["IsCancelled"]);
                meeting.CancellationDateTime = reader["CancellationDateTime"] as DateTime?;
                meeting.CancellationReason = reader["CancellationReason"].ToString();

                bool canAccessMeeting = IsAdmin()
                    ? allowedDepartmentIds.Contains(meeting.DepartmentID ?? 0) && CanAccessDepartmentMeeting(meeting.DepartmentID)
                    : IsMeetingAssignedToCurrentUser(meeting.MeetingID);

                if (canAccessMeeting)
                {
                    meetingsList.Add(meeting);
                }
            }

            reader.Close();
            con.Close();

            return meetingsList;
        }

        public PagedListViewModel<MeetingsModel> BuildMeetingsPage(string? searchtext, int page, string? sortBy, string? sortDirection)
        {
            IEnumerable<MeetingsModel> query = GetAllMeetings(searchtext);
            bool isDesc = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

            query = (sortBy ?? "date").ToLowerInvariant() switch
            {
                "type" => isDesc ? query.OrderByDescending(x => x.MeetingTypeName).ThenByDescending(x => x.MeetingDate) : query.OrderBy(x => x.MeetingTypeName).ThenBy(x => x.MeetingDate),
                "department" => isDesc ? query.OrderByDescending(x => x.DepartmentName).ThenByDescending(x => x.MeetingDate) : query.OrderBy(x => x.DepartmentName).ThenBy(x => x.MeetingDate),
                "status" => isDesc ? query.OrderByDescending(GetMeetingStatusRank).ThenByDescending(x => x.MeetingDate) : query.OrderBy(GetMeetingStatusRank).ThenBy(x => x.MeetingDate),
                _ => isDesc ? query.OrderByDescending(x => x.MeetingDate) : query.OrderBy(x => x.MeetingDate)
            };

            List<MeetingsModel> ordered = query.ToList();
            const int pageSize = 10;
            page = Math.Max(page, 1);

            return new PagedListViewModel<MeetingsModel>
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


        public IActionResult MeetingsDetails(int id)
        {
            if (!IsAdmin() && !IsMeetingInUserDepartment(id))
            {
                TempData["ErrorMessage"] = "You can only view your meetings.";
                return RedirectToAction("MeetingsList");
            }

            MeetingDetailsViewModel viewModel = new MeetingDetailsViewModel();
            MeetingsModel model = new MeetingsModel();

            SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_Meetings_SelectByPK";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@MeetingID", id);

            con.Open();
            SqlDataReader reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                model.MeetingID = Convert.ToInt32(reader["MeetingID"]);
                model.MeetingDate = reader["MeetingDate"] as DateTime?;
                model.MeetingVenueID = reader["MeetingVenueID"] as int?;
                model.MeetingTypeID = reader["MeetingTypeID"] as int?;
                model.DepartmentID = reader["DepartmentID"] as int?;
                model.MeetingDescription = reader["MeetingDescription"].ToString();
                model.DocumentPath = reader["DocumentPath"].ToString();
                model.IsCancelled = reader["IsCancelled"] == DBNull.Value ? false : Convert.ToBoolean(reader["IsCancelled"]);
                model.CancellationDateTime = reader["CancellationDateTime"] as DateTime?;
                model.CancellationReason = reader["CancellationReason"].ToString();
                model.DepartmentName = GetSelectedText(FillDepartmentDropDown(), model.DepartmentID);
                model.MeetingTypeName = GetSelectedText(FillMeetingTypeDropDown(), model.MeetingTypeID);
                model.MeetingVenueName = GetSelectedText(FillMeetingVenueDropdown(), model.MeetingVenueID);
            }

            reader.Close();
            cmd.Parameters.Clear();
            cmd.CommandText = "PR_MeetingMember_SelectAll";
            cmd.CommandType = CommandType.StoredProcedure;
            reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                if (Convert.ToInt32(reader["MeetingID"]) == id)
                {
                    MeetingMemberModel member = new MeetingMemberModel();
                    member.MeetingMemberID = Convert.ToInt32(reader["MeetingMemberID"]);
                    member.MeetingID = Convert.ToInt32(reader["MeetingID"]);
                    member.StaffID = Convert.ToInt32(reader["StaffID"]);
                    member.MeetingDescription = reader["MeetingDescription"].ToString();
                    member.StaffName = reader["StaffName"].ToString();
                    member.IsPresent = Convert.ToBoolean(reader["IsPresent"]);
                    member.Remarks = reader["Remarks"].ToString();
                    viewModel.Members.Add(member);
                }
            }

            reader.Close();
            con.Close();

            viewModel.Meeting = model;
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddMeetingMember(MeetingDetailsViewModel viewModel)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("MeetingsList");
            }

            if (viewModel.NewMember.MeetingID == 0)
            {
                TempData["ErrorMessage"] = "Meeting not found.";
                return RedirectToAction("MeetingsList");
            }

            if (viewModel.NewMember.StaffID == 0)
            {
                TempData["ErrorMessage"] = "Please select staff.";
                return RedirectToAction("MeetingsDetails", new { id = viewModel.NewMember.MeetingID });
            }

            SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            con.Open();

            SqlCommand checkCmd = new SqlCommand();
            checkCmd.Connection = con;
            checkCmd.CommandText = "SELECT COUNT(*) FROM MOM_MeetingMember WHERE MeetingID = @MeetingID AND StaffID = @StaffID";
            checkCmd.CommandType = CommandType.Text;
            checkCmd.Parameters.AddWithValue("@MeetingID", viewModel.NewMember.MeetingID);
            checkCmd.Parameters.AddWithValue("@StaffID", viewModel.NewMember.StaffID);

            int count = Convert.ToInt32(checkCmd.ExecuteScalar());

            if (count > 0)
            {
                con.Close();
                TempData["ErrorMessage"] = "Same staff already added in this meeting.";
                return RedirectToAction("MeetingsDetails", new { id = viewModel.NewMember.MeetingID });
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_MeetingMember_Insert";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@MeetingID", viewModel.NewMember.MeetingID);
            cmd.Parameters.AddWithValue("@StaffID", viewModel.NewMember.StaffID);
            cmd.Parameters.AddWithValue("@IsPresent", false);
            cmd.Parameters.AddWithValue("@Remarks", string.Empty);
            cmd.Parameters.AddWithValue("@Modified", DateTime.Now);
            cmd.ExecuteNonQuery();
            con.Close();
            AuditLogService.Log(
                GetCurrentCompanyName(),
                HttpContext.Session.GetInt32("UserID"),
                HttpContext.Session.GetString("UserName"),
                HttpContext.Session.GetString("UserRole"),
                "AddMember",
                "Meeting",
                viewModel.NewMember.MeetingID.ToString(),
                "Meeting member added",
                $"Staff #{viewModel.NewMember.StaffID} was added to meeting #{viewModel.NewMember.MeetingID}.");

            TempData["SuccessMessage"] = "Meeting member added successfully.";
            return RedirectToAction("MeetingsDetails", new { id = viewModel.NewMember.MeetingID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateMeetingMemberAttendance(int MeetingMemberID, int MeetingID, bool IsPresent, string Remarks)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("MeetingsList");
            }

            SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_MeetingMember_UpdateByPK";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@MeetingMemberID", MeetingMemberID);
            cmd.Parameters.AddWithValue("@IsPresent", IsPresent);
            cmd.Parameters.AddWithValue("@Remarks", Remarks ?? string.Empty);

            con.Open();
            cmd.ExecuteNonQuery();
            con.Close();
            AuditLogService.Log(
                GetCurrentCompanyName(),
                HttpContext.Session.GetInt32("UserID"),
                HttpContext.Session.GetString("UserName"),
                HttpContext.Session.GetString("UserRole"),
                "AttendanceUpdate",
                "Meeting",
                MeetingID.ToString(),
                "Meeting attendance updated",
                $"Attendance for meeting member #{MeetingMemberID} was updated to {(IsPresent ? "Present" : "Absent")}.");

            TempData["SuccessMessage"] = "Attendance updated successfully.";
            return RedirectToAction("MeetingsDetails", new { id = MeetingID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteMeetingMember(int MeetingMemberID, int MeetingID)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("MeetingsList");
            }

            try
            {
                SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandText = "PR_MeetingMember_DeleteByPK";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@MeetingMemberID", MeetingMemberID);

                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();
                AuditLogService.Log(
                    GetCurrentCompanyName(),
                    HttpContext.Session.GetInt32("UserID"),
                    HttpContext.Session.GetString("UserName"),
                    HttpContext.Session.GetString("UserRole"),
                    "ExcludeMember",
                    "Meeting",
                    MeetingID.ToString(),
                    "Meeting member excluded",
                    $"Meeting member #{MeetingMemberID} was excluded from meeting #{MeetingID}.");

                TempData["SuccessMessage"] = "Meeting member deleted successfully.";
            }
            catch
            {
                TempData["DeleteError"] = "Delete failed.";
            }

            return RedirectToAction("MeetingsDetails", new { id = MeetingID });
        }

        public IActionResult ExportToExcel()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("MeetingsList");
            }

            try
            {
                DataTable dt = new DataTable();

                SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "PR_Meetings_SelectAll";
                cmd.Parameters.AddWithValue("@searchtext", DBNull.Value);

                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                dt.Load(dr);
                dr.Close();
                con.Close();

                using (XLWorkbook workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Meetings");

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

                        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "MeetingsList.xlsx");
                    }
                }
            }
            catch
            {
                TempData["ErrorMessage"] = "Error exporting data. Please try again.";
                return RedirectToAction("MeetingsList");
            }
        }

        public IActionResult DownloadImportTemplate()
        {
            byte[] content = ExcelImportService.BuildTemplate(
                "MeetingsImport",
                new[] { "MeetingDate", "MeetingTypeName", "DepartmentName", "MeetingVenueName", "MeetingDescription", "IncludeAllDepartmentsMembers" },
                new List<IReadOnlyList<string>>
                {
                    new[] { "2026-04-05 10:30", "Daily Standup", "Human Resources", "Conference Room A", "Weekly review sync", "No" }
                });

            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "MeetingsImportTemplate.xlsx");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ImportFromExcel(IFormFile? excelFile)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("MeetingsList");
            }

            if (!ExcelImportService.IsExcelFile(excelFile))
            {
                TempData["ErrorMessage"] = $"Please upload a valid Excel file up to {ExcelImportService.MaxFileSizeBytes / (1024 * 1024)} MB.";
                return RedirectToAction("MeetingsList");
            }

            if (!ExcelImportService.HasRequiredHeaders(excelFile!, new[] { "MeetingDate", "MeetingTypeName", "DepartmentName", "MeetingVenueName", "MeetingDescription", "IncludeAllDepartmentsMembers" }, out string headerMessage))
            {
                TempData["ErrorMessage"] = headerMessage;
                return RedirectToAction("MeetingsList");
            }

            List<Dictionary<string, string>> rows = ExcelImportService.ReadRows(excelFile!);
            if (rows.Count == 0)
            {
                TempData["ErrorMessage"] = "Excel file is empty.";
                return RedirectToAction("MeetingsList");
            }

            int importedCount = 0;
            int skippedCount = 0;
            List<ImportReportRowModel> reportRows = new List<ImportReportRowModel>();

            using SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            con.Open();

            for (int index = 0; index < rows.Count; index++)
            {
                Dictionary<string, string> row = rows[index];
                string meetingDateValue = ExcelImportService.GetValue(row, "MeetingDate");
                string meetingTypeName = ExcelImportService.GetValue(row, "MeetingTypeName");
                string departmentName = ExcelImportService.GetValue(row, "DepartmentName");
                string meetingVenueName = ExcelImportService.GetValue(row, "MeetingVenueName");
                string meetingDescription = ExcelImportService.GetValue(row, "MeetingDescription");
                bool includeAllDepartments = ExcelImportService.ParseBoolean(ExcelImportService.GetValue(row, "IncludeAllDepartmentsMembers"));
                string summary = $"{meetingDateValue} | {meetingTypeName} | {meetingDescription}";

                if (!DateTime.TryParse(meetingDateValue, out DateTime meetingDate) ||
                    string.IsNullOrWhiteSpace(meetingTypeName) ||
                    string.IsNullOrWhiteSpace(meetingVenueName) ||
                    (!includeAllDepartments && string.IsNullOrWhiteSpace(departmentName)))
                {
                    skippedCount++;
                    reportRows.Add(new ImportReportRowModel { RowNumber = index + 2, Status = "Skipped", Message = "MeetingDate, MeetingTypeName, MeetingVenueName and DepartmentName are required for normal meeting import.", DataSummary = summary });
                    continue;
                }

                int? meetingTypeId = GetLookupIdByName(con, "MOM_MeetingType", "MeetingTypeID", "MeetingTypeName", meetingTypeName, GetCurrentCompanyName());
                int? meetingVenueId = GetLookupIdByName(con, "MOM_MeetingVenue", "MeetingVenueID", "MeetingVenueName", meetingVenueName, GetCurrentCompanyName());
                int? departmentId = includeAllDepartments
                    ? GetFirstDepartmentIdForCompany()
                    : GetLookupIdByName(con, "MOM_Department", "DepartmentID", "DepartmentName", departmentName, GetCurrentCompanyName());

                if (!meetingTypeId.HasValue || !meetingVenueId.HasValue || !departmentId.HasValue)
                {
                    skippedCount++;
                    reportRows.Add(new ImportReportRowModel { RowNumber = index + 2, Status = "Skipped", Message = "Meeting type, venue, or department could not be resolved in current company.", DataSummary = summary });
                    continue;
                }

                using SqlCommand checkCmd = new SqlCommand(@"SELECT COUNT(*) FROM MOM_Meetings
                                                            WHERE MeetingDate = @MeetingDate
                                                              AND MeetingVenueID = @MeetingVenueID
                                                              AND MeetingTypeID = @MeetingTypeID
                                                              AND DepartmentID = @DepartmentID", con);
                checkCmd.Parameters.AddWithValue("@MeetingDate", meetingDate);
                checkCmd.Parameters.AddWithValue("@MeetingVenueID", meetingVenueId.Value);
                checkCmd.Parameters.AddWithValue("@MeetingTypeID", meetingTypeId.Value);
                checkCmd.Parameters.AddWithValue("@DepartmentID", departmentId.Value);

                if (Convert.ToInt32(checkCmd.ExecuteScalar()) > 0)
                {
                    skippedCount++;
                    reportRows.Add(new ImportReportRowModel { RowNumber = index + 2, Status = "Skipped", Message = "Meeting already exists with the same date, type, venue and department.", DataSummary = summary });
                    continue;
                }

                using SqlCommand cmd = new SqlCommand("PR_Meetings_Insert", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@MeetingDate", meetingDate);
                cmd.Parameters.AddWithValue("@MeetingVenueID", meetingVenueId.Value);
                cmd.Parameters.AddWithValue("@MeetingTypeID", meetingTypeId.Value);
                cmd.Parameters.AddWithValue("@DepartmentID", departmentId.Value);
                cmd.Parameters.AddWithValue("@MeetingDescription", meetingDescription);
                cmd.Parameters.AddWithValue("@DocumentPath", string.Empty);
                cmd.Parameters.AddWithValue("@Modified", DateTime.Now);
                int meetingId = Convert.ToInt32(cmd.ExecuteScalar());

                AddDepartmentMembersToMeeting(con, meetingId, departmentId, includeAllDepartments);
                importedCount++;
                reportRows.Add(new ImportReportRowModel { RowNumber = index + 2, Status = "Imported", Message = includeAllDepartments ? "Meeting created and all department members were added." : "Meeting created and selected department members were added.", DataSummary = summary });
            }

            AuditLogService.Log(
                GetCurrentCompanyName(),
                HttpContext.Session.GetInt32("UserID"),
                HttpContext.Session.GetString("UserName"),
                HttpContext.Session.GetString("UserRole"),
                "Import",
                "Meeting",
                null,
                "Meetings imported from Excel",
                $"{importedCount} meetings imported and {skippedCount} skipped.");

            TempData["ImportReportPath"] = ImportReportService.SaveReport("MeetingsImport", GetCurrentCompanyName(), HttpContext.Session.GetInt32("UserID"), HttpContext.Session.GetString("UserName"), importedCount, skippedCount, reportRows);
            TempData["SuccessMessage"] = $"Meeting import completed. Imported: {importedCount}, Skipped: {skippedCount}. Department members were added automatically.";
            return RedirectToAction("MeetingsList");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]

        public IActionResult Save(MeetingsModel model)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("MeetingsList");
            }

            if (model.IncludeAllDepartmentsMembers)
            {
                ModelState.Remove("DepartmentID");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.DepartmentDropDown = FillDepartmentDropDown();
                ViewBag.MeetingTypeDropDown = FillMeetingTypeDropDown();
                ViewBag.MeetingVenueDropDown = FillMeetingVenueDropdown();
                return View("MeetingsAddEdit", model);
            }

            bool includeAllDepartments = model.IncludeAllDepartmentsMembers || (model.DepartmentID.HasValue && model.DepartmentID.Value == -1);
            if (includeAllDepartments)
            {
                int? defaultDepartmentId = GetFirstDepartmentIdForCompany();
                if (!defaultDepartmentId.HasValue)
                {
                    ModelState.AddModelError("DepartmentID", "Please add at least one department first.");
                    ViewBag.DepartmentDropDown = FillDepartmentDropDown();
                    ViewBag.MeetingTypeDropDown = FillMeetingTypeDropDown();
                    ViewBag.MeetingVenueDropDown = FillMeetingVenueDropdown();
                    return View("MeetingsAddEdit", model);
                }

                model.DepartmentID = defaultDepartmentId.Value;
                model.IncludeAllDepartmentsMembers = true;
            }

            string filePath = string.Empty;
            bool isNewMeeting = model.MeetingID == 0;

            if (!isNewMeeting)
            {
                filePath = FileSecurityService.SanitizeStoredDocumentPath(GetExistingDocumentPath(model.MeetingID));
            }

            if (model.DocumentFile != null)
            {
                if (!FileSecurityService.TrySaveDocument(_env, model.DocumentFile, "uploads", "meeting", out filePath, out string uploadError))
                {
                    ModelState.AddModelError("DocumentFile", uploadError);
                    ViewBag.DepartmentDropDown = FillDepartmentDropDown();
                    ViewBag.MeetingTypeDropDown = FillMeetingTypeDropDown();
                    ViewBag.MeetingVenueDropDown = FillMeetingVenueDropdown();
                    return View("MeetingsAddEdit", model);
                }
            }

            SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            con.Open();
            int meetingId = model.MeetingID;

            if (model.MeetingID == 0)
            {
                SqlCommand checkCmd = new SqlCommand();
                checkCmd.Connection = con;
                checkCmd.CommandText = "SELECT COUNT(*) FROM MOM_Meetings WHERE MeetingDate=@MeetingDate AND MeetingVenueID=@MeetingVenueID AND MeetingTypeID=@MeetingTypeID AND DepartmentID=@DepartmentID";
                checkCmd.CommandType = CommandType.Text;
                checkCmd.Parameters.AddWithValue("@MeetingDate", model.MeetingDate);
                checkCmd.Parameters.AddWithValue("@MeetingVenueID", model.MeetingVenueID);
                checkCmd.Parameters.AddWithValue("@MeetingTypeID", model.MeetingTypeID);
                checkCmd.Parameters.AddWithValue("@DepartmentID", model.DepartmentID);

                int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                if (count > 0)
                {
                    con.Close();
                    ModelState.AddModelError("MeetingDate", "Same meeting already exists.");
                    ViewBag.DepartmentDropDown = FillDepartmentDropDown();
                    ViewBag.MeetingTypeDropDown = FillMeetingTypeDropDown();
                    ViewBag.MeetingVenueDropDown = FillMeetingVenueDropdown();
                    return View("MeetingsAddEdit", model);
                }
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandType = CommandType.StoredProcedure;

            if (model.MeetingID == 0)
            {
                cmd.CommandText = "PR_Meetings_Insert";
                cmd.Parameters.AddWithValue("@MeetingDate", model.MeetingDate);
                cmd.Parameters.AddWithValue("@MeetingVenueID", model.MeetingVenueID);
                cmd.Parameters.AddWithValue("@MeetingTypeID", model.MeetingTypeID);
                cmd.Parameters.AddWithValue("@DepartmentID", model.DepartmentID);
                cmd.Parameters.AddWithValue("@MeetingDescription", model.MeetingDescription ?? string.Empty);
                cmd.Parameters.AddWithValue("@DocumentPath", filePath);
                cmd.Parameters.AddWithValue("@Modified", DateTime.Now);
                meetingId = Convert.ToInt32(cmd.ExecuteScalar());
            }
            else
            {
                cmd.CommandText = "PR_Meetings_UpdateByPK";
                cmd.Parameters.AddWithValue("@MeetingID", model.MeetingID);
                cmd.Parameters.AddWithValue("@MeetingDate", model.MeetingDate);
                cmd.Parameters.AddWithValue("@MeetingVenueID", model.MeetingVenueID);
                cmd.Parameters.AddWithValue("@MeetingTypeID", model.MeetingTypeID);
                cmd.Parameters.AddWithValue("@DepartmentID", model.DepartmentID);
                cmd.Parameters.AddWithValue("@MeetingDescription", model.MeetingDescription ?? string.Empty);
                cmd.Parameters.AddWithValue("@DocumentPath", filePath);
                cmd.ExecuteNonQuery();
            }

            AddDepartmentMembersToMeeting(con, meetingId, model.DepartmentID, model.IncludeAllDepartmentsMembers);
            con.Close();
            AuditLogService.Log(
                GetCurrentCompanyName(),
                HttpContext.Session.GetInt32("UserID"),
                HttpContext.Session.GetString("UserName"),
                HttpContext.Session.GetString("UserRole"),
                isNewMeeting ? "Create" : "Update",
                "Meeting",
                meetingId.ToString(),
                isNewMeeting ? "Meeting scheduled" : "Meeting updated",
                $"{model.MeetingDescription} was {(isNewMeeting ? "scheduled" : "updated")} for {(model.IncludeAllDepartmentsMembers ? "all company departments" : "the selected department")}.");

            TempData["SuccessMessage"] = isNewMeeting
                ? "Meeting added successfully. Department members added automatically."
                : "Meeting updated successfully. Department members synced automatically.";

            return RedirectToAction("MeetingsList");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int MeetingID)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("MeetingsList");
            }

            try
            {
                SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
                con.Open();

                SqlCommand checkCmd = new SqlCommand();
                checkCmd.Connection = con;
                checkCmd.CommandText = "PR_Meetings_SelectByPK";
                checkCmd.CommandType = CommandType.StoredProcedure;
                checkCmd.Parameters.AddWithValue("@MeetingID", MeetingID);

                SqlDataReader reader = checkCmd.ExecuteReader();

                if (reader.Read())
                {
                    bool isCancelled = reader["IsCancelled"] != DBNull.Value && Convert.ToBoolean(reader["IsCancelled"]);
                    DateTime? meetingDate = reader["MeetingDate"] as DateTime?;
                    reader.Close();

                    if (isCancelled)
                    {
                        con.Close();
                        TempData["DeleteError"] = "Meeting already cancelled.";
                        return RedirectToAction("MeetingsList");
                    }

                    if (meetingDate.HasValue && meetingDate.Value <= DateTime.Now)
                    {
                        con.Close();
                        TempData["DeleteError"] = "Completed meeting cannot be cancelled.";
                        return RedirectToAction("MeetingsList");
                    }
                }
                else
                {
                    reader.Close();
                    con.Close();
                    TempData["DeleteError"] = "Meeting not found.";
                    return RedirectToAction("MeetingsList");
                }

                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandText = "PR_Meetings_DeleteByPK";
                cmd.Parameters.AddWithValue("@MeetingID", MeetingID);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.ExecuteNonQuery();
                con.Close();
                AuditLogService.Log(
                    GetCurrentCompanyName(),
                    HttpContext.Session.GetInt32("UserID"),
                    HttpContext.Session.GetString("UserName"),
                    HttpContext.Session.GetString("UserRole"),
                    "Cancel",
                    "Meeting",
                    MeetingID.ToString(),
                    "Meeting cancelled",
                    $"Meeting #{MeetingID} was cancelled.");
                TempData["SuccessMessage"] = "Meeting cancelled successfully.";
            }
            catch
            {
                TempData["DeleteError"] = "Cancel failed.";
            }

            return RedirectToAction("MeetingsList");
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
                    list.Add(new SelectListItem { Text = sdr["DepartmentName"].ToString(), Value = value });
                }
            }
            sdr.Close();
            con.Close();

            list.Insert(0, new SelectListItem
            {
                Text = "Include All Departments",
                Value = "-1"
            });

            return list;
        }

        public List<SelectListItem> FillMeetingTypeDropDown()
        {
            List<SelectListItem> list = new List<SelectListItem>();
            SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_MOM_MEETINGTYPE_DDL";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@CompanyName", GetCurrentCompanyName());
            con.Open();
            SqlDataReader sdr = cmd.ExecuteReader();
            while (sdr.Read())
            {
                string value = Convert.ToString(sdr["MeetingTypeID"]) ?? string.Empty;
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
                    list.Add(new SelectListItem { Text = sdr["MeetingTypeName"].ToString(), Value = value });
                }
            }
            sdr.Close();
            con.Close();
            return list;
        }

        public List<SelectListItem> FillMeetingVenueDropdown()
        {
            List<SelectListItem> list = new List<SelectListItem>();
            SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_MOM_MEETINGVENUE_DDL";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@CompanyName", GetCurrentCompanyName());
            con.Open();
            SqlDataReader sdr = cmd.ExecuteReader();
            while (sdr.Read())
            {
                string value = Convert.ToString(sdr["MeetingVenueID"]) ?? string.Empty;
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
                    list.Add(new SelectListItem { Text = sdr["MeetingVenueName"].ToString(), Value = value });
                }
            }
            sdr.Close();
            con.Close();
            return list;
        }

        public List<SelectListItem> FillStaffDropDown(int? departmentId = null)
        {
            List<SelectListItem> list = new List<SelectListItem>();
            SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_Staff_SelectAll";
            cmd.CommandType = CommandType.StoredProcedure;
            con.Open();
            SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                if (departmentId.HasValue)
                {
                    int staffDepartmentId = reader["DepartmentID"] == DBNull.Value ? 0 : Convert.ToInt32(reader["DepartmentID"]);
                    if (staffDepartmentId != departmentId.Value)
                    {
                        continue;
                    }
                }

                string value = Convert.ToString(reader["StaffID"]) ?? string.Empty;
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
                        Value = value,
                        Text = reader["StaffName"].ToString()
                    });
                }
            }
            reader.Close();
            con.Close();
            return list;
        }

        public int? GetLookupIdByName(SqlConnection con, string tableName, string idColumn, string nameColumn, string lookupValue, string companyName)
        {
            if (!IsSafeLookup(tableName, idColumn, nameColumn))
            {
                throw new InvalidOperationException("Unsupported lookup requested.");
            }

            string sql = $"SELECT TOP 1 {idColumn} FROM {tableName} WHERE {nameColumn} = @LookupValue AND CompanyName = @CompanyName";
            using SqlCommand cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@LookupValue", lookupValue);
            cmd.Parameters.AddWithValue("@CompanyName", companyName);
            object? result = cmd.ExecuteScalar();
            return result == null || result == DBNull.Value ? null : Convert.ToInt32(result);
        }

        public string GetSelectedText(List<SelectListItem> list, int? id)
        {
            if (!id.HasValue)
            {
                return string.Empty;
            }

            foreach (SelectListItem item in list)
            {
                if (item.Value == id.Value.ToString())
                {
                    return item.Text;
                }
            }

            return string.Empty;
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
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.AddWithValue("@MeetingID", meetingId);

            cmd.CommandText = @"SELECT COUNT(*)
                                FROM MOM_Meetings m
                                INNER JOIN MOM_Department d ON m.DepartmentID = d.DepartmentID
                                WHERE m.MeetingID = @MeetingID
                                  AND d.CompanyName = @CompanyName";
            cmd.Parameters.AddWithValue("@CompanyName", GetCurrentCompanyName());

            con.Open();
            int count = Convert.ToInt32(cmd.ExecuteScalar());
            con.Close();

            return count > 0;
        }

        public int GetMeetingStatusRank(MeetingsModel meeting)
        {
            if (meeting.IsCancelled)
            {
                return 2;
            }

            if (meeting.MeetingDate.HasValue && meeting.MeetingDate.Value > DateTime.Now)
            {
                return 0;
            }

            return 1;
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

        public int? GetFirstDepartmentIdForCompany()
        {
            int? departmentId = null;
            SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "SELECT TOP 1 DepartmentID FROM MOM_Department WHERE CompanyName = @CompanyName ORDER BY DepartmentName";
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.AddWithValue("@CompanyName", GetCurrentCompanyName());
            con.Open();
            object result = cmd.ExecuteScalar();
            con.Close();

            if (result != null && result != DBNull.Value)
            {
                departmentId = Convert.ToInt32(result);
            }

            return departmentId;
        }

        public void AddDepartmentMembersToMeeting(SqlConnection con, int meetingId, int? departmentId, bool includeAllDepartmentsMembers)
        {
            List<int> staffIds = new List<int>();

            if (includeAllDepartmentsMembers || (departmentId.HasValue && departmentId.Value == -1))
            {
                staffIds = GetCompanyStaffIds(con);
            }
            else if (departmentId.HasValue && departmentId.Value > 0)
            {
                staffIds = GetDepartmentStaffIds(con, departmentId.Value);
            }

            foreach (int staffId in staffIds)
            {
                SqlCommand checkCmd = new SqlCommand();
                checkCmd.Connection = con;
                checkCmd.CommandText = "SELECT COUNT(*) FROM MOM_MeetingMember WHERE MeetingID = @MeetingID AND StaffID = @StaffID";
                checkCmd.CommandType = CommandType.Text;
                checkCmd.Parameters.AddWithValue("@MeetingID", meetingId);
                checkCmd.Parameters.AddWithValue("@StaffID", staffId);

                int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                if (count > 0)
                {
                    continue;
                }

                SqlCommand insertCmd = new SqlCommand();
                insertCmd.Connection = con;
                insertCmd.CommandText = "PR_MeetingMember_Insert";
                insertCmd.CommandType = CommandType.StoredProcedure;
                insertCmd.Parameters.AddWithValue("@MeetingID", meetingId);
                insertCmd.Parameters.AddWithValue("@StaffID", staffId);
                insertCmd.Parameters.AddWithValue("@IsPresent", false);
                insertCmd.Parameters.AddWithValue("@Remarks", string.Empty);
                insertCmd.Parameters.AddWithValue("@Modified", DateTime.Now);
                insertCmd.ExecuteNonQuery();
            }
        }

        public List<int> GetDepartmentStaffIds(SqlConnection con, int departmentId)
        {
            List<int> staffIds = new List<int>();
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "SELECT StaffID FROM MOM_Staff WHERE DepartmentID = @DepartmentID ORDER BY StaffName";
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.AddWithValue("@DepartmentID", departmentId);

            SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                staffIds.Add(Convert.ToInt32(reader["StaffID"]));
            }
            reader.Close();

            return staffIds;
        }

        public List<int> GetCompanyStaffIds(SqlConnection con)
        {
            List<int> staffIds = new List<int>();
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = @"SELECT s.StaffID
                                FROM MOM_Staff s
                                INNER JOIN MOM_Department d ON s.DepartmentID = d.DepartmentID
                                WHERE d.CompanyName = @CompanyName
                                ORDER BY s.StaffName";
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.AddWithValue("@CompanyName", GetCurrentCompanyName());

            SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                staffIds.Add(Convert.ToInt32(reader["StaffID"]));
            }
            reader.Close();

            return staffIds;
        }

        private bool IsSafeLookup(string tableName, string idColumn, string nameColumn)
        {
            return (tableName, idColumn, nameColumn) switch
            {
                ("MOM_MeetingType", "MeetingTypeID", "MeetingTypeName") => true,
                ("MOM_MeetingVenue", "MeetingVenueID", "MeetingVenueName") => true,
                ("MOM_Department", "DepartmentID", "DepartmentName") => true,
                _ => false
            };
        }

        private string GetExistingDocumentPath(int meetingId)
        {
            using SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            using SqlCommand cmd = new SqlCommand("SELECT TOP 1 DocumentPath FROM MOM_Meetings WHERE MeetingID = @MeetingID", con);
            cmd.Parameters.AddWithValue("@MeetingID", meetingId);
            con.Open();
            return Convert.ToString(cmd.ExecuteScalar()) ?? string.Empty;
        }
        #endregion
    }
}












