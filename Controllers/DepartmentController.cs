using Meeting_Of_Minutes.Models;
using Meeting_Of_Minutes.Services;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using Microsoft.Data.SqlClient;
using ClosedXML.Excel;

namespace Meeting_Of_Minutes.Controllers
{
    public class DepartmentController : Controller
    {
        #region Addedit
        public IActionResult DepartmentAddEdit(int? id)
        {
            DepartmentModel model = new DepartmentModel();

            if (id.HasValue)
            {
                SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandText = "PR_Department_SelectByPK";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@DepartmentID", id.Value);
                cmd.Parameters.AddWithValue("@CompanyName", GetCurrentCompanyName());

                con.Open();
                SqlDataReader sdr = cmd.ExecuteReader();

                if (sdr.Read())
                {
                    model.DepartmentID = Convert.ToInt32(sdr["DepartmentID"]);
                    model.DepartmentName = sdr["DepartmentName"].ToString();
                    model.CompanyName = sdr["CompanyName"].ToString();
                }

                sdr.Close();
                con.Close();
            }

            if (model.DepartmentID != 0 && !CanAccessDepartment(model.DepartmentID))
            {
                return RedirectToAction("DepartmentList");
            }

            return View(model);
        }
        #endregion

        #region GetAll
        [HttpGet]
        public IActionResult DepartmentList(string? searchtext, int page = 1, string sortBy = "name", string sortDirection = "asc")
        {
            searchtext = string.IsNullOrWhiteSpace(searchtext) ? null : searchtext;
            ViewBag.searchtext = searchtext;
            PagedListViewModel<DepartmentModel> departments = BuildDepartmentPage(searchtext, page, sortBy, sortDirection);
            return View(departments);
        }

        [HttpPost]
        public IActionResult DepartmentList(IFormCollection formdata)
        {
            string? searchtext = formdata["searchtext"].ToString();
            return RedirectToAction(nameof(DepartmentList), new { searchtext });
        }
        #endregion

        #region SearchGetAll
        public List<DepartmentModel> GetAllDepartment(string? searchtext)
        {
            List<DepartmentModel> departments = new List<DepartmentModel>();

            SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_Department_SelectAll";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@CompanyName", GetCurrentCompanyName());

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
                DepartmentModel department = new DepartmentModel();
                department.DepartmentID = Convert.ToInt32(sdr["DepartmentID"]);
                department.DepartmentName = sdr["DepartmentName"].ToString();
                department.CompanyName = sdr["CompanyName"].ToString();
                department.StaffCount = Convert.ToInt32(sdr["StaffCount"]);
                department.MeetingsCount = Convert.ToInt32(sdr["MeetingsCount"]);
                if (CanAccessDepartment(department.DepartmentID))
                {
                    departments.Add(department);
                }
            }

            sdr.Close();
            con.Close();

            return departments;
        }

        public PagedListViewModel<DepartmentModel> BuildDepartmentPage(string? searchtext, int page, string? sortBy, string? sortDirection)
        {
            IEnumerable<DepartmentModel> query = GetAllDepartment(searchtext);
            bool isDesc = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

            query = (sortBy ?? "name").ToLowerInvariant() switch
            {
                "staff" => isDesc ? query.OrderByDescending(x => x.StaffCount).ThenBy(x => x.DepartmentName) : query.OrderBy(x => x.StaffCount).ThenBy(x => x.DepartmentName),
                "meetings" => isDesc ? query.OrderByDescending(x => x.MeetingsCount).ThenBy(x => x.DepartmentName) : query.OrderBy(x => x.MeetingsCount).ThenBy(x => x.DepartmentName),
                _ => isDesc ? query.OrderByDescending(x => x.DepartmentName) : query.OrderBy(x => x.DepartmentName)
            };

            List<DepartmentModel> ordered = query.ToList();
            const int pageSize = 10;
            page = Math.Max(page, 1);

            return new PagedListViewModel<DepartmentModel>
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
        #endregion

        #region ExportToExcel
        public IActionResult ExportToExcel()
        {
            try
            {
                List<DepartmentModel> items = GetAllDepartment(null);

                using (XLWorkbook workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Departments");
                    string[] headers = { "DepartmentID", "DepartmentName", "CompanyName", "StaffCount", "MeetingsCount" };
                    for (int i = 0; i < headers.Length; i++)
                    {
                        worksheet.Cell(1, i + 1).Value = headers[i];
                        worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                    }

                    for (int row = 0; row < items.Count; row++)
                    {
                        DepartmentModel item = items[row];
                        worksheet.Cell(row + 2, 1).Value = item.DepartmentID;
                        worksheet.Cell(row + 2, 2).Value = item.DepartmentName;
                        worksheet.Cell(row + 2, 3).Value = item.CompanyName;
                        worksheet.Cell(row + 2, 4).Value = item.StaffCount;
                        worksheet.Cell(row + 2, 5).Value = item.MeetingsCount;
                    }

                    worksheet.Columns().AdjustToContents();

                    using (MemoryStream stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        byte[] content = stream.ToArray();

                        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "DepartmentList.xlsx");
                    }
                }
            }
            catch
            {
                TempData["ErrorMessage"] = "Error exporting data. Please try again.";
                return RedirectToAction("DepartmentList");
            }
        }
        #endregion

        #region Import
        public IActionResult DownloadImportTemplate()
        {
            byte[] content = ExcelImportService.BuildTemplate(
                "DepartmentsImport",
                new[] { "DepartmentName" },
                new List<IReadOnlyList<string>>
                {
                    new[] { "Human Resources" }
                });

            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "DepartmentImportTemplate.xlsx");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult ValidateImportTemplate(IFormFile? excelFile)
        {
            bool isValid = ExcelImportService.ValidateImportFile(excelFile, new[] { "DepartmentName" }, out string message);
            return Json(new { ok = isValid, message });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ImportFromExcel(IFormFile? excelFile)
        {
            if (!ExcelImportService.IsExcelFile(excelFile))
            {
                TempData["ErrorMessage"] = $"Please upload a valid Excel file up to {ExcelImportService.MaxFileSizeBytes / (1024 * 1024)} MB.";
                return RedirectToAction("DepartmentList");
            }

            if (!ExcelImportService.HasRequiredHeaders(excelFile!, new[] { "DepartmentName" }, out string headerMessage))
            {
                TempData["ErrorMessage"] = headerMessage;
                return RedirectToAction("DepartmentList");
            }

            List<Dictionary<string, string>> rows = ExcelImportService.ReadRows(excelFile!);
            if (rows.Count == 0)
            {
                TempData["ErrorMessage"] = "Excel file is empty.";
                return RedirectToAction("DepartmentList");
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
                string summary = departmentName;
                if (string.IsNullOrWhiteSpace(departmentName))
                {
                    skippedCount++;
                    reportRows.Add(new ImportReportRowModel { RowNumber = index + 2, Status = "Skipped", Message = "DepartmentName is required.", DataSummary = summary });
                    continue;
                }

                using SqlCommand checkCmd = new SqlCommand("SELECT COUNT(*) FROM MOM_Department WHERE DepartmentName = @DepartmentName AND CompanyName = @CompanyName", con);
                checkCmd.Parameters.AddWithValue("@DepartmentName", departmentName);
                checkCmd.Parameters.AddWithValue("@CompanyName", companyName);

                if (Convert.ToInt32(checkCmd.ExecuteScalar()) > 0)
                {
                    skippedCount++;
                    reportRows.Add(new ImportReportRowModel { RowNumber = index + 2, Status = "Skipped", Message = "Department already exists in current company.", DataSummary = summary });
                    continue;
                }

                using SqlCommand cmd = new SqlCommand("PR_Department_Insert", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@DepartmentName", departmentName);
                cmd.Parameters.AddWithValue("@CompanyName", companyName);
                cmd.Parameters.AddWithValue("@Modified", DateTime.Now);
                cmd.ExecuteNonQuery();
                importedCount++;
                reportRows.Add(new ImportReportRowModel { RowNumber = index + 2, Status = "Imported", Message = "Department created successfully.", DataSummary = summary });
            }

            AuditLogService.Log(
                companyName,
                HttpContext.Session.GetInt32("UserID"),
                HttpContext.Session.GetString("UserName"),
                HttpContext.Session.GetString("UserRole"),
                "Import",
                "Department",
                null,
                "Departments imported from Excel",
                $"{importedCount} departments imported and {skippedCount} skipped.");

            TempData["ImportReportPath"] = ImportReportService.SaveReport("DepartmentImport", companyName, HttpContext.Session.GetInt32("UserID"), HttpContext.Session.GetString("UserName"), importedCount, skippedCount, reportRows);
            TempData["SuccessMessage"] = $"Department import completed. Imported: {importedCount}, Skipped: {skippedCount}.";
            return RedirectToAction("DepartmentList");
        }
        #endregion

        #region View
        public IActionResult DepartmentView(int id)
        {
            DepartmentModel model = new DepartmentModel();

            SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "PR_Department_SelectByPK";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@DepartmentID", id);
            cmd.Parameters.AddWithValue("@CompanyName", GetCurrentCompanyName());

            con.Open();
            SqlDataReader sdr = cmd.ExecuteReader();

            if (sdr.Read())
            {
                model.DepartmentID = Convert.ToInt32(sdr["DepartmentID"]);
                model.DepartmentName = sdr["DepartmentName"].ToString();
                model.CompanyName = sdr["CompanyName"].ToString();
                model.StaffCount = Convert.ToInt32(sdr["StaffCount"]);
                model.MeetingsCount = Convert.ToInt32(sdr["MeetingsCount"]);
            }

            sdr.Close();
            con.Close();

            if (model.DepartmentID != 0 && !CanAccessDepartment(model.DepartmentID))
            {
                return RedirectToAction("DepartmentList");
            }

            return View(model);
        }

        #endregion

        #region Update Insret
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Save(DepartmentModel model)
        {
            if (model.DepartmentID == 0 && !RoleAccessService.IsSuperAdmin(HttpContext.Session.GetString("UserRole")))
            {
                TempData["ErrorMessage"] = "Only the company Super Admin can create new departments.";
                return RedirectToAction("DepartmentList");
            }

            if (model.DepartmentID != 0 && !CanAccessDepartment(model.DepartmentID))
            {
                TempData["ErrorMessage"] = "You can only manage your own department.";
                return RedirectToAction("DepartmentList");
            }

            if (!ModelState.IsValid)
            {
                return View("DepartmentAddEdit", model);
            }

            SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            con.Open();

            if (model.DepartmentID == 0)
            {
                SqlCommand checkCmd = new SqlCommand();
                checkCmd.Connection = con;
                checkCmd.CommandText = "SELECT COUNT(*) FROM MOM_Department WHERE DepartmentName = @DepartmentName AND CompanyName = @CompanyName";
                checkCmd.CommandType = CommandType.Text;
                checkCmd.Parameters.AddWithValue("@DepartmentName", model.DepartmentName);
                checkCmd.Parameters.AddWithValue("@CompanyName", GetCurrentCompanyName());

                int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                if (count > 0)
                {
                    con.Close();
                    ModelState.AddModelError("DepartmentName", "Department already exists.");
                    return View("DepartmentAddEdit", model);
                }
            }
            else
            {
                SqlCommand checkCmd = new SqlCommand();
                checkCmd.Connection = con;
                checkCmd.CommandText = "SELECT COUNT(*) FROM MOM_Department WHERE DepartmentName = @DepartmentName AND CompanyName = @CompanyName AND DepartmentID <> @DepartmentID";
                checkCmd.CommandType = CommandType.Text;
                checkCmd.Parameters.AddWithValue("@DepartmentName", model.DepartmentName);
                checkCmd.Parameters.AddWithValue("@CompanyName", GetCurrentCompanyName());
                checkCmd.Parameters.AddWithValue("@DepartmentID", model.DepartmentID);

                int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                if (count > 0)
                {
                    con.Close();
                    ModelState.AddModelError("DepartmentName", "Department already exists.");
                    return View("DepartmentAddEdit", model);
                }
            }

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandType = CommandType.StoredProcedure;

            if (model.DepartmentID == 0)
            {
                cmd.CommandText = "PR_Department_Insert";
                cmd.Parameters.AddWithValue("@DepartmentName", model.DepartmentName);
                cmd.Parameters.AddWithValue("@CompanyName", GetCurrentCompanyName());
                cmd.Parameters.AddWithValue("@Modified", DateTime.Now);
            }
            else
            {
                cmd.CommandText = "PR_Department_UpdateByPK";
                cmd.Parameters.AddWithValue("@DepartmentID", model.DepartmentID);
                cmd.Parameters.AddWithValue("@DepartmentName", model.DepartmentName);
                cmd.Parameters.AddWithValue("@CompanyName", GetCurrentCompanyName());
            }

            TempData["SuccessMessage"] = model.DepartmentID == 0 ? "Department added successfully." : "Department updated successfully.";
            cmd.ExecuteNonQuery();
            con.Close();
            AuditLogService.Log(
                GetCurrentCompanyName(),
                HttpContext.Session.GetInt32("UserID"),
                HttpContext.Session.GetString("UserName"),
                HttpContext.Session.GetString("UserRole"),
                model.DepartmentID == 0 ? "Create" : "Update",
                "Department",
                model.DepartmentID == 0 ? null : model.DepartmentID.ToString(),
                model.DepartmentID == 0 ? "Department created" : "Department updated",
                $"{model.DepartmentName} department was {(model.DepartmentID == 0 ? "created" : "updated")}.");

            return RedirectToAction("DepartmentList");
        }
        #endregion

        #region Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int DepartmentID)
        {
            if (!CanAccessDepartment(DepartmentID))
            {
                TempData["ErrorMessage"] = "You can only manage your own department.";
                return RedirectToAction("DepartmentList");
            }

            try
            {
                SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandText = "PR_Department_DeleteByPK";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@DepartmentID", DepartmentID);

                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();
                AuditLogService.Log(
                    GetCurrentCompanyName(),
                    HttpContext.Session.GetInt32("UserID"),
                    HttpContext.Session.GetString("UserName"),
                    HttpContext.Session.GetString("UserRole"),
                    "Delete",
                    "Department",
                    DepartmentID.ToString(),
                    "Department deleted",
                    $"Department #{DepartmentID} was deleted.");
                TempData["SuccessMessage"] = "Department deleted successfully.";
            }
            catch
            {
                TempData["DeleteError"] = "FK violation: department linked data exists.";
            }

            return RedirectToAction("DepartmentList");
        }

        #endregion

        public string GetCurrentCompanyName()
        {
            return HttpContext.Session.GetString("CompanyName") ?? string.Empty;
        }

        private bool CanAccessDepartment(int departmentId)
        {
            if (RoleAccessService.IsSuperAdmin(HttpContext.Session.GetString("UserRole")))
            {
                return true;
            }

            int? currentDepartmentId = HttpContext.Session.GetInt32("DepartmentID");
            return currentDepartmentId.HasValue && currentDepartmentId.Value == departmentId;
        }
    }
}









