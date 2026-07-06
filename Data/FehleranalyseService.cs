using ClosedXML.Excel;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace QIN_Production_Web.Data
{
    public class FehlerAnalyseResult
    {
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
        
        public int Gutteile { get; set; }

        public int SchlechtExtern => Fusseln + Nadelstiche + Pickel + Dekorfehler + Farbfehler + Flecken + Nebel + Vertiefung;
        public int SchlechtIntern => Oelflecken + Tiefziehfehler + Fraesfehler + Knicke + Kratzer;
        public int Schlechtteile => SchlechtIntern + SchlechtExtern;
        public int Gesamt => Gutteile + Schlechtteile;
    }

    public class FehlerRow : FehlerAnalyseResult
    {
        public DateTime FSKdate { get; set; }
        public string Kunde { get; set; } = "";
        public string Projekt { get; set; } = "";
        public string Artikel { get; set; } = "";
        public string Dekor { get; set; } = "";
        public string Charge { get; set; } = "";
        public string Personalnummer { get; set; } = "";
        public string PersonalName { get; set; } = "";
        public string Bemerkungen { get; set; } = "";
    }

    public class ChargeItem
    {
        public string Karte { get; set; } = "";
        public double? Anzahl { get; set; }
        public double? Prozent { get; set; }
    }

    public class FehleranalyseExportResult
    {
        public byte[] Content { get; set; } = Array.Empty<byte>();
        public string FileName { get; set; } = "";
        public string? ErrorMessage { get; set; }
        public bool Success => Content.Length > 0 && string.IsNullOrWhiteSpace(ErrorMessage);
    }

    public class FehleranalyseZielDetail
    {
        public string Material { get; set; } = "";
        public int Gutteile { get; set; }
        public int Schlechtteile { get; set; }
        public int Ziel { get; set; }
        public int Gesamt => Gutteile + Schlechtteile;
        public int Offen => Math.Max(Ziel - Gutteile, 0);
        public int UeberZiel => Math.Max(Gutteile - Ziel, 0);
        public double? Erfuellung => Ziel > 0 ? (double)Gutteile / Ziel : null;
    }

    public class FehleranalyseZielAuswertung
    {
        public int Gutteile { get; set; }
        public int Schlechtteile { get; set; }
        public int Ziel { get; set; }
        public string Hinweis { get; set; } = "";
        public List<FehleranalyseZielDetail> Details { get; set; } = new();

        public int GutteileMitZiel => Details.Where(detail => detail.Ziel > 0).Sum(detail => detail.Gutteile);
        public int SchlechtteileMitZiel => Details.Where(detail => detail.Ziel > 0).Sum(detail => detail.Schlechtteile);
        public int Offen => Math.Max(Ziel - GutteileMitZiel, 0);
        public int UeberZiel => Math.Max(GutteileMitZiel - Ziel, 0);
        public double? Erfuellung => Ziel > 0 ? (double)GutteileMitZiel / Ziel : null;
        public bool HasData => Ziel > 0 || Gutteile > 0 || Schlechtteile > 0 || Details.Count > 0;
    }

    public class FehleranalyseService
    {
        private const string MaterialOhneZuordnung = "Ohne Materialzuordnung";

        public async Task<List<CustomerData>> GetKundenAsync()
        {
            var kunden = new List<CustomerData>();
            string query = "SELECT Kunde, MAX(CAST(IstAktiv AS INT)) FROM dbo.Kunden WHERE Kunde IS NOT NULL AND Kunde <> '' GROUP BY Kunde ORDER BY MAX(CAST(IstAktiv AS INT)) DESC, Kunde";
            try
            {
                using var connection = new SqlConnection(SqlManager.connectionString);
                await connection.OpenAsync();
                using var command = new SqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    kunden.Add(new CustomerData
                    {
                        Name = reader.GetString(0),
                        IsActive = !reader.IsDBNull(1) && reader.GetInt32(1) == 1
                    });
                }
            }
            catch { }
            return kunden;
        }

        public async Task<List<string>> GetProjekteAsync(string kunde)
        {
            var projekte = new List<string>();
            if (string.IsNullOrEmpty(kunde)) return projekte;
            string query = "SELECT DISTINCT Projekt FROM dbo.Kunden WHERE Kunde = @Kunde AND Projekt IS NOT NULL AND Projekt <> '' ORDER BY Projekt";
            try
            {
                using var connection = new SqlConnection(SqlManager.connectionString);
                await connection.OpenAsync();
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Kunde", kunde);
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync()) projekte.Add(reader.GetString(0));
            }
            catch { }
            return projekte;
        }

        public async Task<(List<string> Artikels, List<string> Dekors)> GetArtikelsAndDekorsAsync(string projekt)
        {
            var artikels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var dekors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(projekt)) return (new List<string>(), new List<string>());
            
            try
            {
                using var connection = new SqlConnection(SqlManager.connectionString);
                await connection.OpenAsync();
                
                string query = "SELECT DISTINCT Artikel, Dekor FROM dbo.Kunden WHERE Projekt = @projekt";
                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@projekt", projekt);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    if (!r.IsDBNull(0)) { foreach (var x in r.GetString(0).Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)) artikels.Add(x.Trim()); }
                    if (!r.IsDBNull(1)) { foreach (var x in r.GetString(1).Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)) dekors.Add(x.Trim()); }
                }
            }
            catch { }

            var artikelsList = artikels.OrderBy(x => x).ToList();
            var dekorsList = dekors.OrderBy(x => x).ToList();
            return (artikelsList, dekorsList);
        }

        public async Task<List<FehlerRow>> GetRawFehlerRowsAsync(string chargeId, string kunde, string projekt, string artikel, string dekor, DateTime von, DateTime bis)
        {
            var daten = new List<FehlerRow>();

            string query = @"SELECT t.FSKdate, t.Fusseln, t.Nadelstiche, t.Pickel, t.Dekorfehler, t.Color, t.Flecken, t.Nebel, t.Vertiefung,
                             t.Oelflecken, t.Tiefziehfehler, t.Fraesfehler, t.Knicke, t.Kratzer, t.Gutteile, t.Artikel, t.Personalnummer, t.Dekor, t.Charge, t.Projekt, t.Kunde, t.Bemerkungen, l.Benutzer
                      FROM dbo.Table1 t
                      LEFT JOIN dbo.LoginDaten l ON ISNULL(CAST(t.Personalnummer AS NVARCHAR(100)), '') = ISNULL(CAST(l.Personalnummer AS NVARCHAR(100)), '')
                      WHERE (@chargeId = '' OR t.Charge = @chargeId)
                        AND (@kunde = '' OR t.Kunde = @kunde)
                        AND (@projekt = '' OR t.Projekt = @projekt)
                        AND (@artikel = '' OR t.Artikel = @artikel)
                        AND (@dekor = '' OR t.Dekor = @dekor)
                        AND t.FSKdate >= @fromDate
                        AND t.FSKdate <  DATEADD(day, 1, @toDate);";

            try
            {
                using var connection = new SqlConnection(SqlManager.connectionString);
                await connection.OpenAsync();
                using var command = new SqlCommand(query, connection);

                command.Parameters.AddWithValue("@chargeId", chargeId ?? string.Empty);
                command.Parameters.AddWithValue("@kunde", kunde ?? string.Empty);
                command.Parameters.AddWithValue("@projekt", projekt ?? string.Empty);
                command.Parameters.AddWithValue("@artikel", artikel ?? string.Empty);
                command.Parameters.AddWithValue("@dekor", dekor ?? string.Empty);

                command.Parameters.Add("@fromDate", SqlDbType.Date).Value = von.Date;
                command.Parameters.Add("@toDate", SqlDbType.Date).Value = bis.Date;

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    daten.Add(new FehlerRow
                    {
                        FSKdate = reader["FSKdate"] != DBNull.Value ? Convert.ToDateTime(reader["FSKdate"]) : DateTime.MinValue,
                        Fusseln = reader["Fusseln"] != DBNull.Value ? Convert.ToInt32(reader["Fusseln"]) : 0,
                        Nadelstiche = reader["Nadelstiche"] != DBNull.Value ? Convert.ToInt32(reader["Nadelstiche"]) : 0,
                        Pickel = reader["Pickel"] != DBNull.Value ? Convert.ToInt32(reader["Pickel"]) : 0,
                        Dekorfehler = reader["Dekorfehler"] != DBNull.Value ? Convert.ToInt32(reader["Dekorfehler"]) : 0,
                        Farbfehler = reader["Color"] != DBNull.Value ? Convert.ToInt32(reader["Color"]) : 0,
                        Flecken = reader["Flecken"] != DBNull.Value ? Convert.ToInt32(reader["Flecken"]) : 0,
                        Nebel = reader["Nebel"] != DBNull.Value ? Convert.ToInt32(reader["Nebel"]) : 0,
                        Vertiefung = reader["Vertiefung"] != DBNull.Value ? Convert.ToInt32(reader["Vertiefung"]) : 0,
                        
                        Oelflecken = reader["Oelflecken"] != DBNull.Value ? Convert.ToInt32(reader["Oelflecken"]) : 0,
                        Tiefziehfehler = reader["Tiefziehfehler"] != DBNull.Value ? Convert.ToInt32(reader["Tiefziehfehler"]) : 0,
                        Fraesfehler = reader["Fraesfehler"] != DBNull.Value ? Convert.ToInt32(reader["Fraesfehler"]) : 0,
                        Knicke = reader["Knicke"] != DBNull.Value ? Convert.ToInt32(reader["Knicke"]) : 0,
                        Kratzer = reader["Kratzer"] != DBNull.Value ? Convert.ToInt32(reader["Kratzer"]) : 0,
                        
                        Gutteile = reader["Gutteile"] != DBNull.Value ? Convert.ToInt32(reader["Gutteile"]) : 0,
                        
                        Kunde = reader["Kunde"]?.ToString() ?? "",
                        Projekt = reader["Projekt"]?.ToString() ?? "",
                        Artikel = reader["Artikel"]?.ToString() ?? "",
                        Charge = reader["Charge"]?.ToString() ?? "",
                        Personalnummer = reader["Personalnummer"]?.ToString() ?? "",
                        PersonalName = reader["Benutzer"] != DBNull.Value ? (reader["Benutzer"].ToString() ?? "") : (reader["Personalnummer"]?.ToString() ?? ""),
                        Dekor = reader["Dekor"]?.ToString() ?? "",
                        Bemerkungen = reader["Bemerkungen"]?.ToString() ?? ""
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting fehler rows: {ex.Message}");
            }

            return daten;
        }

        public async Task<FehleranalyseZielAuswertung> GetProduktionszielAuswertungAsync(
            IReadOnlyCollection<FehlerRow>? fehlerRows,
            DateTime von,
            DateTime bis,
            string? artikelFilter,
            bool hasContextFiltersWithoutArtikel)
        {
            var rows = fehlerRows?.ToList() ?? new List<FehlerRow>();
            var plannedRows = await GetSchichtplanZielRowsAsync(von, bis);

            var actualByMaterial = rows
                .GroupBy(
                    row => string.IsNullOrWhiteSpace(row.Artikel) ? MaterialOhneZuordnung : row.Artikel.Trim(),
                    StringComparer.OrdinalIgnoreCase)
                .Select(group => new FehleranalyseMaterialIstRow
                {
                    Material = group.Key,
                    Gutteile = group.Sum(item => item.Gutteile),
                    Schlechtteile = group.Sum(item => item.Schlechtteile)
                })
                .ToList();

            var planByMaterial = plannedRows
                .Where(row => !string.IsNullOrWhiteSpace(row.Material))
                .Where(row => row.ZielMenge > 0)
                .Where(row => row.BenutzerAnzahl > 0)
                .GroupBy(row => row.Material.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(group => new FehleranalyseMaterialPlanRow
                {
                    Material = group.First().Material.Trim(),
                    Ziel = group.Sum(item => item.ZielMenge * item.BenutzerAnzahl)
                })
                .Where(row => row.Ziel > 0)
                .ToList();

            if (!string.IsNullOrWhiteSpace(artikelFilter))
            {
                planByMaterial = FilterPlanMaterialsByReference(planByMaterial, new[] { artikelFilter.Trim() });
            }
            else if (hasContextFiltersWithoutArtikel)
            {
                var actualMaterialNames = actualByMaterial
                    .Where(row => !string.Equals(row.Material, MaterialOhneZuordnung, StringComparison.OrdinalIgnoreCase))
                    .Select(row => row.Material)
                    .ToList();

                planByMaterial = FilterPlanMaterialsByReference(planByMaterial, actualMaterialNames);
            }

            var detailsByMaterial = new Dictionary<string, FehleranalyseZielDetail>(StringComparer.OrdinalIgnoreCase);

            foreach (var plannedMaterial in planByMaterial)
            {
                detailsByMaterial[plannedMaterial.Material] = new FehleranalyseZielDetail
                {
                    Material = plannedMaterial.Material,
                    Ziel = plannedMaterial.Ziel
                };
            }

            foreach (var actualMaterial in actualByMaterial)
            {
                var matchingPlan = MatchPlanMaterial(planByMaterial, actualMaterial.Material);
                var key = matchingPlan?.Material ?? actualMaterial.Material;

                if (!detailsByMaterial.TryGetValue(key, out var detail))
                {
                    detail = new FehleranalyseZielDetail
                    {
                        Material = key
                    };
                    detailsByMaterial[key] = detail;
                }

                detail.Gutteile += actualMaterial.Gutteile;
                detail.Schlechtteile += actualMaterial.Schlechtteile;
            }

            var orderedDetails = detailsByMaterial.Values
                .OrderByDescending(detail => detail.Ziel)
                .ThenByDescending(detail => detail.Gutteile)
                .ThenByDescending(detail => detail.Schlechtteile)
                .ThenBy(detail => detail.Material, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            return new FehleranalyseZielAuswertung
            {
                Gutteile = rows.Sum(row => row.Gutteile),
                Schlechtteile = rows.Sum(row => row.Schlechtteile),
                Ziel = orderedDetails.Sum(detail => detail.Ziel),
                Hinweis = hasContextFiltersWithoutArtikel
                    ? "Bei Kunde-, Projekt-, Dekor- oder Charge-Filtern wird das Ziel über die im Ergebnis gefundenen Materialien an den Schichtplan angekoppelt, weil diese Zusatzfelder dort nicht gespeichert sind."
                    : "",
                Details = orderedDetails
            };
        }

        public Task<FehleranalyseExportResult> ExportToExcelAsync(List<FehlerRow> fehlerListe)
        {
            if (fehlerListe == null || !fehlerListe.Any())
            {
                return Task.FromResult(new FehleranalyseExportResult
                {
                    ErrorMessage = "Keine Daten vorhanden."
                });
            }

            try
            {
                var now = DateTime.Now;
                using var workbook = new XLWorkbook();
                var ws = workbook.Worksheets.Add("Auswertung");

                ws.Range("A1:I1").Merge().Value = $"Ausschusswerte {now:MMMM yyyy}";
                ws.Cell("A1").Style.Font.Bold = true;
                ws.Cell("A1").Style.Fill.BackgroundColor = XLColor.LightGreen;
                ws.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell("A1").Style.Font.Underline = XLFontUnderlineValues.Single;
                ws.Row(1).Height = 30;
                ws.Cell("A1").Style.Font.FontSize = 16;

                string[] headers = { "Artikel", "Folie", "Gutteile", "Schlechtteile", "Extern", "Intern", "Extern %", "Intern %", "Gesamt %" };
                for (int i = 0; i < headers.Length; i++)
                {
                    ws.Cell(2, i + 1).Value = headers[i];
                    ws.Cell(2, i + 1).Style.Font.Bold = true;
                }

                int row = 3;
                var grouped = fehlerListe
                    .GroupBy(r => new { r.Artikel, r.Dekor })
                    .Select(g => new
                    {
                        Artikel = g.Key.Artikel,
                        Dekor = g.Key.Dekor,
                        Gutteile = g.Sum(r => r.Gutteile),
                        SchlechtIntern = g.Sum(r => r.SchlechtIntern),
                        SchlechtExtern = g.Sum(r => r.SchlechtExtern),
                        Schlechtteile = g.Sum(r => r.Schlechtteile),
                        Gesamt = g.Sum(r => r.Gesamt)
                    });

                foreach (var entry in grouped)
                {
                    double ex = entry.Gesamt > 0 ? (double)entry.SchlechtExtern / entry.Gesamt : 0;
                    double i = entry.Gesamt > 0 ? (double)entry.SchlechtIntern / entry.Gesamt : 0;
                    double g = entry.Gesamt > 0 ? (double)entry.Schlechtteile / entry.Gesamt : 0;

                    ws.Cell(row, 1).Value = entry.Artikel;
                    ws.Cell(row, 2).Value = entry.Dekor;
                    ws.Cell(row, 3).Value = entry.Gutteile;
                    ws.Cell(row, 4).Value = entry.Schlechtteile;
                    ws.Cell(row, 5).Value = entry.SchlechtExtern;
                    ws.Cell(row, 6).Value = entry.SchlechtIntern;
                    ws.Cell(row, 7).Value = Math.Round(ex, 4);
                    ws.Cell(row, 7).Style.NumberFormat.Format = "0.00%";
                    ws.Cell(row, 8).Value = Math.Round(i, 4);
                    ws.Cell(row, 8).Style.NumberFormat.Format = "0.00%";
                    ws.Cell(row, 9).Value = Math.Round(g, 4);
                    ws.Cell(row, 9).Style.NumberFormat.Format = "0.00%";

                    row++;
                }

                ws.Columns().AdjustToContents();
                ws.Range(2, 1, row - 1, headers.Length).SetAutoFilter();

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);

                return Task.FromResult(new FehleranalyseExportResult
                {
                    Content = stream.ToArray(),
                    FileName = $"Auswertung_{now:yyyy-MM-dd}.xlsx"
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new FehleranalyseExportResult
                {
                    ErrorMessage = $"Fehler beim Export: {ex.Message}"
                });
            }
        }

        private async Task<List<FehleranalyseSchichtplanZielRow>> GetSchichtplanZielRowsAsync(DateTime von, DateTime bis)
        {
            var rows = new List<FehleranalyseSchichtplanZielRow>();

            const string query = @"
WITH MaterialAssignments AS
(
    SELECT
        p.PlanDatum,
        COALESCE(m1.Material, e.Material) AS Material,
        CASE
            WHEN m1.ID IS NULL THEN 0
            WHEN CAST(m1.CreatedAt AS date) > p.PlanDatum THEN 0
            WHEN m1.UpdatedAt IS NOT NULL AND CAST(m1.UpdatedAt AS date) > p.PlanDatum THEN 0
            ELSE ISNULL(m1.TagesMenge, 0)
        END AS ZielMenge,
        COUNT(ben.ID) AS BenutzerAnzahl
    FROM dbo.SchichtplanPlan p
    INNER JOIN dbo.SchichtplanEintrag e
        ON e.SchichtplanPlanID = p.ID
    INNER JOIN dbo.SchichtplanArbeitsplatz ap
        ON ap.ID = e.ArbeitsplatzID
    INNER JOIN dbo.SchichtplanEintragBenutzer ben
        ON ben.SchichtplanEintragID = e.ID
    LEFT JOIN dbo.SchichtplanMaterialStamm m1
        ON m1.ID = e.MaterialStammID
    WHERE p.PlanDatum >= @fromDate
      AND p.PlanDatum <= @toDate
      AND ap.Bereich = N'Sauberraum'
      AND ISNULL(LTRIM(RTRIM(COALESCE(m1.Material, e.Material))), N'') <> N''
    GROUP BY
        p.PlanDatum,
        e.ID,
        COALESCE(m1.Material, e.Material),
        CASE
            WHEN m1.ID IS NULL THEN 0
            WHEN CAST(m1.CreatedAt AS date) > p.PlanDatum THEN 0
            WHEN m1.UpdatedAt IS NOT NULL AND CAST(m1.UpdatedAt AS date) > p.PlanDatum THEN 0
            ELSE ISNULL(m1.TagesMenge, 0)
        END

    UNION ALL

    SELECT
        p.PlanDatum,
        COALESCE(m2.Material, e.Material2) AS Material,
        CASE
            WHEN m2.ID IS NULL THEN 0
            WHEN CAST(m2.CreatedAt AS date) > p.PlanDatum THEN 0
            WHEN m2.UpdatedAt IS NOT NULL AND CAST(m2.UpdatedAt AS date) > p.PlanDatum THEN 0
            ELSE ISNULL(m2.TagesMenge, 0)
        END AS ZielMenge,
        COUNT(ben.ID) AS BenutzerAnzahl
    FROM dbo.SchichtplanPlan p
    INNER JOIN dbo.SchichtplanEintrag e
        ON e.SchichtplanPlanID = p.ID
    INNER JOIN dbo.SchichtplanArbeitsplatz ap
        ON ap.ID = e.ArbeitsplatzID
    INNER JOIN dbo.SchichtplanEintragBenutzer ben
        ON ben.SchichtplanEintragID = e.ID
    LEFT JOIN dbo.SchichtplanMaterialStamm m2
        ON m2.ID = e.MaterialStammID2
    WHERE p.PlanDatum >= @fromDate
      AND p.PlanDatum <= @toDate
      AND ap.Bereich = N'Sauberraum'
      AND ISNULL(LTRIM(RTRIM(COALESCE(m2.Material, e.Material2))), N'') <> N''
    GROUP BY
        p.PlanDatum,
        e.ID,
        COALESCE(m2.Material, e.Material2),
        CASE
            WHEN m2.ID IS NULL THEN 0
            WHEN CAST(m2.CreatedAt AS date) > p.PlanDatum THEN 0
            WHEN m2.UpdatedAt IS NOT NULL AND CAST(m2.UpdatedAt AS date) > p.PlanDatum THEN 0
            ELSE ISNULL(m2.TagesMenge, 0)
        END
)
SELECT
    PlanDatum,
    Material,
    ZielMenge,
    BenutzerAnzahl
FROM MaterialAssignments
WHERE ZielMenge > 0
  AND BenutzerAnzahl > 0;";

            try
            {
                using var connection = new SqlConnection(SqlManager.FertigungConnectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand(query, connection);
                command.Parameters.Add("@fromDate", SqlDbType.Date).Value = von.Date;
                command.Parameters.Add("@toDate", SqlDbType.Date).Value = bis.Date;

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    rows.Add(new FehleranalyseSchichtplanZielRow
                    {
                        PlanDatum = reader["PlanDatum"] != DBNull.Value ? Convert.ToDateTime(reader["PlanDatum"]) : DateTime.MinValue,
                        Material = reader["Material"]?.ToString() ?? "",
                        ZielMenge = reader["ZielMenge"] != DBNull.Value ? Convert.ToInt32(reader["ZielMenge"]) : 0,
                        BenutzerAnzahl = reader["BenutzerAnzahl"] != DBNull.Value ? Convert.ToInt32(reader["BenutzerAnzahl"]) : 0
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting schichtplan target rows: {ex.Message}");
            }

            return rows;
        }

        private static List<FehleranalyseMaterialPlanRow> FilterPlanMaterialsByReference(
            List<FehleranalyseMaterialPlanRow> planMaterials,
            IEnumerable<string> referenceMaterials)
        {
            var references = referenceMaterials
                .Where(material => !string.IsNullOrWhiteSpace(material))
                .Select(material => material.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (references.Count == 0)
            {
                return new List<FehleranalyseMaterialPlanRow>();
            }

            return planMaterials
                .Where(planMaterial => references.Any(reference => ScoreMaterialMatch(planMaterial.Material, reference) >= 2))
                .ToList();
        }

        private static FehleranalyseMaterialPlanRow? MatchPlanMaterial(
            List<FehleranalyseMaterialPlanRow> planMaterials,
            string actualMaterial)
        {
            if (string.IsNullOrWhiteSpace(actualMaterial) ||
                string.Equals(actualMaterial, MaterialOhneZuordnung, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            FehleranalyseMaterialPlanRow? bestMatch = null;
            var bestScore = 0;

            foreach (var planMaterial in planMaterials)
            {
                var score = ScoreMaterialMatch(planMaterial.Material, actualMaterial);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMatch = planMaterial;
                }
            }

            return bestScore >= 2 ? bestMatch : null;
        }

        private static int ScoreMaterialMatch(string assignedMaterial, string producedMaterial)
        {
            var assignedTokens = TokenizeMaterialName(assignedMaterial);
            var producedTokens = TokenizeMaterialName(producedMaterial);

            if (assignedTokens.Count == 0 || producedTokens.Count == 0)
            {
                return 0;
            }

            var commonTokens = assignedTokens.Intersect(producedTokens, StringComparer.OrdinalIgnoreCase).ToList();
            var assignedCompact = string.Concat(assignedTokens);
            var producedCompact = string.Concat(producedTokens);

            var score = commonTokens.Count;

            if (!string.IsNullOrWhiteSpace(assignedCompact) &&
                !string.IsNullOrWhiteSpace(producedCompact) &&
                (producedCompact.Contains(assignedCompact, StringComparison.OrdinalIgnoreCase) ||
                 assignedCompact.Contains(producedCompact, StringComparison.OrdinalIgnoreCase)))
            {
                score += 2;
            }

            if (assignedTokens.Count <= 3 && commonTokens.Count == assignedTokens.Count)
            {
                score += 1;
            }

            return score;
        }

        private static List<string> TokenizeMaterialName(string? material)
        {
            if (string.IsNullOrWhiteSpace(material))
            {
                return new List<string>();
            }

            var cleaned = new string(
                material
                    .ToUpperInvariant()
                    .Select(ch => char.IsLetterOrDigit(ch) ? ch : ' ')
                    .ToArray());

            return cleaned
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(token => token.Length > 1)
                .Where(token => token is not "BLENDE" and not "TEIL" and not "SATZ" and not "SET")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private sealed class FehleranalyseSchichtplanZielRow
        {
            public DateTime PlanDatum { get; set; }
            public string Material { get; set; } = "";
            public int ZielMenge { get; set; }
            public int BenutzerAnzahl { get; set; }
        }

        private sealed class FehleranalyseMaterialIstRow
        {
            public string Material { get; set; } = "";
            public int Gutteile { get; set; }
            public int Schlechtteile { get; set; }
        }

        private sealed class FehleranalyseMaterialPlanRow
        {
            public string Material { get; set; } = "";
            public int Ziel { get; set; }
        }
    }
}
