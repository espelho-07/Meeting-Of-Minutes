using ClosedXML.Excel;

namespace Meeting_Of_Minutes.Services
{
    public static class ExcelImportService
    {
        public const long MaxFileSizeBytes = 5 * 1024 * 1024;

        public static byte[] BuildTemplate(string sheetName, IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>>? sampleRows = null)
        {
            using XLWorkbook workbook = new XLWorkbook();
            IXLWorksheet worksheet = workbook.Worksheets.Add(sheetName);

            for (int i = 0; i < headers.Count; i++)
            {
                IXLCell cell = worksheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#EEF2FF");
            }

            if (sampleRows != null)
            {
                for (int rowIndex = 0; rowIndex < sampleRows.Count; rowIndex++)
                {
                    IReadOnlyList<string> sampleRow = sampleRows[rowIndex];
                    for (int colIndex = 0; colIndex < sampleRow.Count; colIndex++)
                    {
                        worksheet.Cell(rowIndex + 2, colIndex + 1).Value = sampleRow[colIndex];
                    }
                }
            }

            worksheet.Columns().AdjustToContents();

            using MemoryStream stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public static List<Dictionary<string, string>> ReadRows(IFormFile excelFile)
        {
            try
            {
                using MemoryStream memoryStream = new MemoryStream();
                excelFile.CopyTo(memoryStream);
                memoryStream.Position = 0;

                using XLWorkbook workbook = new XLWorkbook(memoryStream);
                IXLWorksheet worksheet = workbook.Worksheet(1);
                IXLRange? usedRange = worksheet.RangeUsed();

                if (usedRange == null)
                {
                    return new List<Dictionary<string, string>>();
                }

                List<string> headers = usedRange.Row(1)
                    .Cells()
                    .Select(cell => cell.GetString().Trim())
                    .Where(header => !string.IsNullOrWhiteSpace(header))
                    .ToList();

                List<Dictionary<string, string>> rows = new List<Dictionary<string, string>>();

                foreach (IXLRow row in usedRange.RowsUsed().Skip(1))
                {
                    Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    bool hasAnyValue = false;

                    for (int index = 0; index < headers.Count; index++)
                    {
                        string header = headers[index];
                        string value = row.Cell(index + 1).GetFormattedString().Trim();
                        values[header] = value;
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            hasAnyValue = true;
                        }
                    }

                    if (hasAnyValue)
                    {
                        rows.Add(values);
                    }
                }

                return rows;
            }
            catch
            {
                return new List<Dictionary<string, string>>();
            }
        }

        public static bool IsExcelFile(IFormFile? excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
            {
                return false;
            }

            if (excelFile.Length > MaxFileSizeBytes)
            {
                return false;
            }

            string extension = Path.GetExtension(excelFile.FileName);
            return extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".xls", StringComparison.OrdinalIgnoreCase);
        }

        public static bool HasRequiredHeaders(IFormFile excelFile, IReadOnlyList<string> expectedHeaders, out string message)
        {
            try
            {
                using MemoryStream memoryStream = new MemoryStream();
                excelFile.CopyTo(memoryStream);
                memoryStream.Position = 0;

                using XLWorkbook workbook = new XLWorkbook(memoryStream);
                IXLWorksheet worksheet = workbook.Worksheet(1);
                IXLRange? usedRange = worksheet.RangeUsed();

                if (usedRange == null)
                {
                    message = "Excel file is empty.";
                    return false;
                }

                List<string> actualHeaders = usedRange.Row(1)
                    .Cells()
                    .Select(cell => cell.GetString().Trim())
                    .Where(header => !string.IsNullOrWhiteSpace(header))
                    .ToList();

                List<string> missingHeaders = expectedHeaders
                    .Where(expected => !actualHeaders.Any(actual => actual.Equals(expected, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                if (missingHeaders.Count > 0)
                {
                    message = "Missing required columns: " + string.Join(", ", missingHeaders);
                    return false;
                }

                message = string.Empty;
                return true;
            }
            catch
            {
                message = "Excel file could not be read. Upload a valid .xlsx file generated from the provided template.";
                return false;
            }
        }

        public static string GetValue(IDictionary<string, string> row, string key)
        {
            return row.TryGetValue(key, out string? value) ? value.Trim() : string.Empty;
        }

        public static bool ParseBoolean(string value)
        {
            string normalized = value.Trim().ToLowerInvariant();
            return normalized == "true" || normalized == "yes" || normalized == "1" || normalized == "y";
        }
    }
}
