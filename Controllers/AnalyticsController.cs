using ClosedXML.Excel;
using Meeting_Of_Minutes.Models;
using Meeting_Of_Minutes.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Meeting_Of_Minutes.Controllers
{
    public class AnalyticsController : Controller
    {
        [HttpGet]
        public IActionResult Analytics(int days = 90)
        {
            days = NormalizeDays(days);
            AnalyticsViewModel model = BuildAnalyticsModel(days);
            return View(model);
        }

        [HttpGet]
        public IActionResult ExportToExcel(int days = 90)
        {
            days = NormalizeDays(days);
            AnalyticsViewModel model = BuildAnalyticsModel(days);

            DataTable dt = new DataTable();
            dt.Columns.Add("Metric");
            dt.Columns.Add("Value");

            dt.Rows.Add("Days Filter", model.DaysFilter);
            dt.Rows.Add("Total Meetings", model.TotalMeetings);
            dt.Rows.Add("Scheduled Meetings", model.ScheduledMeetings);
            dt.Rows.Add("Completed Meetings", model.CompletedMeetings);
            dt.Rows.Add("Cancelled Meetings", model.CancelledMeetings);
            dt.Rows.Add("Attendance Entries", model.AttendanceEntries);
            dt.Rows.Add("Completion Rate", $"{model.CompletionRate:0.##}%");
            dt.Rows.Add("Cancellation Rate", $"{model.CancellationRate:0.##}%");

            using XLWorkbook workbook = new XLWorkbook();
            IXLWorksheet summarySheet = workbook.Worksheets.Add("Summary");
            WriteTable(summarySheet, dt);

            IXLWorksheet trendSheet = workbook.Worksheets.Add("MonthlyTrend");
            trendSheet.Cell(1, 1).Value = "Month";
            trendSheet.Cell(1, 2).Value = "Meetings";
            trendSheet.Cell(1, 3).Value = "Cancelled";
            for (int i = 0; i < model.TrendLabels.Count; i++)
            {
                trendSheet.Cell(i + 2, 1).Value = model.TrendLabels[i];
                trendSheet.Cell(i + 2, 2).Value = model.TrendMeetingValues.ElementAtOrDefault(i);
                trendSheet.Cell(i + 2, 3).Value = model.TrendCancelledValues.ElementAtOrDefault(i);
            }
            trendSheet.Columns().AdjustToContents();

            IXLWorksheet departmentSheet = workbook.Worksheets.Add("Departments");
            departmentSheet.Cell(1, 1).Value = "Department";
            departmentSheet.Cell(1, 2).Value = "Meetings";
            for (int i = 0; i < model.TopDepartmentLabels.Count; i++)
            {
                departmentSheet.Cell(i + 2, 1).Value = model.TopDepartmentLabels[i];
                departmentSheet.Cell(i + 2, 2).Value = model.TopDepartmentValues.ElementAtOrDefault(i);
            }
            departmentSheet.Columns().AdjustToContents();

            IXLWorksheet typeSheet = workbook.Worksheets.Add("MeetingTypes");
            typeSheet.Cell(1, 1).Value = "Meeting Type";
            typeSheet.Cell(1, 2).Value = "Meetings";
            for (int i = 0; i < model.TopMeetingTypeLabels.Count; i++)
            {
                typeSheet.Cell(i + 2, 1).Value = model.TopMeetingTypeLabels[i];
                typeSheet.Cell(i + 2, 2).Value = model.TopMeetingTypeValues.ElementAtOrDefault(i);
            }
            typeSheet.Columns().AdjustToContents();

            using MemoryStream stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "AnalyticsReport.xlsx");
        }

        private AnalyticsViewModel BuildAnalyticsModel(int days)
        {
            DateTime startDate = DateTime.Today.AddDays(-days);
            bool isAdmin = IsAdmin();
            HashSet<int> allowedDepartmentIds = GetAllowedDepartmentIdsForCompany();
            List<MeetingsModel> visibleMeetings = new List<MeetingsModel>();
            int attendanceEntries = 0;

            using SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            using SqlCommand cmd = new SqlCommand("PR_Meetings_SelectAll", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@searchtext", DBNull.Value);
            con.Open();

            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    MeetingsModel meeting = new MeetingsModel
                    {
                        MeetingID = Convert.ToInt32(reader["MeetingID"]),
                        MeetingDate = reader["MeetingDate"] as DateTime?,
                        DepartmentID = reader["DepartmentID"] == DBNull.Value ? null : Convert.ToInt32(reader["DepartmentID"]),
                        MeetingTypeName = reader["MeetingTypeName"].ToString(),
                        DepartmentName = reader["DepartmentName"].ToString(),
                        MeetingVenueName = reader["MeetingVenueName"].ToString(),
                        MeetingDescription = reader["MeetingDescription"].ToString(),
                        IsCancelled = reader["IsCancelled"] != DBNull.Value && Convert.ToBoolean(reader["IsCancelled"])
                    };

                    bool canAccessMeeting = isAdmin
                        ? allowedDepartmentIds.Contains(meeting.DepartmentID ?? 0)
                        : IsMeetingAssignedToCurrentUser(meeting.MeetingID);

                    if (canAccessMeeting && meeting.MeetingDate.HasValue && meeting.MeetingDate.Value >= startDate)
                    {
                        visibleMeetings.Add(meeting);
                    }
                }
            }

            using (SqlCommand attendanceCmd = new SqlCommand("PR_MeetingMember_SelectAll", con))
            {
                attendanceCmd.CommandType = CommandType.StoredProcedure;
                using SqlDataReader attendanceReader = attendanceCmd.ExecuteReader();
                while (attendanceReader.Read())
                {
                    int meetingId = Convert.ToInt32(attendanceReader["MeetingID"]);
                    if (visibleMeetings.Any(m => m.MeetingID == meetingId))
                    {
                        attendanceEntries++;
                    }
                }
            }

            List<AuditLogModel> logs = isAdmin
                ? AuditLogService.GetRecentByCompany(GetCurrentCompanyName(), 40)
                : HttpContext.Session.GetInt32("UserID").HasValue
                    ? AuditLogService.GetRecentByUser(HttpContext.Session.GetInt32("UserID")!.Value, 40)
                    : new List<AuditLogModel>();

            List<AuditLogModel> riskSignals = logs
                .Where(log => log.ActionType is "LoginFailed" or "Cancel" or "Reject" or "Delete")
                .OrderByDescending(log => log.Created)
                .Take(6)
                .ToList();

            int totalMeetings = visibleMeetings.Count;
            int cancelledMeetings = visibleMeetings.Count(m => m.IsCancelled);
            int scheduledMeetings = visibleMeetings.Count(m => !m.IsCancelled && m.MeetingDate.HasValue && m.MeetingDate.Value > DateTime.Now);
            int completedMeetings = visibleMeetings.Count(m => !m.IsCancelled && (!m.MeetingDate.HasValue || m.MeetingDate.Value <= DateTime.Now));

            Dictionary<string, int> departmentCounts = visibleMeetings
                .GroupBy(m => string.IsNullOrWhiteSpace(m.DepartmentName) ? "Other" : m.DepartmentName!)
                .OrderByDescending(g => g.Count())
                .Take(6)
                .ToDictionary(g => g.Key, g => g.Count());

            Dictionary<string, int> meetingTypeCounts = visibleMeetings
                .GroupBy(m => string.IsNullOrWhiteSpace(m.MeetingTypeName) ? "Other" : m.MeetingTypeName!)
                .OrderByDescending(g => g.Count())
                .Take(6)
                .ToDictionary(g => g.Key, g => g.Count());

            List<string> trendLabels = new List<string>();
            List<int> trendMeetingValues = new List<int>();
            List<int> trendCancelledValues = new List<int>();

            foreach (DateTime month in Enumerable.Range(0, 6).Select(offset => new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-5 + offset)))
            {
                DateTime monthEnd = month.AddMonths(1);
                List<MeetingsModel> monthMeetings = visibleMeetings.Where(m => m.MeetingDate.HasValue && m.MeetingDate.Value >= month && m.MeetingDate.Value < monthEnd).ToList();
                trendLabels.Add(month.ToString("MMM yyyy"));
                trendMeetingValues.Add(monthMeetings.Count);
                trendCancelledValues.Add(monthMeetings.Count(m => m.IsCancelled));
            }

            return new AnalyticsViewModel
            {
                IsAdminUser = isAdmin,
                DaysFilter = days,
                TotalMeetings = totalMeetings,
                ScheduledMeetings = scheduledMeetings,
                CompletedMeetings = completedMeetings,
                CancelledMeetings = cancelledMeetings,
                AttendanceEntries = attendanceEntries,
                CompletionRate = totalMeetings == 0 ? 0 : Math.Round((decimal)completedMeetings * 100 / totalMeetings, 2),
                CancellationRate = totalMeetings == 0 ? 0 : Math.Round((decimal)cancelledMeetings * 100 / totalMeetings, 2),
                TrendLabels = trendLabels,
                TrendMeetingValues = trendMeetingValues,
                TrendCancelledValues = trendCancelledValues,
                TopDepartmentLabels = departmentCounts.Keys.ToList(),
                TopDepartmentValues = departmentCounts.Values.ToList(),
                TopMeetingTypeLabels = meetingTypeCounts.Keys.ToList(),
                TopMeetingTypeValues = meetingTypeCounts.Values.ToList(),
                RiskSignals = riskSignals
            };
        }

        private static void WriteTable(IXLWorksheet worksheet, DataTable dt)
        {
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
        }

        private int NormalizeDays(int days)
        {
            return days is 30 or 90 or 365 ? days : 90;
        }

        private bool IsAdmin()
        {
            return RoleAccessService.IsAdminOrHigher(HttpContext.Session.GetString("UserRole"));
        }

        private string GetCurrentCompanyName()
        {
            return HttpContext.Session.GetString("CompanyName") ?? string.Empty;
        }

        private HashSet<int> GetAllowedDepartmentIdsForCompany()
        {
            HashSet<int> departmentIds = new HashSet<int>();
            string companyName = GetCurrentCompanyName();

            if (string.IsNullOrWhiteSpace(companyName))
            {
                return departmentIds;
            }

            using SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            using SqlCommand cmd = new SqlCommand("SELECT DepartmentID FROM MOM_Department WHERE CompanyName = @CompanyName", con);
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.AddWithValue("@CompanyName", companyName);
            con.Open();
            using SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                departmentIds.Add(Convert.ToInt32(reader["DepartmentID"]));
            }

            return departmentIds;
        }

        private bool IsMeetingAssignedToCurrentUser(int meetingId)
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

            using SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            using SqlCommand cmd = new SqlCommand(@"SELECT COUNT(*)
                                                    FROM MOM_MeetingMember mm
                                                    INNER JOIN MOM_Meetings m ON mm.MeetingID = m.MeetingID
                                                    INNER JOIN MOM_Department d ON m.DepartmentID = d.DepartmentID
                                                    WHERE mm.MeetingID = @MeetingID
                                                      AND mm.StaffID = @StaffID
                                                      AND d.CompanyName = @CompanyName", con);
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.AddWithValue("@MeetingID", meetingId);
            cmd.Parameters.AddWithValue("@StaffID", staffId.Value);
            cmd.Parameters.AddWithValue("@CompanyName", GetCurrentCompanyName());
            con.Open();
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }
    }
}
