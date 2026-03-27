using ClosedXML.Excel;
using Meeting_Of_Minutes.Models;

namespace Meeting_Of_Minutes.Services
{
    public static class ImportReportService
    {
        public static string SaveReport(string reportName, string companyName, int? userId, string? userName, int importedCount, int skippedCount, IReadOnlyList<ImportReportRowModel> rows)
        {
            string reportsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "import-reports");
            if (!Directory.Exists(reportsFolder))
            {
                Directory.CreateDirectory(reportsFolder);
            }

            string safeName = string.Concat(reportName.Where(char.IsLetterOrDigit));
            if (string.IsNullOrWhiteSpace(safeName))
            {
                safeName = "Import";
            }

            string fileName = $"{safeName}_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid():N}.xlsx";
            string fullPath = Path.Combine(reportsFolder, fileName);

            using XLWorkbook workbook = new XLWorkbook();
            IXLWorksheet worksheet = workbook.Worksheets.Add("Import Report");

            worksheet.Cell(1, 1).Value = "Row Number";
            worksheet.Cell(1, 2).Value = "Status";
            worksheet.Cell(1, 3).Value = "Message";
            worksheet.Cell(1, 4).Value = "Data Summary";

            for (int col = 1; col <= 4; col++)
            {
                worksheet.Cell(1, col).Style.Font.Bold = true;
                worksheet.Cell(1, col).Style.Fill.BackgroundColor = XLColor.FromHtml("#EEF2FF");
            }

            for (int i = 0; i < rows.Count; i++)
            {
                ImportReportRowModel row = rows[i];
                worksheet.Cell(i + 2, 1).Value = row.RowNumber;
                worksheet.Cell(i + 2, 2).Value = row.Status;
                worksheet.Cell(i + 2, 3).Value = row.Message;
                worksheet.Cell(i + 2, 4).Value = row.DataSummary;
            }

            worksheet.Columns().AdjustToContents();
            workbook.SaveAs(fullPath);

            ImportHistoryService.Add(new ImportHistoryEntryModel
            {
                CompanyName = companyName,
                ModuleName = reportName,
                UserID = userId,
                UserName = userName ?? string.Empty,
                ImportedCount = importedCount,
                SkippedCount = skippedCount,
                ReportPath = "/import-reports/" + fileName,
                Created = DateTime.Now
            });

            return "/import-reports/" + fileName;
        }
    }
}
