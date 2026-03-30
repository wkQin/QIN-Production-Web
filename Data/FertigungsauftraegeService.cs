using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;

namespace QIN_Production_Web.Data;

public class FertigungsauftraegeService
{
    private readonly string _connectionString = SqlManager.FertigungConnectionString;

    public async Task<IEnumerable<IDictionary<string, object>>> GetAuftraegeAsync(string tableType, string? filterStatus)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        string tableName = GetTableName(tableType);
        if (string.IsNullOrEmpty(tableName)) return Enumerable.Empty<IDictionary<string, object>>();

        string query = "";
        if (tableType == "UV Anweisungen")
        {
            query = "SELECT TOP 1000 ID, [FA_Nr], Erstellungsdatum, Material, Anweisung, Abgesendet FROM dbo.Thermo_Auftrag WHERE Material LIKE '%Rollo%'";
        }
        else
        {
            query = $"SELECT TOP 1000 * FROM {tableName} WHERE 1=1";
        }

        var parameters = new DynamicParameters();

        // Status Filter
        if (filterStatus == "Abgesendet = JA")
        {
            query += " AND Abgesendet = 1";
        }
        else if (filterStatus == "Abgesendet = NEIN")
        {
            query += " AND Abgesendet = 0";
        }

        query += " ORDER BY ID DESC";

        var result = await connection.QueryAsync<dynamic>(query, parameters);
        // Cast DapperDatarow to dictionary so we can easily iterate in Blazor templates
        return result.Select(x => (IDictionary<string, object>)x).ToList();
    }

    public async Task<bool> UpdateAuftragAsync(string tableType, int id, IDictionary<string, object> updatedData, string userName = "Unbekannt")
    {
        string tableName = GetTableName(tableType);
        if (string.IsNullOrEmpty(tableName)) return false;

        var setClauses = new List<string>();
        var parameters = new DynamicParameters();
        parameters.Add("@ID", id);

        foreach (var kvp in updatedData)
        {
            // Skip non-updatable keys
            if (kvp.Key.Equals("ID", StringComparison.OrdinalIgnoreCase) ||
                kvp.Key.Equals("Erstellungsdatum", StringComparison.OrdinalIgnoreCase) ||
                kvp.Key.Equals("Erstelldatum", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            setClauses.Add($"[{kvp.Key}] = @{kvp.Key}");
            
            // Dapper doesn't like System.DBNull as a parameter value, it prefers C# null
            object? val = kvp.Value;
            if (val is DBNull || string.Empty.Equals(val)) 
            {
                val = null;
            }
            parameters.Add($"@{kvp.Key}", val);
        }

        if (!setClauses.Any()) return true; // nothing to update

        string query = $"UPDATE {tableName} SET {string.Join(", ", setClauses)} WHERE ID = @ID";

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        
        int rows = await connection.ExecuteAsync(query, parameters);
        
        if (rows > 0)
        {
            var keys = string.Join(", ", setClauses.Select(s => s.Split('=')[0].Trim('[', ']', ' ')));
            await ActivityLogService.InsertLogAsync(userName, $"[Fertigungsaufträge] Auftrag ID {id} in {tableType} aktualisiert. Betroffene Felder: {keys}");
        }
        
        return rows > 0;
    }

    public async Task<bool> DeleteAuftragAsync(string tableType, int id, string userName = "Unbekannt")
    {
        string tableName = GetTableName(tableType);
        if (string.IsNullOrEmpty(tableName)) return false;

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        string query = $"DELETE FROM {tableName} WHERE ID = @ID";
        int rows = await connection.ExecuteAsync(query, new { ID = id });
        if (rows > 0) await ActivityLogService.InsertLogAsync(userName, $"[Fertigungsaufträge] Auftrag ID {id} aus {tableType} gelöscht.");
        return rows > 0;
    }

    private string GetTableName(string type) => type switch
    {
        "Thermoformung" => "dbo.Thermo_Auftrag",
        "Stanzen" => "dbo.Stanzen_Auftrag",
        "UV" => "dbo.UV_Auftrag",
        "UV Anweisungen" => "dbo.Thermo_Auftrag", // UV Anweisungen are stored in Thermo_Auftrag
        _ => ""
    };
}
