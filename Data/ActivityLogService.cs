using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;

namespace QIN_Production_Web.Data
{
    public class ActivityLogDto
    {
        public int ID { get; set; }
        public string Benutzer { get; set; } = string.Empty;
        public string Prozess { get; set; } = string.Empty;
        public DateTime Datum { get; set; }
    }

    public class ActivityLogService
    {
        public static async Task<IEnumerable<ActivityLogDto>> GetLogsAsync(int limit = 500)
        {
            const string sql = @"
                SELECT TOP (@limit) ID, Benutzer, Prozess, Datum 
                FROM Logs 
                ORDER BY Datum DESC";

            using var connection = new SqlConnection(SqlManager.connectionString);
            return await connection.QueryAsync<ActivityLogDto>(sql, new { limit });
        }

        public static async Task<string> GetUserNameByPersonalnummerAsync(string personalnummer)
        {
            if (string.IsNullOrWhiteSpace(personalnummer)) return "Unbekannt";
            const string sql = "SELECT TOP 1 Benutzer FROM LoginDaten WHERE Personalnummer = @pn";
            try
            {
                using var connection = new SqlConnection(SqlManager.connectionString);
                var result = await connection.ExecuteScalarAsync<string>(sql, new { pn = personalnummer });
                return !string.IsNullOrWhiteSpace(result) ? result.Trim() : "Unbekannt";
            }
            catch { return "Unbekannt"; }
        }

        public static async Task<bool> InsertLogAsync(string benutzer, string prozessText)
        {
            if (string.IsNullOrWhiteSpace(benutzer) || string.IsNullOrWhiteSpace(prozessText))
                return false;

            const string sql = @"
                INSERT INTO Logs (Benutzer, Prozess, Datum)
                VALUES (@benutzer, @prozess, SYSDATETIME());";

            try
            {
                using var connection = new SqlConnection(SqlManager.connectionString);
                int rows = await connection.ExecuteAsync(sql, new { benutzer = benutzer.Trim(), prozess = prozessText.Trim() });
                return rows > 0;
            }
            catch (Exception)
            {
                // Graceful fallback: Never break the main application if logging fails
                return false;
            }
        }
    }
}
