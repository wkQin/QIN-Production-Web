using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
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
        public string? Dickenmessung { get; set; }
    }

    public class ChargenEntry
    {
        public string Charge { get; set; } = string.Empty;
        public string Menge { get; set; } = string.Empty;
        public int Scanner { get; set; }
        public int IsNew01 { get; set; }
    }

    public class MaterialDickenmessungInfo
    {
        public decimal Dickenmessung { get; set; }
        public string? Suchbegriff { get; set; }
        public string? Nr { get; set; }
        public string? Beschreibung { get; set; }
        public string? Beschreibung2 { get; set; }
        public string? Lieferant { get; set; }
        public string? MatchFeld { get; set; }
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

        public static async Task<bool> IsLieferantAutomotivAsync(string? lieferant)
        {
            if (string.IsNullOrWhiteSpace(lieferant))
            {
                return true;
            }

            const string query = "SELECT TOP 1 Automotiv FROM Lieferanten WHERE Lieferant = @Lieferant";

            using (SqlConnection connection = new SqlConnection(SqlManager.connectionString))
            {
                try
                {
                    await connection.OpenAsync();
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Lieferant", lieferant.Trim());
                        var result = await command.ExecuteScalarAsync();

                        if (result == null || result == DBNull.Value)
                        {
                            return true;
                        }

                        return Convert.ToBoolean(result);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return true;
                }
            }
        }

        public static async Task<List<WareneingangEntry>> LoadWareneingangAsync(string? lieferant = null)
        {
            var result = new List<WareneingangEntry>();
            try
            {
                string query = "SELECT w.ID, w.Lieferant, w.LS_Nr, w.EBE_NR, w.Pos, w.Artikel, w.Zustand, w.Menge, w.Bemerkung, w.Dickenmessung, (SELECT COUNT(*) FROM Chargen c WHERE c.Wareneingang_ID = w.ID) AS ChargenCount FROM Wareneingang w WHERE w.Gebucht = 0" + (lieferant != null ? " AND w.Lieferant = @Lieferant" : "") + " ORDER BY w.ID DESC;";
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
                                    Dickenmessung = reader["Dickenmessung"]?.ToString(),
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

        public static async Task<MaterialDickenmessungInfo?> FindMaterialDickenmessungAsync(string? materialText, string? lieferant)
        {
            if (string.IsNullOrWhiteSpace(materialText))
            {
                return null;
            }

            const string query = @"
                SELECT Suchbegriff, Nr, Beschreibung, Beschreibung2, Lieferant, Dickenmessung
                FROM dbo.Materialliste
                WHERE Dickenmessung IS NOT NULL;";

            var matches = new List<(MaterialDickenmessungInfo Info, int Score)>();

            try
            {
                using (SqlConnection connection = new SqlConnection(SqlManager.connectionString))
                {
                    await connection.OpenAsync();
                    using (SqlCommand command = new SqlCommand(query, connection))
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var info = new MaterialDickenmessungInfo
                            {
                                Suchbegriff = reader["Suchbegriff"]?.ToString(),
                                Nr = reader["Nr"]?.ToString(),
                                Beschreibung = reader["Beschreibung"]?.ToString(),
                                Beschreibung2 = reader["Beschreibung2"]?.ToString(),
                                Lieferant = reader["Lieferant"]?.ToString(),
                                Dickenmessung = Convert.ToDecimal(reader["Dickenmessung"], CultureInfo.InvariantCulture)
                            };

                            var bestField = string.Empty;
                            var bestScore = 0;

                            foreach (var candidate in new[]
                            {
                                ("Suchbegriff", info.Suchbegriff),
                                ("Beschreibung", info.Beschreibung),
                                ("Beschreibung2", info.Beschreibung2)
                            })
                            {
                                var score = ScoreMaterialMatch(materialText, candidate.Item2);
                                if (score > bestScore)
                                {
                                    bestScore = score;
                                    bestField = candidate.Item1;
                                }
                            }

                            if (!string.IsNullOrWhiteSpace(lieferant)
                                && string.Equals(info.Lieferant?.Trim(), lieferant.Trim(), StringComparison.OrdinalIgnoreCase))
                            {
                                bestScore += 25;
                            }

                            if (bestScore >= 100)
                            {
                                info.MatchFeld = bestField;
                                matches.Add((info, bestScore));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }

            return matches
                .OrderByDescending(x => x.Score)
                .Select(x => x.Info)
                .FirstOrDefault();
        }

        private static int ScoreMaterialMatch(string input, string? candidate)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                return 0;
            }

            var normalizedInput = NormalizeMaterialText(input);
            var normalizedCandidate = NormalizeMaterialText(candidate);

            if (string.IsNullOrWhiteSpace(normalizedInput) || string.IsNullOrWhiteSpace(normalizedCandidate))
            {
                return 0;
            }

            if (normalizedInput == normalizedCandidate)
            {
                return 1000 + normalizedCandidate.Length;
            }

            if (normalizedInput.Contains(normalizedCandidate, StringComparison.OrdinalIgnoreCase))
            {
                return 800 + normalizedCandidate.Length;
            }

            if (normalizedCandidate.Contains(normalizedInput, StringComparison.OrdinalIgnoreCase))
            {
                return 750 + normalizedInput.Length;
            }

            var inputTokens = GetMaterialTokens(input);
            var candidateTokens = GetMaterialTokens(candidate);
            var commonTokenCount = inputTokens.Intersect(candidateTokens, StringComparer.OrdinalIgnoreCase).Count();

            return commonTokenCount >= 2 ? 50 * commonTokenCount : 0;
        }

        private static string NormalizeMaterialText(string value)
        {
            return string.Concat(Regex.Matches(value.ToLowerInvariant().Replace('µ', 'u'), @"[\p{L}\p{N}]+")
                .Select(match => match.Value)
                .Where(token => token.Length > 1));
        }

        private static HashSet<string> GetMaterialTokens(string value)
        {
            return Regex.Matches(value.ToLowerInvariant().Replace('µ', 'u'), @"[\p{L}\p{N}]+")
                .Select(match => match.Value)
                .Where(token => token.Length > 1)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
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

        public static async Task<bool> InsertWareneingangAsync(string? id, string? lieferant, string? lsNr, string? pos, List<ChargenEntry> chargenList, string? zustand, string? liefermenge, bool? palettentausch, string? bemerkung, UserSession session, bool eintragBearbeiten, string? ebe, string? material, string? dickenmessung)
        {
            var result = await SaveWareneingangCoreAsync(id, lieferant, lsNr, pos, chargenList, zustand, liefermenge, palettentausch, bemerkung, session, eintragBearbeiten, ebe, material, dickenmessung);
            return result.Success;
        }

        public static async Task<bool> InsertWareneingangAndSperreChargenAsync(string? id, string? lieferant, string? lsNr, string? pos, List<ChargenEntry> chargenList, string? zustand, string? liefermenge, bool? palettentausch, string? bemerkung, UserSession session, bool eintragBearbeiten, string? ebe, string? material, string? dickenmessung, string sperrgrund)
        {
            var result = await SaveWareneingangCoreAsync(id, lieferant, lsNr, pos, chargenList, zustand, liefermenge, palettentausch, bemerkung, session, eintragBearbeiten, ebe, material, dickenmessung);
            if (!result.Success || result.ActiveId <= 0)
            {
                return false;
            }

            return await SperreChargenFuerWareneingangAsync(result.ActiveId, session, sperrgrund);
        }

        private static async Task<(bool Success, int ActiveId)> SaveWareneingangCoreAsync(string? id, string? lieferant, string? lsNr, string? pos, List<ChargenEntry> chargenList, string? zustand, string? liefermenge, bool? palettentausch, string? bemerkung, UserSession session, bool eintragBearbeiten, string? ebe, string? material, string? dickenmessung)
        {
            string query = eintragBearbeiten ? 
                @"UPDATE Wareneingang SET Lieferant=@Lieferant, LS_Nr=@LSNr, Pos=@Pos, Zustand=@Zustand, Palettentausch=@Palettentausch, Bemerkung=@Bemerkung, Artikel=@Artikel, Eingangsdatum=@Eingangsdatum, Benutzer=@Benutzer, EBE_Nr=@EBE, Dickenmessung=@Dickenmessung WHERE ID=@ID" :
                @"INSERT INTO Wareneingang (Lieferant, LS_Nr, Pos, Zustand, Palettentausch, Artikel, Eingangsdatum, Benutzer, Bemerkung, EBE_Nr, Dickenmessung) VALUES (@Lieferant, @LSNr, @Pos, @Zustand, @Palettentausch, @Artikel, @Eingangsdatum, @Benutzer, @Bemerkung, @EBE, @Dickenmessung); SELECT SCOPE_IDENTITY();";

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
                        command.Parameters.AddWithValue("@Dickenmessung", (object?)dickenmessung ?? DBNull.Value);
                        
                        if (eintragBearbeiten && int.TryParse(id, out int idInt)) command.Parameters.AddWithValue("@ID", idInt);

                        int activeId = 0;
                        if (eintragBearbeiten) { await command.ExecuteNonQueryAsync(); activeId = int.Parse(id!); }
                        else { activeId = Convert.ToInt32(await command.ExecuteScalarAsync()); }

                        if (activeId > 0 && chargenList != null) await InsertChargenAsync(activeId, chargenList, connection, liefermenge ?? "0");

                        string actionText = eintragBearbeiten 
                            ? $"[Wareneingang] Eintrag ID {activeId} aktualisiert (Material: {material ?? "Unbekannt"})"
                            : $"[Wareneingang] Neuer Eintrag ID {activeId} erstellt (Lieferant: {lieferant ?? "Unbekannt"}, Material: {material ?? "Unbekannt"})";
                        await ActivityLogService.InsertLogAsync(session.Name ?? "Unbekannt", actionText);

                        return (true, activeId);
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); return (false, 0); }
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

        private static async Task<bool> SperreChargenFuerWareneingangAsync(int wareneingangId, UserSession session, string sperrgrund)
        {
            const string updateQuery = @"UPDATE dbo.Chargen SET Gesperrt = 1 WHERE Wareneingang_ID = @WareneingangID;";
            const string countQuery = @"SELECT COUNT(*) FROM dbo.Chargen WHERE Wareneingang_ID = @WareneingangID;";

            try
            {
                using (SqlConnection connection = new SqlConnection(SqlManager.FertigungConnectionString))
                {
                    await connection.OpenAsync();

                    int existingChargen;
                    using (SqlCommand countCommand = new SqlCommand(countQuery, connection))
                    {
                        countCommand.Parameters.AddWithValue("@WareneingangID", wareneingangId);
                        existingChargen = Convert.ToInt32(await countCommand.ExecuteScalarAsync());
                    }

                    if (existingChargen <= 0)
                    {
                        return false;
                    }

                    int affectedRows;
                    using (SqlCommand updateCommand = new SqlCommand(updateQuery, connection))
                    {
                        updateCommand.Parameters.AddWithValue("@WareneingangID", wareneingangId);
                        affectedRows = await updateCommand.ExecuteNonQueryAsync();
                    }

                    if (affectedRows > 0)
                    {
                        await InsertSperrlagerLogsForWareneingangAsync(connection, wareneingangId, session, sperrgrund);

                        await ActivityLogService.InsertLogAsync(
                            session.Name ?? "Unbekannt",
                            $"[Wareneingang] {affectedRows} Charge(n) für Wareneingang ID {wareneingangId} gesperrt. Grund: {sperrgrund}");
                    }

                    return affectedRows > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }


        private static async Task InsertSperrlagerLogsForWareneingangAsync(SqlConnection connection, int wareneingangId, UserSession session, string sperrgrund)
        {
            const string selectQuery = @"SELECT ID, Charge FROM dbo.Chargen WHERE Wareneingang_ID = @WareneingangID AND Gesperrt = 1;";

            var gesperrteChargen = new List<(int ID, string Charge)>();
            using (SqlCommand selectCommand = new SqlCommand(selectQuery, connection))
            {
                selectCommand.Parameters.AddWithValue("@WareneingangID", wareneingangId);
                using SqlDataReader reader = await selectCommand.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    gesperrteChargen.Add((
                        reader["ID"] != DBNull.Value ? Convert.ToInt32(reader["ID"]) : 0,
                        reader["Charge"]?.ToString() ?? ""));
                }
            }

            foreach (var charge in gesperrteChargen.Where(c => c.ID > 0 && !string.IsNullOrWhiteSpace(c.Charge)))
            {
                await InsertSperrlagerLogAsync(
                    connection,
                    charge.ID,
                    charge.Charge,
                    "Gesperrt",
                    "Wareneingang",
                    sperrgrund,
                    session.Name ?? "System",
                    session.Personalnummer ?? "System",
                    null);
            }
        }

        private static async Task InsertSperrlagerLogAsync(SqlConnection connection, int chargenId, string charge, string aktion, string bereich, string grund, string benutzer, string personalnummer, string? lagerortQRCode)
        {
            const string query = @"
                INSERT INTO dbo.Sperrlager
                    (Chargen_ID, Charge, Aktion, Bereich, Grund, GesperrtVon, GesperrtVonPersonalnummer, GesperrtAm, LagerortQRCode, Bemerkung)
                VALUES
                    (@ChargenID, @Charge, @Aktion, @Bereich, @Grund, @Benutzer, @Personalnummer, SYSDATETIME(), @LagerortQRCode, @Grund);";

            using SqlCommand command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ChargenID", chargenId);
            command.Parameters.AddWithValue("@Charge", charge);
            command.Parameters.AddWithValue("@Aktion", aktion);
            command.Parameters.AddWithValue("@Bereich", bereich);
            command.Parameters.AddWithValue("@Grund", grund);
            command.Parameters.AddWithValue("@Benutzer", string.IsNullOrWhiteSpace(benutzer) ? "System" : benutzer);
            command.Parameters.AddWithValue("@Personalnummer", string.IsNullOrWhiteSpace(personalnummer) ? "System" : personalnummer);
            command.Parameters.AddWithValue("@LagerortQRCode", string.IsNullOrWhiteSpace(lagerortQRCode) ? DBNull.Value : lagerortQRCode);
            await command.ExecuteNonQueryAsync();
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
