using System.Text.Json;
using Meeting_Of_Minutes.Models;
using Microsoft.Data.SqlClient;

namespace Meeting_Of_Minutes.Services
{
    public static class SystemSettingsService
    {
        private static readonly string SettingsFilePath = Path.Combine(AppContext.BaseDirectory, "App_Data", "system-settings.json");
        private static readonly object CacheLock = new();
        private static SystemSettingsModel? _cachedSettings;
        private static DateTime _cacheExpiresAt = DateTime.MinValue;

        public static SystemSettingsModel GetSettings()
        {
            lock (CacheLock)
            {
                if (_cachedSettings != null && _cacheExpiresAt > DateTime.Now)
                {
                    return Clone(_cachedSettings);
                }
            }

            SystemSettingsModel settings;
            if (TryReadFromDatabase(out SystemSettingsModel? dbSettings) && dbSettings != null)
            {
                settings = dbSettings;
            }
            else
            {
                settings = ReadFromJson();
            }

            lock (CacheLock)
            {
                _cachedSettings = Clone(settings);
                _cacheExpiresAt = DateTime.Now.AddMinutes(5);
            }

            return settings;
        }

        public static void SaveSettings(SystemSettingsModel model)
        {
            model.Modified = DateTime.Now;
            if (TryWriteToDatabase(model))
            {
                return;
            }

            WriteToJson(model);
            ResetCache();
        }

        public static bool IsAdminSelfRegistrationAllowed()
        {
            SystemSettingsModel settings = GetSettings();
            return settings.AllowAdminSelfRegistration;
        }

        public static void ResetCache()
        {
            lock (CacheLock)
            {
                _cachedSettings = null;
                _cacheExpiresAt = DateTime.MinValue;
            }
        }

        private static bool TryReadFromDatabase(out SystemSettingsModel? settings)
        {
            settings = null;

            try
            {
                using SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
                using SqlCommand cmd = new SqlCommand("SELECT SettingKey, SettingValue FROM MST_SystemSetting", con);
                con.Open();
                using SqlDataReader reader = cmd.ExecuteReader();

                Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);
                while (reader.Read())
                {
                    values[reader["SettingKey"].ToString() ?? string.Empty] = reader["SettingValue"].ToString() ?? string.Empty;
                }

                if (values.Count == 0)
                {
                    return false;
                }

                settings = new SystemSettingsModel
                {
                    PlatformName = values.TryGetValue("PlatformName", out string? platformName) ? platformName : "Meeting Of Minutes",
                    SupportEmail = values.TryGetValue("SupportEmail", out string? supportEmail) ? supportEmail : string.Empty,
                    MaintenanceBanner = values.TryGetValue("MaintenanceBanner", out string? banner) ? banner : string.Empty,
                    AllowAdminSelfRegistration = values.TryGetValue("AllowAdminSelfRegistration", out string? allowValue) && bool.TryParse(allowValue, out bool allow) && allow,
                    SessionTimeoutMinutes = values.TryGetValue("SessionTimeoutMinutes", out string? timeout) && int.TryParse(timeout, out int minutes) ? minutes : 30,
                    SmtpHost = values.TryGetValue("SmtpHost", out string? smtpHost) ? smtpHost : string.Empty,
                    SmtpPort = values.TryGetValue("SmtpPort", out string? smtpPort) && int.TryParse(smtpPort, out int port) ? port : 587,
                    SmtpUsername = values.TryGetValue("SmtpUsername", out string? smtpUsername) ? smtpUsername : string.Empty,
                    SmtpPassword = values.TryGetValue("SmtpPassword", out string? smtpPassword) ? smtpPassword : string.Empty,
                    SmtpFromEmail = values.TryGetValue("SmtpFromEmail", out string? smtpFromEmail) ? smtpFromEmail : string.Empty,
                    SmtpFromName = values.TryGetValue("SmtpFromName", out string? smtpFromName) ? smtpFromName : string.Empty,
                    SmtpUseSsl = !values.TryGetValue("SmtpUseSsl", out string? smtpUseSsl) || !bool.TryParse(smtpUseSsl, out bool useSsl) || useSsl,
                    InviteBaseUrl = values.TryGetValue("InviteBaseUrl", out string? inviteBaseUrl) ? inviteBaseUrl : string.Empty,
                    ModifiedBy = values.TryGetValue("ModifiedBy", out string? modifiedBy) ? modifiedBy : string.Empty
                };

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryWriteToDatabase(SystemSettingsModel model)
        {
            try
            {
                using SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
                con.Open();

                UpsertSetting(con, "PlatformName", model.PlatformName, "General", "Platform title");
                UpsertSetting(con, "SupportEmail", model.SupportEmail, "General", "Support contact email");
                UpsertSetting(con, "MaintenanceBanner", model.MaintenanceBanner, "General", "Optional maintenance banner");
                UpsertSetting(con, "AllowAdminSelfRegistration", model.AllowAdminSelfRegistration.ToString(), "Security", "Allow public admin registration");
                UpsertSetting(con, "SessionTimeoutMinutes", model.SessionTimeoutMinutes.ToString(), "Security", "Session timeout");
                UpsertSetting(con, "SmtpHost", model.SmtpHost, "Email", "SMTP host for invite delivery");
                UpsertSetting(con, "SmtpPort", model.SmtpPort.ToString(), "Email", "SMTP port for invite delivery");
                UpsertSetting(con, "SmtpUsername", model.SmtpUsername, "Email", "SMTP username");
                UpsertSetting(con, "SmtpPassword", model.SmtpPassword, "Email", "SMTP password");
                UpsertSetting(con, "SmtpFromEmail", model.SmtpFromEmail, "Email", "SMTP sender email");
                UpsertSetting(con, "SmtpFromName", model.SmtpFromName, "Email", "SMTP sender name");
                UpsertSetting(con, "SmtpUseSsl", model.SmtpUseSsl.ToString(), "Email", "SMTP SSL flag");
                UpsertSetting(con, "InviteBaseUrl", model.InviteBaseUrl, "Email", "Base URL used for onboarding invite links");
                UpsertSetting(con, "ModifiedBy", model.ModifiedBy, "General", "Last modified by");

                ResetCache();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void UpsertSetting(SqlConnection con, string key, string value, string group, string description)
        {
            using SqlCommand cmd = new SqlCommand(@"
                MERGE MST_SystemSetting AS target
                USING (SELECT @SettingKey AS SettingKey) AS source
                ON target.SettingKey = source.SettingKey
                WHEN MATCHED THEN
                    UPDATE SET SettingValue = @SettingValue, SettingGroup = @SettingGroup, Description = @Description, Modified = GETDATE()
                WHEN NOT MATCHED THEN
                    INSERT (SettingKey, SettingValue, SettingGroup, Description, Modified)
                    VALUES (@SettingKey, @SettingValue, @SettingGroup, @Description, GETDATE());", con);
            cmd.Parameters.AddWithValue("@SettingKey", key);
            cmd.Parameters.AddWithValue("@SettingValue", value ?? string.Empty);
            cmd.Parameters.AddWithValue("@SettingGroup", group);
            cmd.Parameters.AddWithValue("@Description", description);
            cmd.ExecuteNonQuery();
        }

        private static SystemSettingsModel ReadFromJson()
        {
            if (!File.Exists(SettingsFilePath))
            {
                SystemSettingsModel defaults = BuildDefault();
                WriteToJson(defaults);
                return defaults;
            }

            string json = File.ReadAllText(SettingsFilePath);
            return JsonSerializer.Deserialize<SystemSettingsModel>(json) ?? BuildDefault();
        }

        private static void WriteToJson(SystemSettingsModel model)
        {
            string? directory = Path.GetDirectoryName(SettingsFilePath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonSerializer.Serialize(model, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFilePath, json);
        }

        private static SystemSettingsModel BuildDefault()
        {
            return new SystemSettingsModel
            {
                PlatformName = "Meeting Of Minutes",
                SupportEmail = "support@meetingofminutes.local",
                MaintenanceBanner = string.Empty,
                AllowAdminSelfRegistration = false,
                SessionTimeoutMinutes = 30,
                SmtpPort = 587,
                SmtpUseSsl = true
            };
        }

        private static SystemSettingsModel Clone(SystemSettingsModel model)
        {
            return new SystemSettingsModel
            {
                PlatformName = model.PlatformName,
                SupportEmail = model.SupportEmail,
                MaintenanceBanner = model.MaintenanceBanner,
                AllowAdminSelfRegistration = model.AllowAdminSelfRegistration,
                SessionTimeoutMinutes = model.SessionTimeoutMinutes,
                SmtpHost = model.SmtpHost,
                SmtpPort = model.SmtpPort,
                SmtpUsername = model.SmtpUsername,
                SmtpPassword = model.SmtpPassword,
                SmtpFromEmail = model.SmtpFromEmail,
                SmtpFromName = model.SmtpFromName,
                SmtpUseSsl = model.SmtpUseSsl,
                InviteBaseUrl = model.InviteBaseUrl,
                Modified = model.Modified,
                ModifiedBy = model.ModifiedBy
            };
        }
    }
}
