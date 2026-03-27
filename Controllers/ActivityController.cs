using Meeting_Of_Minutes.Models;
using Meeting_Of_Minutes.Services;
using Microsoft.AspNetCore.Mvc;
using ClosedXML.Excel;
using System.Data;

namespace Meeting_Of_Minutes.Controllers
{
    public class ActivityController : Controller
    {
        public IActionResult ActivityLog(string? searchtext, string? actionType, string? entityName)
        {
            List<AuditLogModel> logs;
            bool isAdminUser = RoleAccessService.IsAdminOrHigher(HttpContext.Session.GetString("UserRole"));

            if (isAdminUser)
            {
                logs = AuditLogService.GetRecentByCompany(HttpContext.Session.GetString("CompanyName") ?? string.Empty, 250);
            }
            else
            {
                int? userId = HttpContext.Session.GetInt32("UserID");
                logs = userId.HasValue ? AuditLogService.GetRecentByUser(userId.Value, 250) : new List<AuditLogModel>();
            }

            List<string> actionTypes = logs.Select(x => x.ActionType).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x).ToList();
            List<string> entityNames = logs.Select(x => x.EntityName).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x).ToList();
            logs = ApplyFilters(logs, searchtext, actionType, entityName);

            ViewBag.IsAdminUser = isAdminUser;
            ViewBag.SearchText = searchtext ?? string.Empty;
            ViewBag.ActionType = actionType ?? string.Empty;
            ViewBag.EntityName = entityName ?? string.Empty;
            ViewBag.ActionTypes = actionTypes;
            ViewBag.EntityNames = entityNames;
            return View(logs);
        }

        public IActionResult ExportToExcel(string? searchtext, string? actionType, string? entityName)
        {
            bool isAdminUser = RoleAccessService.IsAdminOrHigher(HttpContext.Session.GetString("UserRole"));
            List<AuditLogModel> logs = isAdminUser
                ? AuditLogService.GetRecentByCompany(HttpContext.Session.GetString("CompanyName") ?? string.Empty, 500)
                : HttpContext.Session.GetInt32("UserID").HasValue
                    ? AuditLogService.GetRecentByUser(HttpContext.Session.GetInt32("UserID")!.Value, 500)
                    : new List<AuditLogModel>();

            logs = ApplyFilters(logs, searchtext, actionType, entityName);

            DataTable dt = new DataTable();
            dt.Columns.Add("Date");
            dt.Columns.Add("Time");
            dt.Columns.Add("User");
            dt.Columns.Add("Role");
            dt.Columns.Add("Action");
            dt.Columns.Add("Entity");
            dt.Columns.Add("Entity ID");
            dt.Columns.Add("Title");
            dt.Columns.Add("Description");

            foreach (AuditLogModel log in logs)
            {
                dt.Rows.Add(
                    log.Created.ToString("dd MMM yyyy"),
                    log.Created.ToString("hh:mm tt"),
                    log.UserName,
                    log.UserRole,
                    log.ActionType,
                    log.EntityName,
                    log.EntityID,
                    log.Title,
                    log.Description);
            }

            using XLWorkbook workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("ActivityLogs");

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

            using MemoryStream stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "ActivityLog.xlsx");
        }

        private List<AuditLogModel> ApplyFilters(List<AuditLogModel> logs, string? searchtext, string? actionType, string? entityName)
        {
            IEnumerable<AuditLogModel> filtered = logs;

            if (!string.IsNullOrWhiteSpace(searchtext))
            {
                string search = searchtext.Trim();
                filtered = filtered.Where(log =>
                    (log.Title?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (log.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (log.UserName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (log.EntityName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            if (!string.IsNullOrWhiteSpace(actionType))
            {
                filtered = filtered.Where(log => string.Equals(log.ActionType, actionType, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(entityName))
            {
                filtered = filtered.Where(log => string.Equals(log.EntityName, entityName, StringComparison.OrdinalIgnoreCase));
            }

            return filtered.ToList();
        }
    }
}
