using Microsoft.Data.SqlClient;
using QIN_Production_Web.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace QIN_Production_Web.Data
{
    public class CustomerData
    {
        public string Name { get; set; } = "";
        public bool IsActive { get; set; }
    }

    public class EndkontrolleEintrag
    {
        public int ID { get; set; }
        public string Charge { get; set; } = "";
        public string FANr { get; set; } = "";
        public string Kunde { get; set; } = "";
        public string Projekt { get; set; } = "";
        public string Artikel { get; set; } = "";
        public string Dekor { get; set; } = "";
        public DateTime Datum { get; set; } = DateTime.Today;

        public int Gutteile { get; set; }
        public int Fusseln { get; set; }
        public int Nadelstiche { get; set; }
        public int Pickel { get; set; }
        public int Dekorfehler { get; set; }
        public int Farbfehler { get; set; }
        public int Flecken { get; set; }
        public int Nebel { get; set; }
        public int Vertiefung { get; set; }

        public int Oelflecken { get; set; }
        public int Tiefziehfehler { get; set; }
        public int Fraesfehler { get; set; }
        public int Knicke { get; set; }
        public int Kratzer { get; set; }

        public string Bemerkung { get; set; } = "";
        public string Personalnummer { get; set; } = "100";
    }

    public class EndkontrolleService
    {
        private const string SchlechtteileToleranzColumn = "Schlechtteile_Toleranz";
        private const decimal DefaultSchlechtteileToleranzProzent = 15m;
        private const string QsRecipientEmail = "qsintern@qin-form.de";

        public static async Task<List<CustomerData>> GetCustomersAsync()
        {
            var list = new List<CustomerData>();
            string query = "SELECT Kunde, MAX(CAST(IstAktiv AS INT)) FROM dbo.Kunden WHERE Kunde IS NOT NULL AND Kunde <> '' GROUP BY Kunde ORDER BY MAX(CAST(IstAktiv AS INT)) DESC, Kunde";

            try
            {
                using var con = new SqlConnection(SqlManager.connectionString);
                await con.OpenAsync();
                using var cmd = new SqlCommand(query, con);
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    list.Add(new CustomerData
                    {
                        Name = reader.GetString(0),
                        IsActive = !reader.IsDBNull(1) && reader.GetInt32(1) == 1
                    });
                }
            }
            catch
            {
            }

            return list;
        }

        public static async Task<List<string>> GetProjectsAsync(string kunde)
        {
            var list = new List<string>();

            try
            {
                using var con = new SqlConnection(SqlManager.connectionString);
                await con.OpenAsync();
                using var cmd = new SqlCommand("SELECT DISTINCT Projekt FROM dbo.Kunden WHERE Kunde = @Kunde AND Projekt IS NOT NULL AND Projekt <> '' ORDER BY Projekt", con);
                cmd.Parameters.AddWithValue("@Kunde", kunde);
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    list.Add(reader.GetString(0));
                }
            }
            catch
            {
            }

            return list;
        }

        public static async Task<(List<string> Artikels, List<string> Dekors)> GetArtikelsAndDekorsAsync(string project)
        {
            var artikels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var dekors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                using var con = new SqlConnection(SqlManager.connectionString);
                await con.OpenAsync();
                using var cmd = new SqlCommand("SELECT DISTINCT Artikel, Dekor FROM dbo.Kunden WHERE Projekt = @Projekt", con);
                cmd.Parameters.AddWithValue("@Projekt", project);
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    if (!reader.IsDBNull(0))
                    {
                        foreach (var artikel in reader.GetString(0).Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            artikels.Add(artikel.Trim());
                        }
                    }

                    if (!reader.IsDBNull(1))
                    {
                        foreach (var dekor in reader.GetString(1).Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            dekors.Add(dekor.Trim());
                        }
                    }
                }
            }
            catch
            {
            }

            return (new List<string>(artikels), new List<string>(dekors));
        }

        public static async Task<(bool Success, string Message)> InsertEintragAsync(EndkontrolleEintrag e, string userName)
        {
            try
            {
                using var con = new SqlConnection(SqlManager.connectionString);
                await con.OpenAsync();

                const string query = @"INSERT INTO dbo.Table1
                    (Kunde, Projekt, Artikel, Dekor, Charge, FSKdate, Gutteile, Fusseln, Nadelstiche, Pickel, Dekorfehler, Color, Flecken, Nebel, Vertiefung, Oelflecken, Tiefziehfehler, Fraesfehler, Knicke, Kratzer, Personalnummer, [FA-Nr], Bemerkungen)
                    VALUES (@kunde, @projekt, @artikel, @dekor, @charge, @FSKdate, @gutteile, @fusseln, @nadelstiche, @pickel, @dekorfehler, @color, @flecken, @nebel, @vertiefung, @oelflecken, @tiefziehfehler, @fraesfehler, @knicke, @kratzer, @personalnummer, @FANr, @bemerkungen)";

                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@kunde", e.Kunde);
                    cmd.Parameters.AddWithValue("@projekt", e.Projekt);
                    cmd.Parameters.AddWithValue("@artikel", e.Artikel);
                    cmd.Parameters.AddWithValue("@dekor", e.Dekor);
                    cmd.Parameters.AddWithValue("@charge", e.Charge);
                    cmd.Parameters.AddWithValue("@FSKdate", e.Datum.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@gutteile", e.Gutteile);
                    cmd.Parameters.AddWithValue("@fusseln", e.Fusseln);
                    cmd.Parameters.AddWithValue("@nadelstiche", e.Nadelstiche);
                    cmd.Parameters.AddWithValue("@pickel", e.Pickel);
                    cmd.Parameters.AddWithValue("@dekorfehler", e.Dekorfehler);
                    cmd.Parameters.AddWithValue("@color", e.Farbfehler);
                    cmd.Parameters.AddWithValue("@flecken", e.Flecken);
                    cmd.Parameters.AddWithValue("@nebel", e.Nebel);
                    cmd.Parameters.AddWithValue("@vertiefung", e.Vertiefung);
                    cmd.Parameters.AddWithValue("@oelflecken", e.Oelflecken);
                    cmd.Parameters.AddWithValue("@tiefziehfehler", e.Tiefziehfehler);
                    cmd.Parameters.AddWithValue("@fraesfehler", e.Fraesfehler);
                    cmd.Parameters.AddWithValue("@knicke", e.Knicke);
                    cmd.Parameters.AddWithValue("@kratzer", e.Kratzer);
                    cmd.Parameters.AddWithValue("@personalnummer", e.Personalnummer);
                    cmd.Parameters.AddWithValue("@FANr", e.FANr);
                    cmd.Parameters.AddWithValue("@bemerkungen", e.Bemerkung);
                    await cmd.ExecuteNonQueryAsync();
                }

                await ActivityLogService.InsertLogAsync(userName, $"[Sauberraum] Fehlersammelkarte fuer Charge {e.Charge} wurde erfolgreich erstellt. Bemerkung: {e.Bemerkung}");
                StartSchlechtteileMonitoring(e.Datum, userName);
                return (true, "Erfolgreich gespeichert.");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public static async Task<List<EndkontrolleEintrag>> GetRecentEintraegeAsync(string personalnummer = "100")
        {
            var list = new List<EndkontrolleEintrag>();

            try
            {
                using var con = new SqlConnection(SqlManager.connectionString);
                await con.OpenAsync();
                const string query = @"SELECT TOP 10 ID, Charge, [FA-Nr], Kunde, Projekt, Artikel, Dekor, Gutteile, Fusseln, Nadelstiche, Pickel, Dekorfehler, Color, Flecken, Nebel, Vertiefung, Oelflecken, Tiefziehfehler, Fraesfehler, Knicke, Kratzer, FSKdate, Bemerkungen
                                     FROM dbo.Table1 WHERE Personalnummer = @Personalnummer ORDER BY ID DESC";
                using var cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Personalnummer", personalnummer);
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    list.Add(new EndkontrolleEintrag
                    {
                        ID = Convert.ToInt32(reader["ID"]),
                        Charge = reader["Charge"]?.ToString() ?? "",
                        FANr = reader["FA-Nr"]?.ToString() ?? "",
                        Datum = reader["FSKdate"] == DBNull.Value ? DateTime.Today : Convert.ToDateTime(reader["FSKdate"]),
                        Kunde = reader["Kunde"]?.ToString() ?? "",
                        Projekt = reader["Projekt"]?.ToString() ?? "",
                        Artikel = reader["Artikel"]?.ToString() ?? "",
                        Dekor = reader["Dekor"]?.ToString() ?? "",
                        Gutteile = reader["Gutteile"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Gutteile"]),
                        Fusseln = reader["Fusseln"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Fusseln"]),
                        Nadelstiche = reader["Nadelstiche"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Nadelstiche"]),
                        Pickel = reader["Pickel"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Pickel"]),
                        Dekorfehler = reader["Dekorfehler"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Dekorfehler"]),
                        Farbfehler = reader["Color"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Color"]),
                        Flecken = reader["Flecken"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Flecken"]),
                        Nebel = reader["Nebel"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Nebel"]),
                        Vertiefung = reader["Vertiefung"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Vertiefung"]),
                        Oelflecken = reader["Oelflecken"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Oelflecken"]),
                        Tiefziehfehler = reader["Tiefziehfehler"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Tiefziehfehler"]),
                        Fraesfehler = reader["Fraesfehler"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Fraesfehler"]),
                        Knicke = reader["Knicke"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Knicke"]),
                        Kratzer = reader["Kratzer"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Kratzer"]),
                        Bemerkung = reader["Bemerkungen"]?.ToString() ?? ""
                    });
                }
            }
            catch
            {
            }

            return list;
        }

        public static async Task<(bool Success, string Message)> UpdateEintragFieldAsync(int id, string field, object value, string userName)
        {
            try
            {
                using var con = new SqlConnection(SqlManager.connectionString);
                await con.OpenAsync();
                string query = $"UPDATE dbo.Table1 SET [{field}] = @value WHERE ID = @ID";
                using var cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@value", value ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ID", id);
                int affected = await cmd.ExecuteNonQueryAsync();

                if (affected > 0)
                {
                    await ActivityLogService.InsertLogAsync(userName, $"[Sauberraum] Eintrag ID {id}: Feld '{field}' wurde auf '{value}' geaendert.");
                    return (true, "Aktualisiert");
                }

                return (false, "Eintrag nicht gefunden.");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public static async Task<(bool Success, string Message)> UpdateEintragAsync(EndkontrolleEintrag e, string userName)
        {
            try
            {
                using var con = new SqlConnection(SqlManager.connectionString);
                await con.OpenAsync();
                const string query = @"UPDATE dbo.Table1 SET
                                        Kunde = @kunde,
                                        Projekt = @projekt,
                                        Artikel = @artikel,
                                        Dekor = @dekor,
                                        Charge = @charge,
                                        FSKdate = @FSKdate,
                                        Gutteile = @gutteile,
                                        Fusseln = @fusseln,
                                        Nadelstiche = @nadelstiche,
                                        Pickel = @pickel,
                                        Dekorfehler = @dekorfehler,
                                        Color = @color,
                                        Flecken = @flecken,
                                        Nebel = @nebel,
                                        Vertiefung = @vertiefung,
                                        Oelflecken = @oelflecken,
                                        Tiefziehfehler = @tiefziehfehler,
                                        Fraesfehler = @fraesfehler,
                                        Knicke = @knicke,
                                        Kratzer = @kratzer,
                                        Personalnummer = @personalnummer,
                                        [FA-Nr] = @FANr,
                                        Bemerkungen = @bemerkungen
                                     WHERE ID = @ID";

                using var cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@ID", e.ID);
                cmd.Parameters.AddWithValue("@kunde", e.Kunde);
                cmd.Parameters.AddWithValue("@projekt", e.Projekt);
                cmd.Parameters.AddWithValue("@artikel", e.Artikel);
                cmd.Parameters.AddWithValue("@dekor", e.Dekor);
                cmd.Parameters.AddWithValue("@charge", e.Charge);
                cmd.Parameters.AddWithValue("@FSKdate", e.Datum.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("@gutteile", e.Gutteile);
                cmd.Parameters.AddWithValue("@fusseln", e.Fusseln);
                cmd.Parameters.AddWithValue("@nadelstiche", e.Nadelstiche);
                cmd.Parameters.AddWithValue("@pickel", e.Pickel);
                cmd.Parameters.AddWithValue("@dekorfehler", e.Dekorfehler);
                cmd.Parameters.AddWithValue("@color", e.Farbfehler);
                cmd.Parameters.AddWithValue("@flecken", e.Flecken);
                cmd.Parameters.AddWithValue("@nebel", e.Nebel);
                cmd.Parameters.AddWithValue("@vertiefung", e.Vertiefung);
                cmd.Parameters.AddWithValue("@oelflecken", e.Oelflecken);
                cmd.Parameters.AddWithValue("@tiefziehfehler", e.Tiefziehfehler);
                cmd.Parameters.AddWithValue("@fraesfehler", e.Fraesfehler);
                cmd.Parameters.AddWithValue("@knicke", e.Knicke);
                cmd.Parameters.AddWithValue("@kratzer", e.Kratzer);
                cmd.Parameters.AddWithValue("@personalnummer", e.Personalnummer);
                cmd.Parameters.AddWithValue("@FANr", e.FANr);
                cmd.Parameters.AddWithValue("@bemerkungen", e.Bemerkung);

                int affected = await cmd.ExecuteNonQueryAsync();
                if (affected > 0)
                {
                    await ActivityLogService.InsertLogAsync(userName, $"[Sauberraum] Fehlersammelkarte ID {e.ID} wurde aktualisiert. Charge: {e.Charge}");
                    StartSchlechtteileMonitoring(e.Datum, userName);
                    return (true, "Aktualisiert");
                }

                return (false, "Eintrag nicht gefunden.");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public static async Task<(bool Success, string Message)> DeleteEintragAsync(int id, string userName)
        {
            try
            {
                using var con = new SqlConnection(SqlManager.connectionString);
                await con.OpenAsync();
                using var cmd = new SqlCommand("DELETE FROM dbo.Table1 WHERE ID = @ID", con);
                cmd.Parameters.AddWithValue("@ID", id);
                int affected = await cmd.ExecuteNonQueryAsync();

                if (affected > 0)
                {
                    await ActivityLogService.InsertLogAsync(userName, $"[Sauberraum] Eintrag mit ID {id} wurde erfolgreich geloescht.");
                    return (true, "Geloescht");
                }

                return (false, "Eintrag nicht gefunden.");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        private static void StartSchlechtteileMonitoring(DateTime produktionsdatum, string userName)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await CheckSchlechtteileAndNotifyAsync(produktionsdatum.Date, userName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Endkontrolle-Schlechtteile-Monitoring fehlgeschlagen: {ex.Message}");
                }
            });
        }

        private static async Task CheckSchlechtteileAndNotifyAsync(DateTime produktionsdatum, string userName)
        {
            using var connection = new SqlConnection(SqlManager.connectionString);
            await connection.OpenAsync();

            bool hasToleranceColumn = await ColumnExistsAsync(connection, "dbo", "Materialliste", SchlechtteileToleranzColumn);
            var productionRows = await LoadDailyProductionRowsAsync(connection, produktionsdatum);
            if (productionRows.Count == 0)
            {
                return;
            }

            var materialRows = await LoadMaterialToleranceRowsAsync(connection, hasToleranceColumn);
            foreach (var productionRow in productionRows)
            {
                var match = FindBestMaterialMatch(productionRow.Artikel, materialRows);
                if (match is null)
                {
                    continue;
                }

                productionRow.ToleranzProzent = match.SchlechtteileToleranz ?? DefaultSchlechtteileToleranzProzent;
                productionRow.UsesDefaultTolerance = !match.SchlechtteileToleranz.HasValue;
                productionRow.MatchField = match.MatchField;
                productionRow.MatchedMaterialNumber = match.Nr;
                productionRow.MatchedMaterialLabel = !string.IsNullOrWhiteSpace(match.Beschreibung)
                    ? match.Beschreibung
                    : !string.IsNullOrWhiteSpace(match.Suchbegriff)
                        ? match.Suchbegriff
                        : match.Nr;
            }

            var kritischeMaterialien = productionRows
                .Where(row => row.IsCritical)
                .OrderByDescending(row => row.SchlechtteileProzent)
                .ThenBy(row => row.Artikel, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            if (kritischeMaterialien.Count == 0)
            {
                return;
            }

            string subject = $"Endkontrolle: QS-Alarm Schlechtteilquote überschritten ({produktionsdatum:dd.MM.yyyy})";
            string htmlBody = BuildSchlechtteileAlertHtml(produktionsdatum, DateTime.Now, userName, productionRows, kritischeMaterialien);
            await EmailHelper.SendHtmlEmailAsync(subject, htmlBody, QsRecipientEmail);
        }

        private static async Task<List<EndkontrolleDailyProductionRow>> LoadDailyProductionRowsAsync(SqlConnection connection, DateTime produktionsdatum)
        {
            const string query = @"
SELECT
    Artikel,
    SUM(ISNULL(Gutteile, 0)) AS Gutteile,
    SUM(ISNULL(Fusseln, 0)) AS Fusseln,
    SUM(ISNULL(Nadelstiche, 0)) AS Nadelstiche,
    SUM(ISNULL(Pickel, 0)) AS Pickel,
    SUM(ISNULL(Dekorfehler, 0)) AS Dekorfehler,
    SUM(ISNULL(Color, 0)) AS Farbfehler,
    SUM(ISNULL(Flecken, 0)) AS Flecken,
    SUM(ISNULL(Nebel, 0)) AS Nebel,
    SUM(ISNULL(Vertiefung, 0)) AS Vertiefung,
    SUM(ISNULL(Oelflecken, 0)) AS Oelflecken,
    SUM(ISNULL(Tiefziehfehler, 0)) AS Tiefziehfehler,
    SUM(ISNULL(Fraesfehler, 0)) AS Fraesfehler,
    SUM(ISNULL(Knicke, 0)) AS Knicke,
    SUM(ISNULL(Kratzer, 0)) AS Kratzer,
    SUM(
        ISNULL(Fusseln, 0) +
        ISNULL(Nadelstiche, 0) +
        ISNULL(Pickel, 0) +
        ISNULL(Dekorfehler, 0) +
        ISNULL(Color, 0) +
        ISNULL(Flecken, 0) +
        ISNULL(Nebel, 0) +
        ISNULL(Vertiefung, 0) +
        ISNULL(Oelflecken, 0) +
        ISNULL(Tiefziehfehler, 0) +
        ISNULL(Fraesfehler, 0) +
        ISNULL(Knicke, 0) +
        ISNULL(Kratzer, 0)
    ) AS Schlechtteile
FROM dbo.Table1
WHERE CAST(FSKdate AS date) = @Produktionsdatum
  AND ISNULL(LTRIM(RTRIM(Artikel)), '') <> ''
GROUP BY Artikel;";

            var rows = new List<EndkontrolleDailyProductionRow>();
            using var command = new SqlCommand(query, connection);
            command.Parameters.Add("@Produktionsdatum", SqlDbType.Date).Value = produktionsdatum;
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                rows.Add(new EndkontrolleDailyProductionRow
                {
                    Artikel = reader["Artikel"]?.ToString()?.Trim() ?? string.Empty,
                    Gutteile = reader["Gutteile"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Gutteile"]),
                    Fusseln = reader["Fusseln"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Fusseln"]),
                    Nadelstiche = reader["Nadelstiche"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Nadelstiche"]),
                    Pickel = reader["Pickel"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Pickel"]),
                    Dekorfehler = reader["Dekorfehler"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Dekorfehler"]),
                    Farbfehler = reader["Farbfehler"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Farbfehler"]),
                    Flecken = reader["Flecken"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Flecken"]),
                    Nebel = reader["Nebel"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Nebel"]),
                    Vertiefung = reader["Vertiefung"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Vertiefung"]),
                    Oelflecken = reader["Oelflecken"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Oelflecken"]),
                    Tiefziehfehler = reader["Tiefziehfehler"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Tiefziehfehler"]),
                    Fraesfehler = reader["Fraesfehler"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Fraesfehler"]),
                    Knicke = reader["Knicke"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Knicke"]),
                    Kratzer = reader["Kratzer"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Kratzer"]),
                    Schlechtteile = reader["Schlechtteile"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Schlechtteile"])
                });
            }

            return rows;
        }

        private static async Task<List<EndkontrolleMaterialToleranceRow>> LoadMaterialToleranceRowsAsync(SqlConnection connection, bool hasToleranceColumn)
        {
            string toleranceSelect = hasToleranceColumn
                ? SchlechtteileToleranzColumn
                : "CAST(NULL AS decimal(10,2)) AS Schlechtteile_Toleranz";
            string query = $@"
SELECT Nr, Suchbegriff, Beschreibung, Beschreibung2, {toleranceSelect}
FROM dbo.Materialliste
WHERE ISNULL(LTRIM(RTRIM(Nr)), '') <> ''
   OR ISNULL(LTRIM(RTRIM(Suchbegriff)), '') <> ''
   OR ISNULL(LTRIM(RTRIM(Beschreibung)), '') <> ''
   OR ISNULL(LTRIM(RTRIM(Beschreibung2)), '') <> '';";

            var rows = new List<EndkontrolleMaterialToleranceRow>();
            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                rows.Add(new EndkontrolleMaterialToleranceRow
                {
                    Nr = reader["Nr"]?.ToString()?.Trim(),
                    Suchbegriff = reader["Suchbegriff"]?.ToString()?.Trim(),
                    Beschreibung = reader["Beschreibung"]?.ToString()?.Trim(),
                    Beschreibung2 = reader["Beschreibung2"]?.ToString()?.Trim(),
                    SchlechtteileToleranz = reader["Schlechtteile_Toleranz"] == DBNull.Value
                        ? null
                        : Convert.ToDecimal(reader["Schlechtteile_Toleranz"], CultureInfo.InvariantCulture)
                });
            }

            return rows;
        }

        private static EndkontrolleMaterialToleranceRow? FindBestMaterialMatch(string artikel, List<EndkontrolleMaterialToleranceRow> materialRows)
        {
            EndkontrolleMaterialToleranceRow? bestMatch = null;
            string? bestField = null;
            int bestScore = 0;

            foreach (var materialRow in materialRows)
            {
                foreach (var candidate in new[]
                {
                    ("Nr", materialRow.Nr),
                    ("Suchbegriff", materialRow.Suchbegriff),
                    ("Beschreibung", materialRow.Beschreibung),
                    ("Beschreibung2", materialRow.Beschreibung2)
                })
                {
                    int score = ScoreMaterialMatch(artikel, candidate.Item2);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestMatch = materialRow;
                        bestField = candidate.Item1;
                    }
                }
            }

            if (bestMatch is null || bestScore < 100)
            {
                return null;
            }

            bestMatch.MatchField = bestField;
            return bestMatch;
        }

        private static int ScoreMaterialMatch(string input, string? candidate)
        {
            if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(candidate))
            {
                return 0;
            }

            string normalizedInput = NormalizeMaterialText(input);
            string normalizedCandidate = NormalizeMaterialText(candidate);
            if (string.IsNullOrWhiteSpace(normalizedInput) || string.IsNullOrWhiteSpace(normalizedCandidate))
            {
                return 0;
            }

            if (normalizedInput.Equals(normalizedCandidate, StringComparison.OrdinalIgnoreCase))
            {
                return 1000 + normalizedCandidate.Length;
            }

            var inputTokens = GetMaterialTokens(input);
            var candidateTokens = GetMaterialTokens(candidate);
            int commonTokenCount = inputTokens.Intersect(candidateTokens, StringComparer.OrdinalIgnoreCase).Count();
            bool allInputTokensMatch = inputTokens.Count > 0 && commonTokenCount == inputTokens.Count;

            if (allInputTokensMatch && inputTokens.Count >= 2)
            {
                return 850 + commonTokenCount;
            }

            if (normalizedCandidate.Contains(normalizedInput, StringComparison.OrdinalIgnoreCase))
            {
                return 700 + normalizedInput.Length + (commonTokenCount * 10);
            }

            if (normalizedInput.Contains(normalizedCandidate, StringComparison.OrdinalIgnoreCase))
            {
                return 650 + normalizedCandidate.Length + (commonTokenCount * 10);
            }

            return commonTokenCount >= 2 ? (commonTokenCount * 60) : 0;
        }

        private static string NormalizeMaterialText(string value)
        {
            return string.Concat(Regex.Matches(value.ToUpperInvariant().Replace('µ', 'U'), @"[\p{L}\p{N}]+")
                .Select(match => match.Value)
                .Where(token => token.Length > 1));
        }

        private static HashSet<string> GetMaterialTokens(string value)
        {
            return Regex.Matches(value.ToUpperInvariant().Replace('µ', 'U'), @"[\p{L}\p{N}]+")
                .Select(match => match.Value)
                .Where(token => token.Length > 1)
                .Where(token => token is not "BLENDE" and not "TEIL" and not "SATZ" and not "SET")
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        private static string BuildSchlechtteileAlertHtml(
            DateTime produktionsdatum,
            DateTime ausgeloestAm,
            string userName,
            List<EndkontrolleDailyProductionRow> alleMaterialien,
            List<EndkontrolleDailyProductionRow> kritischeMaterialien)
        {
            var culture = CultureInfo.GetCultureInfo("de-DE");
            var sortierteMaterialien = alleMaterialien
                .OrderByDescending(row => row.IsCritical)
                .ThenByDescending(row => row.SchlechtteileProzent)
                .ThenBy(row => row.Artikel, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            int materialienMitStandardToleranz = sortierteMaterialien.Count(row => row.UsesDefaultTolerance);
            var html = new StringBuilder();
            html.AppendLine("<div style='font-family: Segoe UI, Arial, sans-serif; max-width: 1120px; margin: 0 auto; color: #14213d; background: linear-gradient(180deg, #f8fafc 0%, #eef2f7 100%); border-radius: 28px; overflow: hidden; border: 1px solid #dbe4f0;'>");
            html.AppendLine("  <div style='padding: 20px 28px 18px 28px; background: radial-gradient(circle at top right, rgba(248,113,113,0.22), transparent 34%), linear-gradient(135deg, #7f1d1d 0%, #b91c1c 52%, #ef4444 100%); color: #ffffff;'>");
            html.AppendLine("      <div style='font-size: 13px; letter-spacing: 0.12em; text-transform: uppercase; opacity: 0.88; font-weight: 700;'>Endkontrolle Sauberraum</div>");
            html.AppendLine("      <h2 style='margin: 8px 0 6px 0; font-size: 28px; line-height: 1.12;'>Endkontrolle: QS-Alarm Schlechtteilquote überschritten</h2>");
            html.AppendLine($"      <p style='margin: 0; font-size: 14px; line-height: 1.55; max-width: 760px;'>Am <strong>{produktionsdatum:dd.MM.yyyy}</strong> liegt die Schlechtteilquote bei mindestens einem Material über der gepflegten Toleranz aus <strong>dbo.Materialliste.{SchlechtteileToleranzColumn}</strong>.</p>");
            html.AppendLine("  </div>");

            html.AppendLine("  <div style='padding: 18px 28px 24px 28px;'>");
            html.AppendLine("      <div style='display: flex; flex-wrap: wrap; gap: 12px; margin-bottom: 18px;'>");
            html.AppendLine($"          <div style='background: #ffffff; border: 1px solid #e5e7eb; border-radius: 18px; padding: 14px 16px; min-width: 180px; box-shadow: 0 12px 28px rgba(15, 23, 42, 0.06);'><div style='font-size: 12px; color: #64748b; text-transform: uppercase; letter-spacing: 0.08em; font-weight: 700;'>Ausgelöst am</div><div style='font-size: 22px; font-weight: 800; margin-top: 6px;'>{ausgeloestAm:dd.MM.yyyy HH:mm}</div></div>");
            html.AppendLine($"          <div style='background: #fff1f2; border: 1px solid #fecdd3; border-radius: 18px; padding: 14px 16px; min-width: 180px; box-shadow: 0 12px 28px rgba(15, 23, 42, 0.06);'><div style='font-size: 12px; color: #9f1239; text-transform: uppercase; letter-spacing: 0.08em; font-weight: 700;'>Zu prüfen</div><div style='font-size: 22px; font-weight: 800; margin-top: 6px; color: #be123c;'>{kritischeMaterialien.Count} Material(ien)</div></div>");
            html.AppendLine($"          <div style='background: #fff7ed; border: 1px solid #fdba74; border-radius: 18px; padding: 14px 16px; min-width: 220px; box-shadow: 0 12px 28px rgba(15, 23, 42, 0.06);'><div style='font-size: 12px; color: #9a3412; text-transform: uppercase; letter-spacing: 0.08em; font-weight: 700;'>Standardfälle</div><div style='font-size: 22px; font-weight: 800; margin-top: 6px; color: #c2410c;'>{materialienMitStandardToleranz}</div><div style='font-size: 12px; margin-top: 4px; color: #9a3412;'>ohne gepflegte Toleranz</div></div>");
            html.AppendLine("      </div>");
            html.AppendLine($"      <div style='margin-bottom: 22px; padding: 14px 16px; border-radius: 18px; background: #ffffff; border: 1px solid #e5e7eb; box-shadow: 0 12px 28px rgba(15, 23, 42, 0.06);'><strong>Ausgelöst durch:</strong> {WebUtility.HtmlEncode(userName)}</div>");

            html.AppendLine("      <h3 style='margin: 8px 0 14px 0; color: #991b1b; font-size: 20px;'>Kritische Materialien</h3>");
            html.AppendLine("      <table style='width: 100%; border-collapse: separate; border-spacing: 0; margin: 0 0 26px 0; background: #ffffff; border: 1px solid #fecaca; border-radius: 18px; overflow: hidden; box-shadow: 0 18px 36px rgba(127, 29, 29, 0.08);'>");
            html.AppendLine("          <tr style='background: linear-gradient(135deg, #fee2e2 0%, #fecaca 100%);'>");
            html.AppendLine("              <th style='padding: 12px 14px; border-bottom: 1px solid #fecaca; text-align: left;'>Artikel</th>");
            html.AppendLine("              <th style='padding: 12px 14px; border-bottom: 1px solid #fecaca; text-align: right;'>Gutteile</th>");
            html.AppendLine("              <th style='padding: 12px 14px; border-bottom: 1px solid #fecaca; text-align: right;'>Schlechtteile</th>");
            html.AppendLine("              <th style='padding: 12px 14px; border-bottom: 1px solid #fecaca; text-align: right;'>Quote</th>");
            html.AppendLine("              <th style='padding: 12px 14px; border-bottom: 1px solid #fecaca; text-align: right;'>Toleranz</th>");
            html.AppendLine("              <th style='padding: 12px 14px; border-bottom: 1px solid #fecaca; text-align: left;'>Wichtigste Fehler</th>");
            html.AppendLine("              <th style='padding: 12px 14px; border-bottom: 1px solid #fecaca; text-align: left;'>Materialliste-Match</th>");
            html.AppendLine("          </tr>");

            foreach (var material in kritischeMaterialien)
            {
                html.AppendLine("          <tr style='background: #fff7f7;'>");
                html.AppendLine($"              <td style='padding: 14px; border-bottom: 1px solid #fee2e2; font-weight: 800; color: #b91c1c; vertical-align: top;'>{WebUtility.HtmlEncode(material.Artikel)}</td>");
                html.AppendLine($"              <td style='padding: 14px; border-bottom: 1px solid #fee2e2; text-align: right; vertical-align: top;'>{material.Gutteile.ToString("N0", culture)}</td>");
                html.AppendLine($"              <td style='padding: 14px; border-bottom: 1px solid #fee2e2; text-align: right; vertical-align: top;'>{material.Schlechtteile.ToString("N0", culture)}</td>");
                html.AppendLine($"              <td style='padding: 14px; border-bottom: 1px solid #fee2e2; text-align: right; font-weight: 800; color: #b91c1c; vertical-align: top;'>{material.SchlechtteileProzent.ToString("0.##", culture)} %</td>");
                html.AppendLine($"              <td style='padding: 14px; border-bottom: 1px solid #fee2e2; text-align: right; vertical-align: top;'>{FormatTolerance(material, culture)}</td>");
                html.AppendLine($"              <td style='padding: 14px; border-bottom: 1px solid #fee2e2; vertical-align: top;'>{BuildTopDefectsHtml(material, culture)}</td>");
                html.AppendLine($"              <td style='padding: 14px; border-bottom: 1px solid #fee2e2; vertical-align: top;'>{WebUtility.HtmlEncode(GetMatchLabel(material))}</td>");
                html.AppendLine("          </tr>");
            }

            html.AppendLine("      </table>");
            html.AppendLine("      <h3 style='margin: 8px 0 14px 0; color: #1f2937; font-size: 20px;'>Alle erfassten Materialien des Tages</h3>");
            html.AppendLine("      <table style='width: 100%; border-collapse: separate; border-spacing: 0; margin: 0; background: #ffffff; border: 1px solid #dbe4f0; border-radius: 18px; overflow: hidden; box-shadow: 0 18px 36px rgba(15, 23, 42, 0.06);'>");
            html.AppendLine("          <tr style='background: linear-gradient(135deg, #eff6ff 0%, #e2e8f0 100%);'>");
            html.AppendLine("              <th style='padding: 12px 14px; border-bottom: 1px solid #dbe4f0; text-align: left;'>Artikel</th>");
            html.AppendLine("              <th style='padding: 12px 14px; border-bottom: 1px solid #dbe4f0; text-align: right;'>Gutteile</th>");
            html.AppendLine("              <th style='padding: 12px 14px; border-bottom: 1px solid #dbe4f0; text-align: right;'>Schlechtteile</th>");
            html.AppendLine("              <th style='padding: 12px 14px; border-bottom: 1px solid #dbe4f0; text-align: right;'>Quote</th>");
            html.AppendLine("              <th style='padding: 12px 14px; border-bottom: 1px solid #dbe4f0; text-align: right;'>Toleranz</th>");
            html.AppendLine("              <th style='padding: 12px 14px; border-bottom: 1px solid #dbe4f0; text-align: left;'>Status</th>");
            html.AppendLine("          </tr>");

            foreach (var material in sortierteMaterialien)
            {
                string rowStyle = material.IsCritical ? "background: #fef2f2;" : "background: #ffffff;";
                string statusText = material.IsCritical
                    ? material.UsesDefaultTolerance
                        ? "Kontrollieren · Standard 15 % verwendet"
                        : "Kontrollieren"
                    : material.ToleranzProzent.HasValue
                        ? material.UsesDefaultTolerance
                            ? "Innerhalb Standard 15 %"
                            : "Innerhalb Toleranz"
                        : "Kein Material-Match";
                string titleColor = material.IsCritical ? "#b91c1c" : "#111827";
                string fontWeight = material.IsCritical ? "bold" : "600";

                html.AppendLine($"          <tr style='{rowStyle}'>");
                html.AppendLine($"              <td style='padding: 12px 14px; border-bottom: 1px solid #e5e7eb; font-weight: {fontWeight}; color: {titleColor};'>{WebUtility.HtmlEncode(material.Artikel)}</td>");
                html.AppendLine($"              <td style='padding: 12px 14px; border-bottom: 1px solid #e5e7eb; text-align: right;'>{material.Gutteile.ToString("N0", culture)}</td>");
                html.AppendLine($"              <td style='padding: 12px 14px; border-bottom: 1px solid #e5e7eb; text-align: right;'>{material.Schlechtteile.ToString("N0", culture)}</td>");
                html.AppendLine($"              <td style='padding: 12px 14px; border-bottom: 1px solid #e5e7eb; text-align: right;'>{material.SchlechtteileProzent.ToString("0.##", culture)} %</td>");
                html.AppendLine($"              <td style='padding: 12px 14px; border-bottom: 1px solid #e5e7eb; text-align: right;'>{FormatTolerance(material, culture)}</td>");
                html.AppendLine($"              <td style='padding: 12px 14px; border-bottom: 1px solid #e5e7eb; color: {(material.IsCritical ? "#b91c1c" : "#374151")};'>{WebUtility.HtmlEncode(statusText)}</td>");
                html.AppendLine("          </tr>");
            }

            html.AppendLine("      </table>");
            html.AppendLine("      <div style='margin-top: 22px; padding: 16px 18px; border-radius: 18px; background: #f8fafc; border: 1px solid #dbe4f0; color: #475569; font-size: 12px; line-height: 1.7;'>");
            html.AppendLine("          Die Schlechtteilquote wird aus allen Fehlerfeldern der Endkontrolle als <strong>Schlechtteile / (Gutteile + Schlechtteile) × 100</strong> berechnet.");
            html.AppendLine("      </div>");
            html.AppendLine("  </div>");
            html.AppendLine("</div>");
            return html.ToString();
        }

        private static string BuildTopDefectsHtml(EndkontrolleDailyProductionRow material, CultureInfo culture)
        {
            var topDefects = material.GetTopDefects()
                .Take(3)
                .ToList();

            if (topDefects.Count == 0)
            {
                return "<span style='color: #64748b;'>Keine Einzelwerte</span>";
            }

            var html = new StringBuilder();
            html.AppendLine("<div style='display: flex; flex-direction: column; gap: 6px;'>");

            foreach (var defect in topDefects)
            {
                html.AppendLine($"<div style='display: inline-flex; align-items: center; gap: 8px;'><span style='min-width: 22px; height: 22px; display: inline-flex; align-items: center; justify-content: center; border-radius: 999px; background: #fee2e2; color: #b91c1c; font-size: 11px; font-weight: 800;'>{defect.Rank}</span><span style='font-weight: 700; color: #991b1b;'>{WebUtility.HtmlEncode(defect.Name)}</span><span style='color: #334155;'>{defect.Value.ToString("N0", culture)}</span></div>");
            }

            html.AppendLine("</div>");
            return html.ToString();
        }

        private static string GetMatchLabel(EndkontrolleDailyProductionRow material)
        {
            if (string.IsNullOrWhiteSpace(material.MatchedMaterialLabel))
            {
                return "Kein Match in Materialliste";
            }

            var parts = new List<string> { material.MatchedMaterialLabel };
            if (!string.IsNullOrWhiteSpace(material.MatchedMaterialNumber))
            {
                parts.Add($"Nr. {material.MatchedMaterialNumber}");
            }

            if (!string.IsNullOrWhiteSpace(material.MatchField))
            {
                parts.Add($"Match über {material.MatchField}");
            }

            if (material.UsesDefaultTolerance)
            {
                parts.Add($"Keine Toleranz gepflegt · Standard {DefaultSchlechtteileToleranzProzent.ToString("0.##", CultureInfo.GetCultureInfo("de-DE"))} % verwendet");
            }

            return string.Join(" | ", parts);
        }

        private static string FormatTolerance(EndkontrolleDailyProductionRow material, CultureInfo culture)
        {
            if (!material.ToleranzProzent.HasValue)
            {
                return "-";
            }

            return material.UsesDefaultTolerance
                ? $"{material.ToleranzProzent.Value.ToString("0.##", culture)} % (Standard)"
                : $"{material.ToleranzProzent.Value.ToString("0.##", culture)} %";
        }

        private static async Task<bool> ColumnExistsAsync(SqlConnection connection, string schema, string table, string column)
        {
            const string query = @"
SELECT COUNT(1)
FROM sys.columns c
INNER JOIN sys.tables t ON t.object_id = c.object_id
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE s.name = @Schema AND t.name = @Table AND c.name = @Column;";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Schema", schema);
            command.Parameters.AddWithValue("@Table", table);
            command.Parameters.AddWithValue("@Column", column);
            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }

        private sealed class EndkontrolleDailyProductionRow
        {
            public string Artikel { get; set; } = string.Empty;
            public int Gutteile { get; set; }
            public int Fusseln { get; set; }
            public int Nadelstiche { get; set; }
            public int Pickel { get; set; }
            public int Dekorfehler { get; set; }
            public int Farbfehler { get; set; }
            public int Flecken { get; set; }
            public int Nebel { get; set; }
            public int Vertiefung { get; set; }
            public int Oelflecken { get; set; }
            public int Tiefziehfehler { get; set; }
            public int Fraesfehler { get; set; }
            public int Knicke { get; set; }
            public int Kratzer { get; set; }
            public int Schlechtteile { get; set; }
            public int Gesamt => Gutteile + Schlechtteile;
            public decimal SchlechtteileProzent => Gesamt <= 0 ? 0m : Math.Round((decimal)Schlechtteile * 100m / Gesamt, 2);
            public decimal? ToleranzProzent { get; set; }
            public bool UsesDefaultTolerance { get; set; }
            public string? MatchedMaterialNumber { get; set; }
            public string? MatchedMaterialLabel { get; set; }
            public string? MatchField { get; set; }
            public bool IsCritical => ToleranzProzent.HasValue && SchlechtteileProzent > ToleranzProzent.Value;

            public IEnumerable<(int Rank, string Name, int Value)> GetTopDefects()
            {
                return new[]
                {
                    ("Fusseln", Fusseln),
                    ("Nadelstiche", Nadelstiche),
                    ("Pickel", Pickel),
                    ("Dekorfehler", Dekorfehler),
                    ("Farbfehler", Farbfehler),
                    ("Flecken", Flecken),
                    ("Nebel", Nebel),
                    ("Vertiefung", Vertiefung),
                    ("Ölflecken", Oelflecken),
                    ("Tiefziehfehler", Tiefziehfehler),
                    ("Fräsfehler", Fraesfehler),
                    ("Knicke", Knicke),
                    ("Kratzer", Kratzer)
                }
                .Where(item => item.Item2 > 0)
                .OrderByDescending(item => item.Item2)
                .ThenBy(item => item.Item1, StringComparer.CurrentCultureIgnoreCase)
                .Select((item, index) => (index + 1, item.Item1, item.Item2));
            }
        }

        private sealed class EndkontrolleMaterialToleranceRow
        {
            public string? Nr { get; set; }
            public string? Suchbegriff { get; set; }
            public string? Beschreibung { get; set; }
            public string? Beschreibung2 { get; set; }
            public decimal? SchlechtteileToleranz { get; set; }
            public string? MatchField { get; set; }
        }
    }
}
