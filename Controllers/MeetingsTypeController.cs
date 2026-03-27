using Meeting_Of_Minutes.Models;
using Meeting_Of_Minutes.Services;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using Microsoft.Data.SqlClient;
using ClosedXML.Excel;

namespace Meeting_Of_Minutes.Controllers
{
    public class MeetingsTypeController : Controller
    {
        #region Actions
        [HttpGet]
        public IActionResult MeetingsTypeList(string? searchtext, int page = 1, string sortBy = "name", string sortDirection = "asc")
        {
            searchtext = string.IsNullOrWhiteSpace(searchtext) ? null : searchtext;
            ViewBag.searchtext = searchtext;
            return View(BuildMeetingTypePage(searchtext, page, sortBy, sortDirection));
        }

        [HttpPost]
        public IActionResult MeetingsTypeList(IFormCollection formdata)
        {
            string? searchtext = formdata["searchtext"].ToString();
            return RedirectToAction(nameof(MeetingsTypeList), new { searchtext });
        }

        public List<MeetingTypeModel> GetAllMeetingTypes(string? searchtext)
        {
            List<MeetingTypeModel> meetingTypesList = new List<MeetingTypeModel>();
            string companyName = HttpContext.Session.GetString("CompanyName") ?? string.Empty;

            SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_MeetingType_SelectAll";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@CompanyName", companyName);

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
                MeetingTypeModel meeting = new MeetingTypeModel();
                meeting.MeetingTypeID = Convert.ToInt32(reader["MeetingTypeID"]);
                meeting.MeetingTypeName = reader["MeetingTypeName"].ToString();
                meeting.Remarks = reader["Remarks"].ToString();
                meeting.Created = reader["Created"] == DBNull.Value ? DateTime.Now : Convert.ToDateTime(reader["Created"]);
                meeting.Modified = reader["Modified"] == DBNull.Value ? DateTime.Now : Convert.ToDateTime(reader["Modified"]);
                meetingTypesList.Add(meeting);
            }

            reader.Close();
            con.Close();

            return meetingTypesList;
        }

        public PagedListViewModel<MeetingTypeModel> BuildMeetingTypePage(string? searchtext, int page, string? sortBy, string? sortDirection)
        {
            IEnumerable<MeetingTypeModel> query = GetAllMeetingTypes(searchtext);
            bool isDesc = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

            query = (sortBy ?? "name").ToLowerInvariant() switch
            {
                "created" => isDesc ? query.OrderByDescending(x => x.Created).ThenBy(x => x.MeetingTypeName) : query.OrderBy(x => x.Created).ThenBy(x => x.MeetingTypeName),
                _ => isDesc ? query.OrderByDescending(x => x.MeetingTypeName) : query.OrderBy(x => x.MeetingTypeName)
            };

            List<MeetingTypeModel> ordered = query.ToList();
            const int pageSize = 10;
            page = Math.Max(page, 1);

            return new PagedListViewModel<MeetingTypeModel>
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


        public IActionResult MeetingsTypeAddEdit(int? id)
        {
            MeetingTypeModel model = new MeetingTypeModel();

            if (id.HasValue)
            {
                SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandText = "PR_MeetingType_SelectByPK";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@MeetingTypeID", id.Value);
                cmd.Parameters.AddWithValue("@CompanyName", HttpContext.Session.GetString("CompanyName") ?? string.Empty);

                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    model.MeetingTypeID = Convert.ToInt32(reader["MeetingTypeID"]);
                    model.MeetingTypeName = reader["MeetingTypeName"].ToString();
                    model.Remarks = reader["Remarks"].ToString();
                    model.Created = reader["Created"] == DBNull.Value ? DateTime.Now : Convert.ToDateTime(reader["Created"]);
                    model.Modified = reader["Modified"] == DBNull.Value ? DateTime.Now : Convert.ToDateTime(reader["Modified"]);
                }

                reader.Close();
                con.Close();
            }

            return View(model);
        }

        public IActionResult ExportToExcel()
        {
            try
            {
                DataTable dt = new DataTable();

                SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "PR_MeetingType_SelectAll";
                cmd.Parameters.AddWithValue("@CompanyName", HttpContext.Session.GetString("CompanyName") ?? string.Empty);
                cmd.Parameters.AddWithValue("@searchtext", DBNull.Value);

                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                dt.Load(dr);
                dr.Close();
                con.Close();

                using (XLWorkbook workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("MeetingTypes");

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

                        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "MeetingTypeList.xlsx");
                    }
                }
            }
            catch
            {
                TempData["ErrorMessage"] = "Error exporting data. Please try again.";
                return RedirectToAction("MeetingsTypeList");
            }
        }

        public IActionResult DownloadImportTemplate()
        {
            byte[] content = ExcelImportService.BuildTemplate(
                "MeetingTypesImport",
                new[] { "MeetingTypeName", "Remarks" },
                new List<IReadOnlyList<string>>
                {
                    new[] { "Daily Standup", "Short team sync" }
                });

            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "MeetingTypeImportTemplate.xlsx");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ImportFromExcel(IFormFile? excelFile)
        {
            if (!ExcelImportService.IsExcelFile(excelFile))
            {
                TempData["ErrorMessage"] = $"Please upload a valid Excel file up to {ExcelImportService.MaxFileSizeBytes / (1024 * 1024)} MB.";
                return RedirectToAction("MeetingsTypeList");
            }

            if (!ExcelImportService.HasRequiredHeaders(excelFile!, new[] { "MeetingTypeName", "Remarks" }, out string headerMessage))
            {
                TempData["ErrorMessage"] = headerMessage;
                return RedirectToAction("MeetingsTypeList");
            }

            List<Dictionary<string, string>> rows = ExcelImportService.ReadRows(excelFile!);
            if (rows.Count == 0)
            {
                TempData["ErrorMessage"] = "Excel file is empty.";
                return RedirectToAction("MeetingsTypeList");
            }

            int importedCount = 0;
            int skippedCount = 0;
            string companyName = HttpContext.Session.GetString("CompanyName") ?? string.Empty;
            List<ImportReportRowModel> reportRows = new List<ImportReportRowModel>();

            using SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            con.Open();

            for (int index = 0; index < rows.Count; index++)
            {
                Dictionary<string, string> row = rows[index];
                string meetingTypeName = ExcelImportService.GetValue(row, "MeetingTypeName");
                string remarks = ExcelImportService.GetValue(row, "Remarks");
                string summary = meetingTypeName;

                if (string.IsNullOrWhiteSpace(meetingTypeName))
                {
                    skippedCount++;
                    reportRows.Add(new ImportReportRowModel { RowNumber = index + 2, Status = "Skipped", Message = "MeetingTypeName is required.", DataSummary = summary });
                    continue;
                }

                using SqlCommand checkCmd = new SqlCommand("SELECT COUNT(*) FROM MOM_MeetingType WHERE MeetingTypeName = @MeetingTypeName AND CompanyName = @CompanyName", con);
                checkCmd.Parameters.AddWithValue("@MeetingTypeName", meetingTypeName);
                checkCmd.Parameters.AddWithValue("@CompanyName", companyName);

                if (Convert.ToInt32(checkCmd.ExecuteScalar()) > 0)
                {
                    skippedCount++;
                    reportRows.Add(new ImportReportRowModel { RowNumber = index + 2, Status = "Skipped", Message = "Meeting type already exists in current company.", DataSummary = summary });
                    continue;
                }

                using SqlCommand cmd = new SqlCommand("PR_MeetingType_Insert", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@MeetingTypeName", meetingTypeName);
                cmd.Parameters.AddWithValue("@Remarks", remarks);
                cmd.Parameters.AddWithValue("@CompanyName", companyName);
                cmd.Parameters.AddWithValue("@Modified", DateTime.Now);
                cmd.ExecuteNonQuery();
                importedCount++;
                reportRows.Add(new ImportReportRowModel { RowNumber = index + 2, Status = "Imported", Message = "Meeting type created successfully.", DataSummary = summary });
            }

            AuditLogService.Log(
                companyName,
                HttpContext.Session.GetInt32("UserID"),
                HttpContext.Session.GetString("UserName"),
                HttpContext.Session.GetString("UserRole"),
                "Import",
                "MeetingType",
                null,
                "Meeting types imported from Excel",
                $"{importedCount} meeting types imported and {skippedCount} skipped.");

            TempData["ImportReportPath"] = ImportReportService.SaveReport("MeetingTypeImport", companyName, HttpContext.Session.GetInt32("UserID"), HttpContext.Session.GetString("UserName"), importedCount, skippedCount, reportRows);
            TempData["SuccessMessage"] = $"Meeting type import completed. Imported: {importedCount}, Skipped: {skippedCount}.";
            return RedirectToAction("MeetingsTypeList");
        }


        public IActionResult MeetingsTypeDetails(int id)
        {
            MeetingTypeModel model = new MeetingTypeModel();

            SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_MeetingType_SelectByPK";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@MeetingTypeID", id);
            cmd.Parameters.AddWithValue("@CompanyName", HttpContext.Session.GetString("CompanyName") ?? string.Empty);

            con.Open();
            SqlDataReader reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                model.MeetingTypeID = Convert.ToInt32(reader["MeetingTypeID"]);
                model.MeetingTypeName = reader["MeetingTypeName"].ToString();
                model.Remarks = reader["Remarks"].ToString();
                model.Created = reader["Created"] == DBNull.Value ? DateTime.Now : Convert.ToDateTime(reader["Created"]);
                model.Modified = reader["Modified"] == DBNull.Value ? DateTime.Now : Convert.ToDateTime(reader["Modified"]);
            }

            reader.Close();
            con.Close();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]

        public IActionResult Save(MeetingTypeModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("MeetingsTypeAddEdit", model);
            }

            SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            con.Open();
            string companyName = HttpContext.Session.GetString("CompanyName") ?? string.Empty;

            if (model.MeetingTypeID == 0)
            {
                SqlCommand checkCmd = new SqlCommand();
                checkCmd.Connection = con;
                checkCmd.CommandText = "SELECT COUNT(*) FROM MOM_MeetingType WHERE MeetingTypeName = @MeetingTypeName AND CompanyName = @CompanyName";
                checkCmd.CommandType = CommandType.Text;
                checkCmd.Parameters.AddWithValue("@MeetingTypeName", model.MeetingTypeName);
                checkCmd.Parameters.AddWithValue("@CompanyName", companyName);

                int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                if (count > 0)
                {
                    con.Close();
                    ModelState.AddModelError("MeetingTypeName", "Meeting Type already exists.");
                    return View("MeetingsTypeAddEdit", model);
                }
            }
            else
            {
                SqlCommand checkCmd = new SqlCommand();
                checkCmd.Connection = con;
                checkCmd.CommandText = "SELECT COUNT(*) FROM MOM_MeetingType WHERE MeetingTypeName = @MeetingTypeName AND MeetingTypeID <> @MeetingTypeID AND CompanyName = @CompanyName";
                checkCmd.CommandType = CommandType.Text;
                checkCmd.Parameters.AddWithValue("@MeetingTypeName", model.MeetingTypeName);
                checkCmd.Parameters.AddWithValue("@MeetingTypeID", model.MeetingTypeID);
                checkCmd.Parameters.AddWithValue("@CompanyName", companyName);

                int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                if (count > 0)
                {
                    con.Close();
                    ModelState.AddModelError("MeetingTypeName", "Meeting Type already exists.");
                    return View("MeetingsTypeAddEdit", model);
                }
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandType = CommandType.StoredProcedure;

            if (model.MeetingTypeID == 0)
            {
                cmd.CommandText = "PR_MeetingType_Insert";
                cmd.Parameters.AddWithValue("@MeetingTypeName", model.MeetingTypeName);
                cmd.Parameters.AddWithValue("@Remarks", model.Remarks ?? string.Empty);
                cmd.Parameters.AddWithValue("@CompanyName", companyName);
                cmd.Parameters.AddWithValue("@Modified", DateTime.Now);
            }
            else
            {
                cmd.CommandText = "PR_MeetingType_UpdateByPK";
                cmd.Parameters.AddWithValue("@MeetingTypeID", model.MeetingTypeID);
                cmd.Parameters.AddWithValue("@MeetingTypeName", model.MeetingTypeName);
                cmd.Parameters.AddWithValue("@Remarks", model.Remarks ?? string.Empty);
                cmd.Parameters.AddWithValue("@CompanyName", companyName);
            }
            TempData["SuccessMessage"] = model.MeetingTypeID == 0 ? "Meeting type added successfully." : "Meeting type updated successfully.";
            cmd.ExecuteNonQuery();
            con.Close();
            AuditLogService.Log(
                HttpContext.Session.GetString("CompanyName"),
                HttpContext.Session.GetInt32("UserID"),
                HttpContext.Session.GetString("UserName"),
                HttpContext.Session.GetString("UserRole"),
                model.MeetingTypeID == 0 ? "Create" : "Update",
                "MeetingType",
                model.MeetingTypeID == 0 ? null : model.MeetingTypeID.ToString(),
                model.MeetingTypeID == 0 ? "Meeting type created" : "Meeting type updated",
                $"{model.MeetingTypeName} meeting type was {(model.MeetingTypeID == 0 ? "created" : "updated")}.");

            return RedirectToAction("MeetingsTypeList");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int MeetingTypeID)
        {
            try
            {
                SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandText = "PR_MeetingType_DeleteByPK";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@MeetingTypeID", MeetingTypeID);

                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();
                AuditLogService.Log(
                    HttpContext.Session.GetString("CompanyName"),
                    HttpContext.Session.GetInt32("UserID"),
                    HttpContext.Session.GetString("UserName"),
                    HttpContext.Session.GetString("UserRole"),
                    "Delete",
                    "MeetingType",
                    MeetingTypeID.ToString(),
                    "Meeting type deleted",
                    $"Meeting type #{MeetingTypeID} was deleted.");
                TempData["SuccessMessage"] = "Meeting type deleted successfully.";
            }
            catch
            {
                TempData["DeleteError"] = "FK violation: linked data exists.";
            }

            return RedirectToAction("MeetingsTypeList");
        }
        #endregion
    }
}













