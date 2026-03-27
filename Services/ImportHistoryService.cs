using System.Text.Json;
using Meeting_Of_Minutes.Models;

namespace Meeting_Of_Minutes.Services
{
    public static class ImportHistoryService
    {
        private static readonly object SyncRoot = new object();

        public static void Add(ImportHistoryEntryModel entry)
        {
            lock (SyncRoot)
            {
                List<ImportHistoryEntryModel> entries = GetAllInternal();
                entries.Insert(0, entry);
                SaveAll(entries);
            }
        }

        public static List<ImportHistoryEntryModel> GetAllByCompany(string companyName)
        {
            lock (SyncRoot)
            {
                return GetAllInternal()
                    .Where(x => string.Equals(x.CompanyName, companyName, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(x => x.Created)
                    .ToList();
            }
        }

        private static List<ImportHistoryEntryModel> GetAllInternal()
        {
            string filePath = GetStoragePath();
            if (!File.Exists(filePath))
            {
                return new List<ImportHistoryEntryModel>();
            }

            string json = File.ReadAllText(filePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<ImportHistoryEntryModel>();
            }

            return JsonSerializer.Deserialize<List<ImportHistoryEntryModel>>(json) ?? new List<ImportHistoryEntryModel>();
        }

        private static void SaveAll(List<ImportHistoryEntryModel> entries)
        {
            string filePath = GetStoragePath();
            string? directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonSerializer.Serialize(entries);
            File.WriteAllText(filePath, json);
        }

        private static string GetStoragePath()
        {
            return Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "import-history.json");
        }
    }
}
