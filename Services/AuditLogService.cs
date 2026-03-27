using Meeting_Of_Minutes.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Meeting_Of_Minutes.Services
{
    public static class AuditLogService
    {
        public static void Log(string? companyName, int? userId, string? userName, string? userRole, string actionType, string entityName, string? entityId, string title, string description)
        {
            using SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            using SqlCommand cmd = new SqlCommand("PR_MST_AuditLog_Insert", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@CompanyName", string.IsNullOrWhiteSpace(companyName) ? DBNull.Value : companyName);
            cmd.Parameters.AddWithValue("@UserID", userId.HasValue ? userId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@UserName", string.IsNullOrWhiteSpace(userName) ? DBNull.Value : userName);
            cmd.Parameters.AddWithValue("@UserRole", string.IsNullOrWhiteSpace(userRole) ? DBNull.Value : userRole);
            cmd.Parameters.AddWithValue("@ActionType", actionType);
            cmd.Parameters.AddWithValue("@EntityName", entityName);
            cmd.Parameters.AddWithValue("@EntityID", string.IsNullOrWhiteSpace(entityId) ? DBNull.Value : entityId);
            cmd.Parameters.AddWithValue("@Title", title);
            cmd.Parameters.AddWithValue("@Description", description);
            cmd.Parameters.AddWithValue("@Modified", DateTime.Now);
            con.Open();
            cmd.ExecuteNonQuery();
        }

        public static List<AuditLogModel> GetRecentByCompany(string companyName, int count = 8)
        {
            List<AuditLogModel> logs = new List<AuditLogModel>();
            using SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            using SqlCommand cmd = new SqlCommand("PR_MST_AuditLog_SelectByCompany", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@CompanyName", companyName);
            cmd.Parameters.AddWithValue("@TopCount", count);
            con.Open();
            using SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                logs.Add(ReadAuditLog(reader));
            }
            return logs;
        }

        public static List<AuditLogModel> GetRecentByUser(int userId, int count = 8)
        {
            List<AuditLogModel> logs = new List<AuditLogModel>();
            using SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            using SqlCommand cmd = new SqlCommand("PR_MST_AuditLog_SelectByUserID", con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@UserID", userId);
            cmd.Parameters.AddWithValue("@TopCount", count);
            con.Open();
            using SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                logs.Add(ReadAuditLog(reader));
            }
            return logs;
        }

        public static List<AuditLogModel> GetRecentGlobal(int count = 8)
        {
            List<AuditLogModel> logs = new List<AuditLogModel>();
            using SqlConnection con = new SqlConnection(Meeting_Of_Minutes.DbConnectionHelper.ConnectionString);
            using SqlCommand cmd = new SqlCommand(@"
                SELECT TOP (@TopCount)
                       AuditLogID, CompanyName, UserID, UserName, UserRole, ActionType, EntityName, EntityID, Title, Description, Created
                FROM MST_AuditLog
                ORDER BY Created DESC", con);
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.AddWithValue("@TopCount", count);
            con.Open();
            using SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                logs.Add(ReadAuditLog(reader));
            }
            return logs;
        }

        private static AuditLogModel ReadAuditLog(SqlDataReader reader)
        {
            AuditLogModel log = new AuditLogModel();
            log.AuditLogID = Convert.ToInt32(reader["AuditLogID"]);
            log.CompanyName = reader["CompanyName"].ToString() ?? string.Empty;
            log.UserID = reader["UserID"] == DBNull.Value ? null : Convert.ToInt32(reader["UserID"]);
            log.UserName = reader["UserName"].ToString() ?? string.Empty;
            log.UserRole = reader["UserRole"].ToString() ?? string.Empty;
            log.ActionType = reader["ActionType"].ToString() ?? string.Empty;
            log.EntityName = reader["EntityName"].ToString() ?? string.Empty;
            log.EntityID = reader["EntityID"].ToString() ?? string.Empty;
            log.Title = reader["Title"].ToString() ?? string.Empty;
            log.Description = reader["Description"].ToString() ?? string.Empty;
            log.Created = Convert.ToDateTime(reader["Created"]);
            return log;
        }
    }
}
