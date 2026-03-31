using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace QIN_Production_Web.Data
{
    public class SystemAlert
    {
        public int AlertID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? TargetGroup { get; set; }
        public bool IsRead { get; set; }
    }

    public class AlertService
    {
        public async Task<List<SystemAlert>> GetRecentAlertsAsync(string userId)
        {
            var alerts = new List<SystemAlert>();
            try
            {
                using (var connection = new SqlConnection(SqlManager.connectionString))
                {
                    await connection.OpenAsync();
                    // Lade die letzten Ankündigungen und checke UserAlertStatus
                    string query = @"
                        SELECT TOP 10 A.AlertID, A.Title, A.Message, A.CreatedAt, A.CreatedBy, A.TargetGroup,
                               ISNULL(UAS.IsRead, 0) AS IsRead
                        FROM Alerts A
                        LEFT JOIN UserAlertStatus UAS ON A.AlertID = UAS.AlertID AND UAS.UserID = @UserId
                        ORDER BY A.CreatedAt DESC";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@UserId", userId);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                alerts.Add(new SystemAlert
                                {
                                    AlertID = reader["AlertID"] != DBNull.Value ? Convert.ToInt32(reader["AlertID"]) : 0,
                                    Title = reader["Title"]?.ToString() ?? "",
                                    Message = reader["Message"]?.ToString() ?? "",
                                    CreatedAt = reader["CreatedAt"] != DBNull.Value ? Convert.ToDateTime(reader["CreatedAt"]) : DateTime.MinValue,
                                    CreatedBy = reader["CreatedBy"]?.ToString(),
                                    TargetGroup = reader["TargetGroup"]?.ToString(),
                                    IsRead = reader["IsRead"] != DBNull.Value && Convert.ToBoolean(reader["IsRead"])
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetAlerts Error: " + ex.Message);
            }
            return alerts;
        }

        public async Task MarkAlertAsReadAsync(int alertId, string userId)
        {
            try
            {
                using (var connection = new SqlConnection(SqlManager.connectionString))
                {
                    await connection.OpenAsync();
                    
                    string query = @"
                        IF EXISTS (SELECT 1 FROM UserAlertStatus WHERE AlertID = @AlertID AND UserID = @UserID)
                        BEGIN
                            UPDATE UserAlertStatus 
                            SET IsRead = 1, ReadAt = SYSDATETIME() 
                            WHERE AlertID = @AlertID AND UserID = @UserID
                        END
                        ELSE
                        BEGIN
                            INSERT INTO UserAlertStatus (AlertID, UserID, IsRead, ReadAt)
                            VALUES (@AlertID, @UserID, 1, SYSDATETIME())
                        END";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@AlertID", alertId);
                        command.Parameters.AddWithValue("@UserID", userId);
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("MarkAlertAsRead Error: " + ex.Message);
            }
        }

        public async Task CreateAlertAsync(SystemAlert alert)
        {
            try
            {
                using (var connection = new SqlConnection(SqlManager.connectionString))
                {
                    await connection.OpenAsync();
                    // Füge neue Benachrichtigung in dbo.Alerts ein
                    string query = @"
                        INSERT INTO Alerts (Title, Message, CreatedAt, CreatedBy, TargetGroup)
                        VALUES (@Title, @Message, SYSDATETIME(), @CreatedBy, @TargetGroup)";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Title", string.IsNullOrWhiteSpace(alert.Title) ? DBNull.Value : alert.Title);
                        command.Parameters.AddWithValue("@Message", string.IsNullOrWhiteSpace(alert.Message) ? DBNull.Value : alert.Message);
                        command.Parameters.AddWithValue("@CreatedBy", string.IsNullOrWhiteSpace(alert.CreatedBy) ? DBNull.Value : alert.CreatedBy);
                        command.Parameters.AddWithValue("@TargetGroup", string.IsNullOrWhiteSpace(alert.TargetGroup) ? DBNull.Value : alert.TargetGroup);

                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("CreateAlert Error: " + ex.Message);
            }
        }
    }
}
