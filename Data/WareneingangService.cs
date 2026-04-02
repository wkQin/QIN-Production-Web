using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace QIN_Production_Web.Data
{
    public class WareneingangEntry
    {
        public int ID { get; set; }
        public string? Lieferant { get; set; }
        public string? EBE_NR { get; set; }
        public string? LS_Nr { get; set; }
        public string? Pos { get; set; }
        public string? Menge { get; set; }
        public string? Artikel { get; set; }
        public string? Bemerkung { get; set; }
        public string? Zustand { get; set; }
        public int Chargen { get; set; }
        public int? Benutzer { get; set; }
        public string? Eingangsdatum { get; set; }
        public bool? Palettentausch { get; set; }
        public bool? Gebucht { get; set; }
    }

    public class ChargenEntry
    {
        public string Charge { get; set; } = string.Empty;
        public string Menge { get; set; } = string.Empty;
        public int Scanner { get; set; }
        public int IsNew01 { get; set; }
    }

    public class WareneingangService
    {
        public static async Task<List<string>> GetLieferantenAsync()
        {
            var lieferanten = new List<string>();
            string query = "SELECT Lieferant FROM Lieferanten";
            using (SqlConnection connection = new SqlConnection(SqlManager.connectionString))
            {
                try
                {
                    await connection.OpenAsync();
                    using (SqlCommand command = new SqlCommand(query, connection))
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string? lieferant = reader["Lieferant"]?.ToString();
                            if (!string.IsNullOrWhiteSpace(lieferant)) lieferanten.Add(lieferant);
                        }
                    }
                }
                catch (Exception ex) { Console.WriteLine(ex.Message); }
            }
            return lieferanten;
        }

        public static async Task<List<WareneingangEntry>> LoadWareneingangAsync(string? lieferant = null)
        {
            var result = new List<WareneingangEntry>();
            try
            {
                string query = "SELECT w.ID, w.Lieferant, w.LS_Nr, w.EBE_NR, w.Pos, w.Artikel, w.Zustand, w.Menge, w.Bemerkung, (SELECT COUNT(*) FROM Chargen c WHERE c.Wareneingang_ID = w.ID) AS ChargenCount FROM Wareneingang w WHERE w.Gebucht = 0" + (lieferant != null ? " AND w.Lieferant = @Lieferant" : "") + " ORDER BY w.ID DESC;";
                using (SqlConnection connection = new SqlConnection(SqlManager.FertigungConnectionString))
                {
                    await connection.OpenAsync();
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        if (lieferant != null) command.Parameters.AddWithValue("@Lieferant", lieferant);
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                result.Add(new WareneingangEntry
                                {
                                    ID = reader.GetInt32(reader.GetOrdinal("ID")),
                                    Lieferant = reader["Lieferant"]?.ToString(),
                                    EBE_NR = reader["EBE_NR"]?.ToString(),
                                    LS_Nr = reader["LS_Nr"]?.ToString(),
                                    Pos = reader["Pos"]?.ToString(),
                                    Menge = reader["Menge"]?.ToString(),
                                    Artikel = reader["Artikel"]?.ToString(),
                                    Bemerkung = reader["Bemerkung"]?.ToString(),
                                    Zustand = reader["Zustand"]?.ToString(),
                                    Chargen = reader["ChargenCount"] != DBNull.Value ? Convert.ToInt32(reader["ChargenCount"]) : 0
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
            return result;
        }

        public static async Task<List<string>> FindAllMaterialsAsync(string lieferant)
        {
            var materials = new List<string>();
            string query = @"SELECT Beschreibung FROM Artikelliste WHERE Nr IN (SELECT MaterialNr FROM Materialliste WHERE Lieferant = @Lieferant);";
            try
            {
                using (SqlConnection connection = new SqlConnection(SqlManager.connectionString))
                {
                    await connection.OpenAsync();
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Lieferant", lieferant);
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                if (reader["Beschreibung"] != DBNull.Value) materials.Add(reader["Beschreibung"].ToString()!);
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
            return materials;
        }

        public static async Task<List<ChargenEntry>> FindChargenAsync(int wareneingangsId)
        {
            var chargen = new List<ChargenEntry>();
            using (SqlConnection connection = new SqlConnection(SqlManager.FertigungConnectionString))
            {
                await connection.OpenAsync();
                string query = "SELECT * FROM Chargen WHERE Wareneingang_ID = @Wareneingangs_id";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Wareneingangs_id", wareneingangsId);
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            chargen.Add(new ChargenEntry
                            {
                                Charge = reader["Charge"]?.ToString() ?? "",
                                Menge = reader["Aktuelle_Menge"]?.ToString() ?? "0",
                                Scanner = reader["Kontrolle"] != DBNull.Value ? Convert.ToInt32(reader["Kontrolle"]) : 0,
                                IsNew01 = 0
                            });
                        }
                    }
                }
            }
            return chargen;
        }

        public static async Task<bool> InsertWareneingangAsync(string? id, string? lieferant, string? lsNr, string? pos, List<ChargenEntry> chargenList, string? zustand, string? liefermenge, bool? palettentausch, string? bemerkung, UserSession session, bool eintragBearbeiten, string? ebe, string? material)
        {
            string query = eintragBearbeiten ? 
                @"UPDATE Wareneingang SET Lieferant=@Lieferant, LS_Nr=@LSNr, Pos=@Pos, Zustand=@Zustand, Palettentausch=@Palettentausch, Bemerkung=@Bemerkung, Artikel=@Artikel, Eingangsdatum=@Eingangsdatum, Benutzer=@Benutzer, EBE_Nr=@EBE WHERE ID=@ID" :
                @"INSERT INTO Wareneingang (Lieferant, LS_Nr, Pos, Zustand, Palettentausch, Artikel, Eingangsdatum, Benutzer, Bemerkung, EBE_Nr) VALUES (@Lieferant, @LSNr, @Pos, @Zustand, @Palettentausch, @Artikel, @Eingangsdatum, @Benutzer, @Bemerkung, @EBE); SELECT SCOPE_IDENTITY();";

            try
            {
                using (SqlConnection connection = new SqlConnection(SqlManager.FertigungConnectionString))
                {
                    await connection.OpenAsync();
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Lieferant", (object?)lieferant ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Zustand", (object?)zustand ?? DBNull.Value);
                        command.Parameters.AddWithValue("@LSNr", (object?)lsNr ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Pos", (object?)pos ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Palettentausch", (palettentausch ?? false) ? 1 : 0);
                        command.Parameters.AddWithValue("@Artikel", (object?)material ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Eingangsdatum", DateTime.Now);
                        command.Parameters.AddWithValue("@Benutzer", session.Personalnummer ?? "100");
                        command.Parameters.AddWithValue("@Bemerkung", (object?)bemerkung ?? DBNull.Value);
                        command.Parameters.AddWithValue("@EBE", (object?)ebe ?? DBNull.Value);
                        
                        if (eintragBearbeiten && int.TryParse(id, out int idInt)) command.Parameters.AddWithValue("@ID", idInt);

                        int activeId = 0;
                        if (eintragBearbeiten) { await command.ExecuteNonQueryAsync(); activeId = int.Parse(id!); }
                        else { activeId = Convert.ToInt32(await command.ExecuteScalarAsync()); }

                        if (activeId > 0 && chargenList != null) await InsertChargenAsync(activeId, chargenList, connection, liefermenge ?? "0");

                        string actionText = eintragBearbeiten 
                            ? $"[Wareneingang] Eintrag ID {activeId} aktualisiert (Material: {material ?? "Unbekannt"})"
                            : $"[Wareneingang] Neuer Eintrag ID {activeId} erstellt (Lieferant: {lieferant ?? "Unbekannt"}, Material: {material ?? "Unbekannt"})";
                        await ActivityLogService.InsertLogAsync(session.Name ?? "Unbekannt", actionText);
                    }
                }
                return true;
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); return false; }
        }

        private static async Task InsertChargenAsync(int wareneingangId, List<ChargenEntry> chargenList, SqlConnection connection, string liefermenge)
        {
            string chargenQuery = @"INSERT INTO Chargen (Wareneingang_ID, Charge, Aktuelle_Menge, Kontrolle, Einheit, Echte_Menge, Liefermenge, Status_ID) VALUES (@WareneingangID, @ChargenNr, @Menge, @Kontrolle, @Einheit, @Echte_Menge, @Liefermenge, 2)";
            using (SqlCommand cmd = new SqlCommand(chargenQuery, connection))
            {
                cmd.Parameters.AddWithValue("@WareneingangID", wareneingangId);
                cmd.Parameters.AddWithValue("@Einheit", "LM");
                cmd.Parameters.Add("@ChargenNr", SqlDbType.NVarChar);
                cmd.Parameters.Add("@Menge", SqlDbType.Int);
                cmd.Parameters.Add("@Echte_Menge", SqlDbType.Int);
                cmd.Parameters.Add("@Liefermenge", SqlDbType.Int);
                cmd.Parameters.Add("@Kontrolle", SqlDbType.Int);

                foreach (var row in chargenList)
                {
                    if (row.IsNew01 == 1)
                    {
                        cmd.Parameters["@Liefermenge"].Value = int.TryParse(liefermenge, out int lm) ? lm : 0;
                        cmd.Parameters["@ChargenNr"].Value = row.Charge;
                        cmd.Parameters["@Menge"].Value = int.TryParse(row.Menge, out int mg) ? mg : 0;
                        cmd.Parameters["@Echte_Menge"].Value = int.TryParse(row.Menge, out int eg) ? eg : 0;
                        cmd.Parameters["@Kontrolle"].Value = row.Scanner;
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
        }


        public static async Task<string> GetEingangsDatumForChargeAsync(string charge)
        {
            string datum = DateTime.Now.ToString("dd.MM.yyyy");
            string query = @"SELECT w.Eingangsdatum FROM Wareneingang w JOIN Chargen c ON w.ID = c.Wareneingang_ID WHERE c.Charge = @charge";
            try 
            {
                using (SqlConnection connection = new SqlConnection(SqlManager.FertigungConnectionString))
                {
                    await connection.OpenAsync();
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@charge", charge);
                        var result = await command.ExecuteScalarAsync();
                        if (result != null && result != DBNull.Value && DateTime.TryParse(result.ToString(), out DateTime parsedDate))
                        {
                            datum = parsedDate.ToString("dd.MM.yyyy");
                        }
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
            return datum;
        }
    }
}
