using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;

namespace QIN_Production_Web.Data;

public class VerwaltungWareneingangService
{
    private readonly string _connectionString = SqlManager.FertigungConnectionString;

    // --- Wareneingang CRUD ---

    public async Task<IEnumerable<WareneingangEntry>> GetWareneingangsAsync(string statusFilter, DateTime? datumVon = null, DateTime? datumBis = null)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        string query = @"
            SELECT ID, Lieferant, LS_Nr, EBE_NR, Pos, Artikel, Zustand, Menge, Bemerkung, Benutzer, Eingangsdatum, Palettentausch, Gebucht, Dickenmessung,
                   (SELECT COUNT(*) FROM Chargen c WHERE c.Wareneingang_ID = w.ID) AS Chargen
            FROM Wareneingang w";

        var whereParts = new List<string>();

        if (statusFilter == "Gebucht")
            whereParts.Add("w.Gebucht = 1");
        else if (statusFilter == "Nicht Gebucht")
            whereParts.Add("(w.Gebucht = 0 OR w.Gebucht IS NULL)");

        if (datumVon.HasValue)
            whereParts.Add("CAST(w.Eingangsdatum AS date) >= @DatumVon");

        if (datumBis.HasValue)
            whereParts.Add("CAST(w.Eingangsdatum AS date) <= @DatumBis");

        if (whereParts.Count > 0)
            query += " WHERE " + string.Join(" AND ", whereParts);

        query += " ORDER BY w.ID DESC";

        return await connection.QueryAsync<WareneingangEntry>(query, new
        {
            DatumVon = datumVon?.Date,
            DatumBis = datumBis?.Date
        });
    }

    public async Task<bool> UpdateWareneingangAsync(WareneingangEntry row, string userName = "Unbekannt")
    {
        string query = @"UPDATE Wareneingang
            SET Lieferant = @Lieferant,
                LS_Nr = @LS_Nr,
                EBE_Nr = @EBE_NR,
                Zustand = @Zustand,
                Pos = @Pos,
                Menge = @Menge,
                Eingangsdatum = @Eingangsdatum,
                Benutzer = @Benutzer,
                Bemerkung = @Bemerkung,
                Gebucht = @Gebucht,
                Palettentausch = @Palettentausch,
                Artikel = @Artikel,
                Dickenmessung = @Dickenmessung
            WHERE ID = @ID";

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        
        int rows = await connection.ExecuteAsync(query, row);
        if (rows > 0) await ActivityLogService.InsertLogAsync(userName, $"[WE-Verwaltung] Wareneingang ID {row.ID} aktualisiert (Material: {row.Artikel ?? "Unbekannt"}).");
        return rows > 0;
    }

    public async Task<bool> DeleteWareneingangAsync(int id, string userName = "Unbekannt")
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        
        // In SQL Server, deleting Wareneingang might leave orphaned Chargen if there's no ON DELETE CASCADE.
        // We will explicitly delete attached Chargen first as a safety measure.
        await connection.ExecuteAsync("DELETE FROM Chargen WHERE Wareneingang_ID = @ID", new { ID = id });
        
        string query = "DELETE FROM Wareneingang WHERE ID = @ID";
        int rows = await connection.ExecuteAsync(query, new { ID = id });
        if (rows > 0) await ActivityLogService.InsertLogAsync(userName, $"[WE-Verwaltung] Wareneingang ID {id} (inkl. eventueller Chargen) gelöscht.");
        return rows > 0;
    }


    // --- Chargen CRUD ---

    public async Task<IEnumerable<ChargeEntry>> GetChargenForWareneingangAsync(int wareneingangId)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        string query = "SELECT ID, Charge, Echte_Menge, Aktuelle_Menge, Wareneingang_ID FROM Chargen WHERE Wareneingang_ID = @ID ORDER BY ID";
        return await connection.QueryAsync<ChargeEntry>(query, new { ID = wareneingangId });
    }

    public async Task<bool> UpdateChargeAsync(ChargeEntry charge, string userName = "Unbekannt")
    {
        string query = @"UPDATE Chargen
                         SET Charge = @Charge,
                             Echte_Menge = @Echte_Menge
                         WHERE ID = @ID";

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        int rows = await connection.ExecuteAsync(query, charge);
        if (rows > 0) await ActivityLogService.InsertLogAsync(userName, $"[WE-Verwaltung] Charge ID {charge.ID} / {charge.Charge} (Menge: {charge.Echte_Menge}) aktualisiert.");
        return rows > 0;
    }

    public async Task<bool> DeleteChargeAsync(int id, string userName = "Unbekannt")
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        string query = "DELETE FROM Chargen WHERE ID = @ID";
        int rows = await connection.ExecuteAsync(query, new { ID = id });
        if (rows > 0) await ActivityLogService.InsertLogAsync(userName, $"[WE-Verwaltung] Charge ID {id} gelöscht.");
        return rows > 0;
    }

    public async Task<bool> AddChargeAsync(ChargeEntry charge, string userName = "Unbekannt")
    {
        string query = @"INSERT INTO Chargen (Wareneingang_ID, Charge, Echte_Menge)
                         VALUES (@Wareneingang_ID, @Charge, @Echte_Menge)";

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        int rows = await connection.ExecuteAsync(query, charge);
        if (rows > 0) await ActivityLogService.InsertLogAsync(userName, $"[WE-Verwaltung] Neue Charge {charge.Charge} zur WE-ID {charge.Wareneingang_ID} (Menge: {charge.Echte_Menge}) hinzugefügt.");
        return rows > 0;
    }
}

public class ChargeEntry
{
    public int ID { get; set; }
    public string? Charge { get; set; }
    public int? Echte_Menge { get; set; }
    public int? Aktuelle_Menge { get; set; }
    public int Wareneingang_ID { get; set; }
    public int MengeAnzeige => Echte_Menge ?? Aktuelle_Menge ?? 0;
}
