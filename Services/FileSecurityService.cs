using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Meeting_Of_Minutes.Services
{
    public static class FileSecurityService
    {
        private static readonly HashSet<string> AllowedDocumentExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".png", ".jpg", ".jpeg", ".doc", ".docx", ".xls", ".xlsx"
        };

        public const long MaxDocumentFileSizeBytes = 5 * 1024 * 1024;

        public static bool TrySaveDocument(IWebHostEnvironment environment, IFormFile? file, string folderName, string filePrefix, out string relativePath, out string errorMessage)
        {
            relativePath = string.Empty;
            errorMessage = string.Empty;

            if (file == null || file.Length == 0)
            {
                errorMessage = "Please upload a document.";
                return false;
            }

            if (file.Length > MaxDocumentFileSizeBytes)
            {
                errorMessage = $"File size must be {MaxDocumentFileSizeBytes / (1024 * 1024)} MB or less.";
                return false;
            }

            string extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension) || !AllowedDocumentExtensions.Contains(extension))
            {
                errorMessage = "Only PDF, Excel, Word, JPG, and PNG files are allowed.";
                return false;
            }

            string webRoot = string.IsNullOrWhiteSpace(environment.WebRootPath)
                ? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")
                : environment.WebRootPath;

            string targetFolder = Path.Combine(webRoot, folderName);
            Directory.CreateDirectory(targetFolder);

            string safePrefix = Regex.Replace(filePrefix ?? "document", "[^a-zA-Z0-9_-]", string.Empty);
            if (string.IsNullOrWhiteSpace(safePrefix))
            {
                safePrefix = "document";
            }

            string fileName = $"{safePrefix}_{DateTime.UtcNow:yyyyMMddHHmmss}_{Convert.ToHexString(RandomNumberGenerator.GetBytes(8))}{extension.ToLowerInvariant()}";
            string fullPath = Path.Combine(targetFolder, fileName);

            using FileStream fileStream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            file.CopyTo(fileStream);

            relativePath = $"/{folderName.Trim('/').Trim('\\')}/{fileName}";
            return true;
        }

        public static string SanitizeStoredDocumentPath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            string trimmed = path.Trim();
            if (!trimmed.StartsWith('/'))
            {
                return string.Empty;
            }

            if (trimmed.StartsWith("//", StringComparison.Ordinal) ||
                trimmed.Contains("..", StringComparison.Ordinal) ||
                trimmed.Contains('\\', StringComparison.Ordinal) ||
                trimmed.Contains('\0'))
            {
                return string.Empty;
            }

            return trimmed;
        }

        public static bool IsSafeDocumentPath(string? path)
        {
            return !string.IsNullOrWhiteSpace(SanitizeStoredDocumentPath(path));
        }
    }
}
