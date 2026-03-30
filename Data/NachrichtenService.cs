using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;

namespace QIN_Production_Web.Data;

public class NachrichtenService
{
    private readonly string _connectionString = SqlManager.connectionString;

    public event Action? OnUnreadCountChanged;

    public void NotifyUnreadCountChanged() => OnUnreadCountChanged?.Invoke();

    public async Task<int> GetTotalUnreadCountAsync(string currentUser)
    {
        if (string.IsNullOrEmpty(currentUser)) return 0;
        const string sql = @"
            SELECT COUNT(ID)
            FROM Nachrichten
            WHERE Empfaenger = @currentUser AND Gelesen = 0;
        ";
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        return await connection.ExecuteScalarAsync<int>(sql, new { currentUser });
    }

    public async Task<IEnumerable<ConversationDto>> GetConversationsAsync(string currentUser)
    {
        const string sql = @"
            SELECT
                ISNULL(u.Personalnummer, 0) AS UserId,
                u.Benutzer       AS DisplayName,
                u.LastSeen       AS LastSeenDate,
                MAX(m.Datum)     AS LastMessageDate,
                (
                    SELECT TOP 1 m2.Nachricht
                    FROM Nachrichten m2
                    WHERE (m2.Benutzer = u.Benutzer AND m2.Empfaenger = @currentUser)
                       OR (m2.Benutzer = @currentUser AND m2.Empfaenger = u.Benutzer)
                    ORDER BY m2.Datum DESC
                ) AS LastMessagePreview,
                (
                    SELECT TOP 1 m2.Benutzer
                    FROM Nachrichten m2
                    WHERE (m2.Benutzer = u.Benutzer AND m2.Empfaenger = @currentUser)
                       OR (m2.Benutzer = @currentUser AND m2.Empfaenger = u.Benutzer)
                    ORDER BY m2.Datum DESC
                ) AS LastMessageSender,
                ISNULL((
                    SELECT COUNT(m3.ID)
                    FROM Nachrichten m3
                    WHERE m3.Benutzer = u.Benutzer AND m3.Empfaenger = @currentUser AND m3.Gelesen = 0
                ), 0) AS UnreadCount,
                CAST(CASE 
                    WHEN u.LastSeen IS NOT NULL 
                         AND DATEDIFF(SECOND, u.LastSeen, SYSDATETIME()) <= 60 THEN 1
                    ELSE 0
                END AS BIT) AS IsOnline
            FROM LoginDaten u
            LEFT JOIN Nachrichten m
                ON (
                    (m.Benutzer = u.Benutzer AND m.Empfaenger = @currentUser)
                    OR (m.Benutzer = @currentUser AND m.Empfaenger = u.Benutzer)
                )
            WHERE u.Benutzer <> @currentUser
            GROUP BY u.Personalnummer, u.Benutzer, u.LastSeen
            ORDER BY LastMessageDate DESC, u.Benutzer;
        ";

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        return await connection.QueryAsync<ConversationDto>(sql, new { currentUser });
    }

    public async Task<IEnumerable<MessageDto>> GetMessagesAsync(string currentUser, string otherUser)
    {
        const string sql = @"
            SELECT ID, Benutzer AS Sender, Nachricht AS Text, Datum, Empfaenger AS Receiver, CAST(ISNULL(Gelesen, 0) AS BIT) AS Gelesen
            FROM Nachrichten
            WHERE (Benutzer = @currentUser AND Empfaenger = @otherUser)
               OR (Benutzer = @otherUser AND Empfaenger = @currentUser)
            ORDER BY Datum ASC;
        ";

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        
        var messages = await connection.QueryAsync<MessageDto>(sql, new { currentUser, otherUser });

        // Update read status for incoming messages from the other user
        const string sqlRead = @"
            UPDATE Nachrichten
            SET Gelesen = 1
            WHERE Empfaenger = @currentUser AND Benutzer = @otherUser AND Gelesen = 0;
        ";
        
        int rowsRead = await connection.ExecuteAsync(sqlRead, new { currentUser, otherUser });
        if (rowsRead > 0)
        {
            NotifyUnreadCountChanged();
        }

        return messages;
    }

    public async Task<bool> SendMessageAsync(string sender, string receiver, string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;

        const string sql = @"
            INSERT INTO Nachrichten (Benutzer, Nachricht, Datum, Empfaenger, Gelesen)
            VALUES (@sender, @text, SYSDATETIME(), @receiver, 0);
        ";

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        int rows = await connection.ExecuteAsync(sql, new { sender = sender.Trim(), text, receiver = receiver.Trim() });
        if (rows > 0)
        {
            NotifyUnreadCountChanged();
        }
        return rows > 0;
    }
}

public class ConversationDto
{
    public int? UserId { get; set; }
    public string? DisplayName { get; set; }
    public DateTime? LastSeenDate { get; set; }
    public DateTime? LastMessageDate { get; set; }
    public string? LastMessagePreview { get; set; }
    public string? LastMessageSender { get; set; }
    public int UnreadCount { get; set; }
    public bool IsOnline { get; set; }
    
    public string Initials => string.IsNullOrWhiteSpace(DisplayName) ? "?" 
                              : DisplayName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length == 1 
                              ? DisplayName[..1].ToUpper() 
                              : $"{DisplayName.Split(' ')[0][0]}{DisplayName.Split(' ')[^1][0]}".ToUpper();

    public string FormattedTime => LastMessageDate.HasValue ? LastMessageDate.Value.ToString("dd.MM.yyyy HH:mm") : "";
}

public class MessageDto
{
    public int ID { get; set; }
    public string? Sender { get; set; }
    public string? Receiver { get; set; }
    public string? Text { get; set; }
    public DateTime? Datum { get; set; }
    public bool Gelesen { get; set; }
    
    public string FormattedTime => Datum.HasValue ? Datum.Value.ToString("dd.MM.yyyy HH:mm") : "";
}
