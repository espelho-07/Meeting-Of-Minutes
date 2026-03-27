using Meeting_Of_Minutes.Models;
using Meeting_Of_Minutes.Services;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using Microsoft.Data.SqlClient;
using ClosedXML.Excel;

namespace Meeting_Of_Minutes.Controllers
{
    public class MeetingVenueController : Controller
    {
        #region Actions
        public IActionResult MeetingVenueAddEdit(int? id)
        {
            MeetingVenueModel model = new MeetingVenueModel();
            string companyName = HttpContext.Session.GetString("CompanyName") ?? string.Empty;

            if (id.HasValue)
            {
                SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandText = "PR_MeetingVenue_SelectByPK";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@MeetingVenueID", id.Value);
                cmd.Parameters.AddWithValue("@CompanyName", companyName);

                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    model.MeetingVenueID = Convert.ToInt32(reader["MeetingVenueID"]);
                    model.MeetingVenueName = reader["MeetingVenueName"].ToString();
                }

                reader.Close();
                con.Close();
            }

            return View(model);
        }


        public IActionResult MeetingVenueList(string? searchtext, int page = 1, string sortBy = "name", string sortDirection = "asc")
        {
            searchtext = string.IsNullOrWhiteSpace(searchtext) ? null : searchtext;
            ViewBag.searchtext = searchtext;
            return View(BuildMeetingVenuePage(searchtext, page, sortBy, sortDirection));
        }

        [HttpPost]
        public IActionResult MeetingVenueList(IFormCollection formdata)
        {
            string? searchtext = formdata["searchtext"].ToString();
            return RedirectToAction(nameof(MeetingVenueList), new { searchtext });
        }

        private List<MeetingVenueModel> GetAllMeetingVenues(string? searchtext)
        {
            List<MeetingVenueModel> list = new List<MeetingVenueModel>();
            string companyName = HttpContext.Session.GetString("CompanyName") ?? string.Empty;

            SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_MeetingVenue_SelectAll";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@CompanyName", companyName);

            con.Open();
            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                string venueName = reader["MeetingVenueName"].ToString() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(searchtext) &&
                    venueName.IndexOf(searchtext, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                MeetingVenueModel mv = new MeetingVenueModel();
                mv.MeetingVenueID = Convert.ToInt32(reader["MeetingVenueID"]);
                mv.MeetingVenueName = venueName;
                list.Add(mv);
            }

            reader.Close();
            con.Close();

            return list;
        }

        private PagedListViewModel<MeetingVenueModel> BuildMeetingVenuePage(string? searchtext, int page, string? sortBy, string? sortDirection)
        {
            IEnumerable<MeetingVenueModel> query = GetAllMeetingVenues(searchtext);
            bool isDesc = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

            query = (sortBy ?? "name").ToLowerInvariant() switch
            {
                "id" => isDesc ? query.OrderByDescending(x => x.MeetingVenueID) : query.OrderBy(x => x.MeetingVenueID),
                _ => isDesc ? query.OrderByDescending(x => x.MeetingVenueName) : query.OrderBy(x => x.MeetingVenueName)
            };

            List<MeetingVenueModel> ordered = query.ToList();
            const int pageSize = 10;
            page = Math.Max(page, 1);

            return new PagedListViewModel<MeetingVenueModel>
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

        public IActionResult ExportToExcel()
        {
            try
            {
                DataTable dt = new DataTable();

            SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "PR_MeetingVenue_SelectAll";
            cmd.Parameters.AddWithValue("@CompanyName", HttpContext.Session.GetString("CompanyName") ?? string.Empty);

                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                dt.Load(dr);
                dr.Close();
                con.Close();

                using (XLWorkbook workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("MeetingVenues");

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

                        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "MeetingVenueList.xlsx");
                    }
                }
            }
            catch
            {
                TempData["ErrorMessage"] = "Error exporting data. Please try again.";
                return RedirectToAction("MeetingVenueList");
            }
        }

        public IActionResult DownloadImportTemplate()
        {
            byte[] content = ExcelImportService.BuildTemplate(
                "MeetingVenuesImport",
                new[] { "MeetingVenueName" },
                new List<IReadOnlyList<string>>
                {
                    new[] { "Conference Room A" }
                });

            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "MeetingVenueImportTemplate.xlsx");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ImportFromExcel(IFormFile? excelFile)
        {
            if (!ExcelImportService.IsExcelFile(excelFile))
            {
                TempData["ErrorMessage"] = $"Please upload a valid Excel file up to {ExcelImportService.MaxFileSizeBytes / (1024 * 1024)} MB.";
                return RedirectToAction("MeetingVenueList");
            }

            if (!ExcelImportService.HasRequiredHeaders(excelFile!, new[] { "MeetingVenueName" }, out string headerMessage))
            {
                TempData["ErrorMessage"] = headerMessage;
                return RedirectToAction("MeetingVenueList");
            }

            List<Dictionary<string, string>> rows = ExcelImportService.ReadRows(excelFile!);
            if (rows.Count == 0)
            {
                TempData["ErrorMessage"] = "Excel file is empty.";
                return RedirectToAction("MeetingVenueList");
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
                string meetingVenueName = ExcelImportService.GetValue(row, "MeetingVenueName");
                string summary = meetingVenueName;
                if (string.IsNullOrWhiteSpace(meetingVenueName))
                {
                    skippedCount++;
                    reportRows.Add(new ImportReportRowModel { RowNumber = index + 2, Status = "Skipped", Message = "MeetingVenueName is required.", DataSummary = summary });
                    continue;
                }

                using SqlCommand checkCmd = new SqlCommand("SELECT COUNT(*) FROM MOM_MeetingVenue WHERE MeetingVenueName = @MeetingVenueName AND CompanyName = @CompanyName", con);
                checkCmd.Parameters.AddWithValue("@MeetingVenueName", meetingVenueName);
                checkCmd.Parameters.AddWithValue("@CompanyName", companyName);

                if (Convert.ToInt32(checkCmd.ExecuteScalar()) > 0)
                {
                    skippedCount++;
                    reportRows.Add(new ImportReportRowModel { RowNumber = index + 2, Status = "Skipped", Message = "Venue already exists in current company.", DataSummary = summary });
                    continue;
                }

                using SqlCommand cmd = new SqlCommand("PR_MeetingVenue_Insert", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@MeetingVenueName", meetingVenueName);
                cmd.Parameters.AddWithValue("@CompanyName", companyName);
                cmd.Parameters.AddWithValue("@Modified", DateTime.Now);
                cmd.ExecuteNonQuery();
                importedCount++;
                reportRows.Add(new ImportReportRowModel { RowNumber = index + 2, Status = "Imported", Message = "Venue created successfully.", DataSummary = summary });
            }

            AuditLogService.Log(
                companyName,
                HttpContext.Session.GetInt32("UserID"),
                HttpContext.Session.GetString("UserName"),
                HttpContext.Session.GetString("UserRole"),
                "Import",
                "MeetingVenue",
                null,
                "Meeting venues imported from Excel",
                $"{importedCount} meeting venues imported and {skippedCount} skipped.");

            TempData["ImportReportPath"] = ImportReportService.SaveReport("MeetingVenueImport", companyName, HttpContext.Session.GetInt32("UserID"), HttpContext.Session.GetString("UserName"), importedCount, skippedCount, reportRows);
            TempData["SuccessMessage"] = $"Meeting venue import completed. Imported: {importedCount}, Skipped: {skippedCount}.";
            return RedirectToAction("MeetingVenueList");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]

        public IActionResult Save(MeetingVenueModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("MeetingVenueAddEdit", model);
            }

            SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            con.Open();
            string companyName = HttpContext.Session.GetString("CompanyName") ?? string.Empty;

            if (model.MeetingVenueID == 0)
            {
                SqlCommand checkCmd = new SqlCommand();
                checkCmd.Connection = con;
                checkCmd.CommandText = "SELECT COUNT(*) FROM MOM_MeetingVenue WHERE MeetingVenueName = @MeetingVenueName AND CompanyName = @CompanyName";
                checkCmd.CommandType = CommandType.Text;
                checkCmd.Parameters.AddWithValue("@MeetingVenueName", model.MeetingVenueName);
                checkCmd.Parameters.AddWithValue("@CompanyName", companyName);

                int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                if (count > 0)
                {
                    con.Close();
                    ModelState.AddModelError("MeetingVenueName", "Meeting Venue already exists.");
                    return View("MeetingVenueAddEdit", model);
                }
            }
            else
            {
                SqlCommand checkCmd = new SqlCommand();
                checkCmd.Connection = con;
                checkCmd.CommandText = "SELECT COUNT(*) FROM MOM_MeetingVenue WHERE MeetingVenueName = @MeetingVenueName AND MeetingVenueID <> @MeetingVenueID AND CompanyName = @CompanyName";
                checkCmd.CommandType = CommandType.Text;
                checkCmd.Parameters.AddWithValue("@MeetingVenueName", model.MeetingVenueName);
                checkCmd.Parameters.AddWithValue("@MeetingVenueID", model.MeetingVenueID);
                checkCmd.Parameters.AddWithValue("@CompanyName", companyName);

                int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                if (count > 0)
                {
                    con.Close();
                    ModelState.AddModelError("MeetingVenueName", "Meeting Venue already exists.");
                    return View("MeetingVenueAddEdit", model);
                }
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandType = CommandType.StoredProcedure;

            if (model.MeetingVenueID == 0)
            {
                cmd.CommandText = "PR_MeetingVenue_Insert";
                cmd.Parameters.AddWithValue("@MeetingVenueName", model.MeetingVenueName);
                cmd.Parameters.AddWithValue("@CompanyName", companyName);
                cmd.Parameters.AddWithValue("@Modified", DateTime.Now);
            }
            else
            {
                cmd.CommandText = "PR_MeetingVenue_UpdateByPK";
                cmd.Parameters.AddWithValue("@MeetingVenueID", model.MeetingVenueID);
                cmd.Parameters.AddWithValue("@MeetingVenueName", model.MeetingVenueName);
                cmd.Parameters.AddWithValue("@CompanyName", companyName);
            }
            TempData["SuccessMessage"] = model.MeetingVenueID == 0 ? "Meeting venue added successfully." : "Meeting venue updated successfully.";
            cmd.ExecuteNonQuery();
            con.Close();
            AuditLogService.Log(
                HttpContext.Session.GetString("CompanyName"),
                HttpContext.Session.GetInt32("UserID"),
                HttpContext.Session.GetString("UserName"),
                HttpContext.Session.GetString("UserRole"),
                model.MeetingVenueID == 0 ? "Create" : "Update",
                "MeetingVenue",
                model.MeetingVenueID == 0 ? null : model.MeetingVenueID.ToString(),
                model.MeetingVenueID == 0 ? "Meeting venue created" : "Meeting venue updated",
                $"{model.MeetingVenueName} venue was {(model.MeetingVenueID == 0 ? "created" : "updated")}.");

            return RedirectToAction("MeetingVenueList");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int MeetingVenueID)
        {
            try
            {
                SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandText = "PR_MeetingVenue_DeleteByPK";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@MeetingVenueID", MeetingVenueID);

                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();
                AuditLogService.Log(
                    HttpContext.Session.GetString("CompanyName"),
                    HttpContext.Session.GetInt32("UserID"),
                    HttpContext.Session.GetString("UserName"),
                    HttpContext.Session.GetString("UserRole"),
                    "Delete",
                    "MeetingVenue",
                    MeetingVenueID.ToString(),
                    "Meeting venue deleted",
                    $"Meeting venue #{MeetingVenueID} was deleted.");
                TempData["SuccessMessage"] = "Meeting venue deleted successfully.";
            }
            catch
            {
                TempData["DeleteError"] = "FK violation: linked data exists.";
            }

            return RedirectToAction("MeetingVenueList");
        }
        #endregion
    }
}












