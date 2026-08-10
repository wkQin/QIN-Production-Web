using ClosedXML.Excel;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
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

    public class FehleranalyseExportRequest
    {
        public IReadOnlyCollection<FehlerRow> Eintraege { get; set; } = Array.Empty<FehlerRow>();
        public DateTime StartDatum { get; set; }
        public DateTime EndDatum { get; set; }
        public string Charge { get; set; } = "";
        public string Kunde { get; set; } = "";
        public string Projekt { get; set; } = "";
        public string Artikel { get; set; } = "";
        public string Dekor { get; set; } = "";
    }

    public sealed class FehleranalyseMitarbeiterMaterialRow
    {
        public string Mitarbeiter { get; init; } = "";
        public string Personalnummer { get; init; } = "";
        public string Kunde { get; init; } = "";
        public string Projekt { get; init; } = "";
        public string Artikel { get; init; } = "";
        public string Dekor { get; init; } = "";
        public int Eintraege { get; init; }
        public int Chargen { get; init; }
        public string ChargeBeispiele { get; init; } = "";
        public int Gutteile { get; init; }
        public int SchlechtExtern { get; init; }
        public int SchlechtIntern { get; init; }
        public int Schlechtteile { get; init; }
        public int Gesamt { get; init; }
        public double Ausschussquote { get; init; }
        public double Gutquote { get; init; }
        public double ExternQuote { get; init; }
        public double InternQuote { get; init; }
        public string TopFehler1 { get; init; } = "-";
        public string TopFehler2 { get; init; } = "-";
        public string TopFehler3 { get; init; } = "-";
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
        private static readonly CultureInfo ExportCulture = CultureInfo.GetCultureInfo("de-DE");
        private static readonly (string Name, string Bereich, Func<FehlerAnalyseResult, int> Selector)[] FehlerartenDefinitionen =
        {
            ("Fusseln", "Extern", row => row.Fusseln),
            ("Nadelstiche", "Extern", row => row.Nadelstiche),
            ("Pickel", "Extern", row => row.Pickel),
            ("Dekorfehler", "Extern", row => row.Dekorfehler),
            ("Farbfehler", "Extern", row => row.Farbfehler),
            ("Flecken", "Extern", row => row.Flecken),
            ("Nebel", "Extern", row => row.Nebel),
            ("Vertiefung", "Extern", row => row.Vertiefung),
            ("Ölflecken", "Intern", row => row.Oelflecken),
            ("Tiefziehfehler", "Intern", row => row.Tiefziehfehler),
            ("Stanz-/Fräsfehler", "Intern", row => row.Fraesfehler),
            ("Knicke", "Intern", row => row.Knicke),
            ("Kratzer", "Intern", row => row.Kratzer)
        };

        private sealed class FehlerartExportRow
        {
            public string Bereich { get; init; } = "";
            public string Fehlerart { get; init; } = "";
            public int Anzahl { get; init; }
            public double AnteilGesamtmenge { get; init; }
            public double AnteilSchlechtteile { get; init; }
        }

        private sealed class MaterialExportRow
        {
            public string Kunde { get; init; } = "";
            public string Projekt { get; init; } = "";
            public string Artikel { get; init; } = "";
            public string Dekor { get; init; } = "";
            public int Mitarbeitende { get; init; }
            public int Eintraege { get; init; }
            public int Gutteile { get; init; }
            public int SchlechtExtern { get; init; }
            public int SchlechtIntern { get; init; }
            public int Schlechtteile { get; init; }
            public int Gesamt { get; init; }
            public double Ausschussquote { get; init; }
            public double ExternQuote { get; init; }
            public double InternQuote { get; init; }
            public double BesteQuote { get; init; }
            public double SchlechtesteQuote { get; init; }
            public double Quotenspanne { get; init; }
            public string AuffaelligerMitarbeiter { get; init; } = "-";
            public string HaeufigsterFehler { get; init; } = "-";
        }

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

        public Task<FehleranalyseExportResult> ExportToExcelAsync(FehleranalyseExportRequest request)
        {
            var fehlerListe = request?.Eintraege?
                .Where(row => row != null)
                .OrderBy(row => row.FSKdate)
                .ThenBy(row => row.PersonalName, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(row => row.Artikel, StringComparer.CurrentCultureIgnoreCase)
                .ToList() ?? new List<FehlerRow>();

            if (!fehlerListe.Any())
            {
                return Task.FromResult(new FehleranalyseExportResult
                {
                    ErrorMessage = "Keine Daten vorhanden."
                });
            }

            try
            {
                var now = DateTime.Now;
                var gesamt = SummarizeResults(fehlerListe);
                var fehlerarten = CreateFehlerartRows(gesamt);
                var mitarbeitervergleich = CreateMitarbeiterExportRows(fehlerListe);
                var materialvergleich = CreateMaterialExportRows(fehlerListe, mitarbeitervergleich);
                var auffaelligeKombinationen = mitarbeitervergleich
                    .OrderByDescending(row => row.Schlechtteile)
                    .ThenByDescending(row => row.Ausschussquote)
                    .ThenByDescending(row => row.Gesamt)
                    .Take(12)
                    .ToList();

                using var workbook = new XLWorkbook();
                BuildOverviewWorksheetReadable(workbook, request ?? new FehleranalyseExportRequest(), now, gesamt, fehlerarten, auffaelligeKombinationen);
                BuildMitarbeiterWorksheetReadable(workbook, mitarbeitervergleich);
                BuildMaterialWorksheetReadable(workbook, materialvergleich);
                BuildEintraegeWorksheetReadable(workbook, fehlerListe);

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);

                return Task.FromResult(new FehleranalyseExportResult
                {
                    Content = stream.ToArray(),
                    FileName = BuildExportFileName(request ?? new FehleranalyseExportRequest(), now)
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

        public List<FehleranalyseMitarbeiterMaterialRow> GetMitarbeiterMaterialRows(IReadOnlyCollection<FehlerRow>? rows)
        {
            return CreateMitarbeiterExportRows(rows ?? Array.Empty<FehlerRow>());
        }

        private static void BuildOverviewWorksheet(
            XLWorkbook workbook,
            FehleranalyseExportRequest request,
            DateTime createdAt,
            FehlerAnalyseResult gesamt,
            IReadOnlyList<FehlerartExportRow> fehlerarten,
            IReadOnlyList<FehleranalyseMitarbeiterMaterialRow> auffaelligeKombinationen)
        {
            var ws = workbook.Worksheets.Add("QS-Übersicht");
            ws.Range("A1:L1").Merge().Value = "Fehleranalyse QS-Export";
            ApplyTitleStyle(ws.Range("A1:L1"), XLColor.FromHtml("#0F4C81"));

            ws.Range("A2:L2").Merge().Value = $"Erstellt am {createdAt:dd.MM.yyyy HH:mm}";
            ApplySubtitleStyle(ws.Range("A2:L2"));

            ws.Range("A4:F4").Merge().Value = "Ausgewählter Bereich";
            ApplySectionStyle(ws.Range("A4:F4"));
            WriteFilterRow(ws, 5, "Zeitraum", $"{request.StartDatum:dd.MM.yyyy} bis {request.EndDatum:dd.MM.yyyy}", 1, 6);
            WriteFilterRow(ws, 6, "Charge", request.Charge, 1, 6);
            WriteFilterRow(ws, 7, "Kunde", request.Kunde, 1, 6);
            WriteFilterRow(ws, 8, "Projekt", request.Projekt, 1, 6);
            WriteFilterRow(ws, 9, "Artikel", request.Artikel, 1, 6);
            WriteFilterRow(ws, 10, "Dekor", request.Dekor, 1, 6);

            ws.Range("G4:L4").Merge().Value = "Gesamtwerte";
            ApplySectionStyle(ws.Range("G4:L4"));
            WriteMetricCard(ws, 5, 7, 6, 8, "Gutteile", gesamt.Gutteile, XLColor.FromHtml("#DCFCE7"));
            WriteMetricCard(ws, 5, 9, 6, 10, "Schlechtteile", gesamt.Schlechtteile, XLColor.FromHtml("#FEE2E2"));
            WriteMetricCard(ws, 5, 11, 6, 12, "Gesamt", gesamt.Gesamt, XLColor.FromHtml("#DBEAFE"));
            WriteMetricCard(ws, 8, 7, 9, 8, "Schlecht extern", gesamt.SchlechtExtern, XLColor.FromHtml("#FFEDD5"));
            WriteMetricCard(ws, 8, 9, 9, 10, "Schlecht intern", gesamt.SchlechtIntern, XLColor.FromHtml("#EDE9FE"));
            WriteMetricCard(ws, 8, 11, 9, 12, "Einträge", request.Eintraege.Count, XLColor.FromHtml("#E0F2FE"));
            ws.Range("A12:L12").Merge().Value = "Prozentwerte";
            WriteMetricRow(ws, 7, "Internquote", CalculateQuote(gesamt.SchlechtIntern, gesamt.Gesamt), "Schlecht extern", gesamt.SchlechtExtern, "Schlecht intern", gesamt.SchlechtIntern, true, percentFirst: true);

            var row = 12;
            ws.Range(row, 1, row, 10).Merge().Value = "QS-Vergleich nach Mitarbeiter und Material";
            ApplySectionStyle(ws.Range(row, 1, row, 10));
            row++;

            string[] auffaelligHeaders =
            {
                "Mitarbeiter", "Personalnr.", "Artikel", "Dekor", "Gutteile", "Schlechtteile", "Gesamt", "Ausschuss %", "Extern %", "Top Fehler"
            };
            WriteHeaderRow(ws, row, auffaelligHeaders);
            row++;

            if (!auffaelligeKombinationen.Any())
            {
                ws.Range(row, 1, row, 10).Merge().Value = "Keine auswertbaren Kombinationen vorhanden.";
                ApplyEmptyRowStyle(ws.Range(row, 1, row, 10));
                row++;
            }
            else
            {
                foreach (var item in auffaelligeKombinationen)
                {
                    ws.Cell(row, 1).Value = item.Mitarbeiter;
                    ws.Cell(row, 2).Value = item.Personalnummer;
                    ws.Cell(row, 3).Value = item.Artikel;
                    ws.Cell(row, 4).Value = item.Dekor;
                    ws.Cell(row, 5).Value = item.Gutteile;
                    ws.Cell(row, 6).Value = item.Schlechtteile;
                    ws.Cell(row, 7).Value = item.Gesamt;
                    SetPercentCell(ws.Cell(row, 8), item.Ausschussquote);
                    SetPercentCell(ws.Cell(row, 9), item.ExternQuote);
                    ws.Cell(row, 10).Value = JoinTopFehler(item.TopFehler1, item.TopFehler2, item.TopFehler3);
                    row++;
                }
            }

            ws.Range(row, 1, row, 10).Merge().Value = "Fehlerarten nach Häufigkeit";
            ApplySectionStyle(ws.Range(row, 1, row, 10));
            row++;

            string[] fehlerHeaders = { "Bereich", "Fehlerart", "Anzahl", "Anteil an Gesamtmenge", "Anteil an Schlechtteilen" };
            WriteHeaderRow(ws, row, fehlerHeaders, 1, 5);
            row++;

            foreach (var fehler in fehlerarten)
            {
                ws.Cell(row, 1).Value = fehler.Bereich;
                ws.Cell(row, 2).Value = fehler.Fehlerart;
                ws.Cell(row, 3).Value = fehler.Anzahl;
                SetPercentCell(ws.Cell(row, 4), fehler.AnteilGesamtmenge);
                SetPercentCell(ws.Cell(row, 5), fehler.AnteilSchlechtteile);
                row++;
            }

            FinalizeWorksheet(ws, 10, true);
        }

        private static void BuildMitarbeiterWorksheet(XLWorkbook workbook, IReadOnlyList<FehleranalyseMitarbeiterMaterialRow> rows)
        {
            var ws = workbook.Worksheets.Add("Mitarbeitervergleich");
            ws.Range("A1:R1").Merge().Value = "Mitarbeitervergleich je Material";
            ApplyTitleStyle(ws.Range("A1:R1"), XLColor.FromHtml("#0F766E"));
            ws.Range("A2:R2").Merge().Value = "Direkter QS-Vergleich pro Mitarbeiter und Materialkombination inklusive Prozentwerten und Fehler-Schwerpunkten.";
            ApplySubtitleStyle(ws.Range("A2:R2"));

            var headerRow = 4;
            string[] headers =
            {
                "Mitarbeiter", "Personalnr.", "Kunde", "Projekt", "Artikel", "Dekor", "Einträge", "Chargen", "Charge-Beispiele",
                "Gutteile", "Schlecht extern", "Schlecht intern", "Schlechtteile", "Gesamt", "Ausschuss %", "Gut %", "Extern %", "Intern %"
            };
            WriteHeaderRow(ws, headerRow, headers);

            var row = headerRow + 1;
            foreach (var item in rows)
            {
                ws.Cell(row, 1).Value = item.Mitarbeiter;
                ws.Cell(row, 2).Value = item.Personalnummer;
                ws.Cell(row, 3).Value = item.Kunde;
                ws.Cell(row, 4).Value = item.Projekt;
                ws.Cell(row, 5).Value = item.Artikel;
                ws.Cell(row, 6).Value = item.Dekor;
                ws.Cell(row, 7).Value = item.Eintraege;
                ws.Cell(row, 8).Value = item.Chargen;
                ws.Cell(row, 9).Value = item.ChargeBeispiele;
                ws.Cell(row, 10).Value = item.Gutteile;
                ws.Cell(row, 11).Value = item.SchlechtExtern;
                ws.Cell(row, 12).Value = item.SchlechtIntern;
                ws.Cell(row, 13).Value = item.Schlechtteile;
                ws.Cell(row, 14).Value = item.Gesamt;
                SetPercentCell(ws.Cell(row, 15), item.Ausschussquote);
                SetPercentCell(ws.Cell(row, 16), item.Gutquote);
                SetPercentCell(ws.Cell(row, 17), item.ExternQuote);
                SetPercentCell(ws.Cell(row, 18), item.InternQuote);
                row++;
            }

            if (rows.Any())
            {
                var topHeaderRow = row + 2;
                ws.Range(topHeaderRow, 1, topHeaderRow, 6).Merge().Value = "Top Fehler je Zeile";
                ApplySectionStyle(ws.Range(topHeaderRow, 1, topHeaderRow, 6));
                WriteHeaderRow(ws, topHeaderRow + 1, new[] { "Mitarbeiter", "Artikel", "Dekor", "Top Fehler 1", "Top Fehler 2", "Top Fehler 3" }, 1, 6);

                var topRow = topHeaderRow + 2;
                foreach (var item in rows)
                {
                    ws.Cell(topRow, 1).Value = item.Mitarbeiter;
                    ws.Cell(topRow, 2).Value = item.Artikel;
                    ws.Cell(topRow, 3).Value = item.Dekor;
                    ws.Cell(topRow, 4).Value = item.TopFehler1;
                    ws.Cell(topRow, 5).Value = item.TopFehler2;
                    ws.Cell(topRow, 6).Value = item.TopFehler3;
                    topRow++;
                }
            }

            FinalizeWorksheet(ws, 18, true, freezeRow: headerRow);
        }

        private static void BuildMaterialWorksheet(XLWorkbook workbook, IReadOnlyList<MaterialExportRow> rows)
        {
            var ws = workbook.Worksheets.Add("Materialvergleich");
            ws.Range("A1:Q1").Merge().Value = "Materialvergleich für QS";
            ApplyTitleStyle(ws.Range("A1:Q1"), XLColor.FromHtml("#B45309"));
            ws.Range("A2:Q2").Merge().Value = "Zeigt pro Material, wie stark die Mitarbeiterquoten auseinanderliegen und wo QS zuerst hinschauen sollte.";
            ApplySubtitleStyle(ws.Range("A2:Q2"));

            var headerRow = 4;
            string[] headers =
            {
                "Kunde", "Projekt", "Artikel", "Dekor", "Mitarbeitende", "Einträge", "Gutteile", "Schlecht extern", "Schlecht intern",
                "Schlechtteile", "Gesamt", "Ausschuss %", "Extern %", "Intern %", "Beste Quote", "Schlechteste Quote", "Quotenspanne", "Mitarbeiter im Vergleich", "Häufigster Fehler"
            };
            WriteHeaderRow(ws, headerRow, headers, 1, 19);

            var row = headerRow + 1;
            foreach (var item in rows)
            {
                ws.Cell(row, 1).Value = item.Kunde;
                ws.Cell(row, 2).Value = item.Projekt;
                ws.Cell(row, 3).Value = item.Artikel;
                ws.Cell(row, 4).Value = item.Dekor;
                ws.Cell(row, 5).Value = item.Mitarbeitende;
                ws.Cell(row, 6).Value = item.Eintraege;
                ws.Cell(row, 7).Value = item.Gutteile;
                ws.Cell(row, 8).Value = item.SchlechtExtern;
                ws.Cell(row, 9).Value = item.SchlechtIntern;
                ws.Cell(row, 10).Value = item.Schlechtteile;
                ws.Cell(row, 11).Value = item.Gesamt;
                SetPercentCell(ws.Cell(row, 12), item.Ausschussquote);
                SetPercentCell(ws.Cell(row, 13), item.ExternQuote);
                SetPercentCell(ws.Cell(row, 14), item.InternQuote);
                SetPercentCell(ws.Cell(row, 15), item.BesteQuote);
                SetPercentCell(ws.Cell(row, 16), item.SchlechtesteQuote);
                SetPercentCell(ws.Cell(row, 17), item.Quotenspanne);
                ws.Cell(row, 18).Value = item.AuffaelligerMitarbeiter;
                ws.Cell(row, 19).Value = item.HaeufigsterFehler;
                row++;
            }

            FinalizeWorksheet(ws, 19, true, freezeRow: headerRow);
        }

        private static void BuildEintraegeWorksheet(XLWorkbook workbook, IReadOnlyList<FehlerRow> rows)
        {
            var ws = workbook.Worksheets.Add("Einzelne Einträge");
            ws.Range("A1:AC1").Merge().Value = "Einzelne Einträge";
            ApplyTitleStyle(ws.Range("A1:AC1"), XLColor.FromHtml("#7C3AED"));
            ws.Range("A2:AC2").Merge().Value = "Rohdaten mit Summen- und Prozentspalten für QS, Rückverfolgung und Detailprüfungen.";
            ApplySubtitleStyle(ws.Range("A2:AC2"));

            var headerRow = 4;
            string[] headers =
            {
                "Datum", "Charge", "Kunde", "Projekt", "Artikel", "Dekor", "Personalnr.", "Mitarbeiter", "Gutteile",
                "Fusseln", "Nadelstiche", "Pickel", "Dekorfehler", "Farbfehler", "Flecken", "Nebel", "Vertiefung",
                "Ölflecken", "Tiefziehfehler", "Stanz-/Fräsfehler", "Knicke", "Kratzer", "Schlecht extern", "Schlecht intern",
                "Schlechtteile", "Gesamt", "Ausschuss %", "Gut %", "Hauptfehler", "Bemerkungen"
            };
            WriteHeaderRow(ws, headerRow, headers, 1, 30);

            var row = headerRow + 1;
            foreach (var item in rows)
            {
                ws.Cell(row, 1).Value = item.FSKdate;
                ws.Cell(row, 1).Style.DateFormat.Format = "dd.MM.yyyy";
                ws.Cell(row, 2).Value = item.Charge;
                ws.Cell(row, 3).Value = item.Kunde;
                ws.Cell(row, 4).Value = item.Projekt;
                ws.Cell(row, 5).Value = item.Artikel;
                ws.Cell(row, 6).Value = item.Dekor;
                ws.Cell(row, 7).Value = item.Personalnummer;
                ws.Cell(row, 8).Value = item.PersonalName;
                ws.Cell(row, 9).Value = item.Gutteile;
                ws.Cell(row, 10).Value = item.Fusseln;
                ws.Cell(row, 11).Value = item.Nadelstiche;
                ws.Cell(row, 12).Value = item.Pickel;
                ws.Cell(row, 13).Value = item.Dekorfehler;
                ws.Cell(row, 14).Value = item.Farbfehler;
                ws.Cell(row, 15).Value = item.Flecken;
                ws.Cell(row, 16).Value = item.Nebel;
                ws.Cell(row, 17).Value = item.Vertiefung;
                ws.Cell(row, 18).Value = item.Oelflecken;
                ws.Cell(row, 19).Value = item.Tiefziehfehler;
                ws.Cell(row, 20).Value = item.Fraesfehler;
                ws.Cell(row, 21).Value = item.Knicke;
                ws.Cell(row, 22).Value = item.Kratzer;
                ws.Cell(row, 23).Value = item.SchlechtExtern;
                ws.Cell(row, 24).Value = item.SchlechtIntern;
                ws.Cell(row, 25).Value = item.Schlechtteile;
                ws.Cell(row, 26).Value = item.Gesamt;
                SetPercentCell(ws.Cell(row, 27), CalculateQuote(item.Schlechtteile, item.Gesamt));
                SetPercentCell(ws.Cell(row, 28), CalculateQuote(item.Gutteile, item.Gesamt));
                ws.Cell(row, 29).Value = GetTopFehlerLabels(item, 1).FirstOrDefault() ?? "-";
                ws.Cell(row, 30).Value = item.Bemerkungen;
                row++;
            }

            ws.Column(30).Style.Alignment.WrapText = true;
            FinalizeWorksheet(ws, 30, true, freezeRow: headerRow);
        }

        private static void BuildOverviewWorksheetReadable(
            XLWorkbook workbook,
            FehleranalyseExportRequest request,
            DateTime createdAt,
            FehlerAnalyseResult gesamt,
            IReadOnlyList<FehlerartExportRow> fehlerarten,
            IReadOnlyList<FehleranalyseMitarbeiterMaterialRow> auffaelligeKombinationen)
        {
            var ws = workbook.Worksheets.Add("QS-Übersicht");
            ws.Range("A1:L1").Merge().Value = "Fehleranalyse QS-Export";
            ApplyTitleStyle(ws.Range("A1:L1"), XLColor.FromHtml("#0F4C81"));
            ws.Range("A2:L2").Merge().Value = $"Erstellt am {createdAt:dd.MM.yyyy HH:mm}";
            ApplySubtitleStyle(ws.Range("A2:L2"));

            ws.Range("A4:F4").Merge().Value = "Ausgewählter Bereich";
            ApplySectionStyle(ws.Range("A4:F4"));
            WriteFilterRowExtended(ws, 5, "Zeitraum", $"{request.StartDatum:dd.MM.yyyy} bis {request.EndDatum:dd.MM.yyyy}", 1, 6);
            WriteFilterRowExtended(ws, 6, "Charge", request.Charge, 1, 6);
            WriteFilterRowExtended(ws, 7, "Kunde", request.Kunde, 1, 6);
            WriteFilterRowExtended(ws, 8, "Projekt", request.Projekt, 1, 6);
            WriteFilterRowExtended(ws, 9, "Artikel", request.Artikel, 1, 6);
            WriteFilterRowExtended(ws, 10, "Dekor", request.Dekor, 1, 6);

            ws.Range("G4:L4").Merge().Value = "Gesamtwerte";
            ApplySectionStyle(ws.Range("G4:L4"));
            WriteMetricCard(ws, 5, 7, 6, 8, "Gutteile", gesamt.Gutteile, XLColor.FromHtml("#DCFCE7"));
            WriteMetricCard(ws, 5, 9, 6, 10, "Schlechtteile", gesamt.Schlechtteile, XLColor.FromHtml("#FEE2E2"));
            WriteMetricCard(ws, 5, 11, 6, 12, "Gesamt", gesamt.Gesamt, XLColor.FromHtml("#DBEAFE"));
            WriteMetricCard(ws, 8, 7, 9, 8, "Schlecht extern", gesamt.SchlechtExtern, XLColor.FromHtml("#FFEDD5"));
            WriteMetricCard(ws, 8, 9, 9, 10, "Schlecht intern", gesamt.SchlechtIntern, XLColor.FromHtml("#EDE9FE"));
            WriteMetricCard(ws, 8, 11, 9, 12, "Einträge", request.Eintraege.Count, XLColor.FromHtml("#E0F2FE"));

            ws.Range("A12:L12").Merge().Value = "Prozentwerte";
            ApplySectionStyle(ws.Range("A12:L12"));
            WriteMetricCard(ws, 13, 1, 14, 3, "Gutteile %", CalculateQuote(gesamt.Gutteile, gesamt.Gesamt), XLColor.FromHtml("#DCFCE7"), isPercent: true);
            WriteMetricCard(ws, 13, 4, 14, 6, "Schlechtteile %", CalculateQuote(gesamt.Schlechtteile, gesamt.Gesamt), XLColor.FromHtml("#FEE2E2"), isPercent: true);
            WriteMetricCard(ws, 13, 7, 14, 9, "Schlecht extern %", CalculateQuote(gesamt.SchlechtExtern, gesamt.Gesamt), XLColor.FromHtml("#FFEDD5"), isPercent: true);
            WriteMetricCard(ws, 13, 10, 14, 12, "Schlecht intern %", CalculateQuote(gesamt.SchlechtIntern, gesamt.Gesamt), XLColor.FromHtml("#EDE9FE"), isPercent: true);

            var row = 17;
            ws.Range(row, 1, row, 12).Merge().Value = "QS-Vergleich nach Mitarbeiter und Material";
            ApplySectionStyle(ws.Range(row, 1, row, 12));
            row++;

            WriteHeaderRow(ws, row, new[]
            {
                "Mitarbeiter", "Personalnr.", "Artikel", "Dekor", "Gutteile", "Schlechtteile", "Schlechtteile %", "Schlecht extern", "Schlecht extern %", "Schlecht intern", "Schlecht intern %", "Top Fehler"
            });
            row++;

            if (!auffaelligeKombinationen.Any())
            {
                ws.Range(row, 1, row, 12).Merge().Value = "Keine auswertbaren Kombinationen vorhanden.";
                ApplyEmptyRowStyle(ws.Range(row, 1, row, 12));
                row++;
            }
            else
            {
                foreach (var item in auffaelligeKombinationen)
                {
                    ws.Cell(row, 1).Value = item.Mitarbeiter;
                    ws.Cell(row, 2).Value = item.Personalnummer;
                    ws.Cell(row, 3).Value = item.Artikel;
                    ws.Cell(row, 4).Value = item.Dekor;
                    ws.Cell(row, 5).Value = item.Gutteile;
                    ws.Cell(row, 6).Value = item.Schlechtteile;
                    SetPercentCell(ws.Cell(row, 7), item.Ausschussquote);
                    ws.Cell(row, 8).Value = item.SchlechtExtern;
                    SetPercentCell(ws.Cell(row, 9), item.ExternQuote);
                    ws.Cell(row, 10).Value = item.SchlechtIntern;
                    SetPercentCell(ws.Cell(row, 11), item.InternQuote);
                    ws.Cell(row, 12).Value = JoinTopFehler(item.TopFehler1, item.TopFehler2, item.TopFehler3);
                    row++;
                }
            }

            ws.Range(row, 1, row, 12).Merge().Value = "Fehlerarten nach Häufigkeit";
            ApplySectionStyle(ws.Range(row, 1, row, 12));
            row++;

            WriteHeaderRow(ws, row, new[]
            {
                "Bereich", "Fehlerart", "Anzahl", "Schlechtteile % an Gesamt", "Anteil an allen Schlechtteilen"
            }, 1, 5);
            row++;

            foreach (var fehler in fehlerarten)
            {
                ws.Cell(row, 1).Value = fehler.Bereich;
                ws.Cell(row, 2).Value = fehler.Fehlerart;
                ws.Cell(row, 3).Value = fehler.Anzahl;
                SetPercentCell(ws.Cell(row, 4), fehler.AnteilGesamtmenge);
                SetPercentCell(ws.Cell(row, 5), fehler.AnteilSchlechtteile);
                row++;
            }

            FinalizeWorksheet(ws, 12, false);
        }

        private static void BuildMitarbeiterWorksheetReadable(XLWorkbook workbook, IReadOnlyList<FehleranalyseMitarbeiterMaterialRow> rows)
        {
            var ws = workbook.Worksheets.Add("Mitarbeitervergleich");
            ws.Range("A1:R1").Merge().Value = "Mitarbeitervergleich je Material";
            ApplyTitleStyle(ws.Range("A1:R1"), XLColor.FromHtml("#0F766E"));
            ws.Range("A2:R2").Merge().Value = "Vergleich pro Mitarbeiter und Material mit klaren Schlechtteile-Werten und Prozenten.";
            ApplySubtitleStyle(ws.Range("A2:R2"));

            const int headerRow = 4;
            WriteHeaderRow(ws, headerRow, new[]
            {
                "Mitarbeiter", "Personalnr.", "Kunde", "Projekt", "Artikel", "Dekor", "Einträge", "Chargen", "Charge-Beispiele",
                "Gutteile", "Schlecht extern", "Schlecht intern", "Schlechtteile", "Gesamt", "Schlechtteile %", "Gutteile %", "Schlecht extern %", "Schlecht intern %"
            });

            var row = headerRow + 1;
            foreach (var item in rows)
            {
                ws.Cell(row, 1).Value = item.Mitarbeiter;
                ws.Cell(row, 2).Value = item.Personalnummer;
                ws.Cell(row, 3).Value = item.Kunde;
                ws.Cell(row, 4).Value = item.Projekt;
                ws.Cell(row, 5).Value = item.Artikel;
                ws.Cell(row, 6).Value = item.Dekor;
                ws.Cell(row, 7).Value = item.Eintraege;
                ws.Cell(row, 8).Value = item.Chargen;
                ws.Cell(row, 9).Value = item.ChargeBeispiele;
                ws.Cell(row, 10).Value = item.Gutteile;
                ws.Cell(row, 11).Value = item.SchlechtExtern;
                ws.Cell(row, 12).Value = item.SchlechtIntern;
                ws.Cell(row, 13).Value = item.Schlechtteile;
                ws.Cell(row, 14).Value = item.Gesamt;
                SetPercentCell(ws.Cell(row, 15), item.Ausschussquote);
                SetPercentCell(ws.Cell(row, 16), item.Gutquote);
                SetPercentCell(ws.Cell(row, 17), item.ExternQuote);
                SetPercentCell(ws.Cell(row, 18), item.InternQuote);
                row++;
            }

            if (rows.Any())
            {
                var topHeaderRow = row + 2;
                ws.Range(topHeaderRow, 1, topHeaderRow, 6).Merge().Value = "Wichtigste Fehler je Zeile";
                ApplySectionStyle(ws.Range(topHeaderRow, 1, topHeaderRow, 6));
                WriteHeaderRow(ws, topHeaderRow + 1, new[] { "Mitarbeiter", "Artikel", "Dekor", "Top Fehler 1", "Top Fehler 2", "Top Fehler 3" }, 1, 6);

                var topRow = topHeaderRow + 2;
                foreach (var item in rows)
                {
                    ws.Cell(topRow, 1).Value = item.Mitarbeiter;
                    ws.Cell(topRow, 2).Value = item.Artikel;
                    ws.Cell(topRow, 3).Value = item.Dekor;
                    ws.Cell(topRow, 4).Value = item.TopFehler1;
                    ws.Cell(topRow, 5).Value = item.TopFehler2;
                    ws.Cell(topRow, 6).Value = item.TopFehler3;
                    topRow++;
                }
            }

            FinalizeWorksheet(ws, 18, false, freezeRow: headerRow);
        }

        private static void BuildMaterialWorksheetReadable(XLWorkbook workbook, IReadOnlyList<MaterialExportRow> rows)
        {
            var ws = workbook.Worksheets.Add("Materialvergleich");
            ws.Range("A1:Q1").Merge().Value = "Materialvergleich für QS";
            ApplyTitleStyle(ws.Range("A1:Q1"), XLColor.FromHtml("#B45309"));
            ws.Range("A2:Q2").Merge().Value = "Zeigt pro Material, wie stark die Schlechtteile-Werte zwischen Mitarbeitern auseinanderliegen.";
            ApplySubtitleStyle(ws.Range("A2:Q2"));

            const int headerRow = 4;
            WriteHeaderRow(ws, headerRow, new[]
            {
                "Kunde", "Projekt", "Artikel", "Dekor", "Mitarbeitende", "Einträge", "Gutteile", "Schlecht extern", "Schlecht intern",
                "Schlechtteile", "Gesamt", "Schlechtteile %", "Schlecht extern %", "Schlecht intern %", "Beste Schlechtteile %", "Schlechteste Schlechtteile %", "Abweichung %", "Mitarbeiter im Vergleich", "Häufigster Fehler"
            }, 1, 19);

            var row = headerRow + 1;
            foreach (var item in rows)
            {
                ws.Cell(row, 1).Value = item.Kunde;
                ws.Cell(row, 2).Value = item.Projekt;
                ws.Cell(row, 3).Value = item.Artikel;
                ws.Cell(row, 4).Value = item.Dekor;
                ws.Cell(row, 5).Value = item.Mitarbeitende;
                ws.Cell(row, 6).Value = item.Eintraege;
                ws.Cell(row, 7).Value = item.Gutteile;
                ws.Cell(row, 8).Value = item.SchlechtExtern;
                ws.Cell(row, 9).Value = item.SchlechtIntern;
                ws.Cell(row, 10).Value = item.Schlechtteile;
                ws.Cell(row, 11).Value = item.Gesamt;
                SetPercentCell(ws.Cell(row, 12), item.Ausschussquote);
                SetPercentCell(ws.Cell(row, 13), item.ExternQuote);
                SetPercentCell(ws.Cell(row, 14), item.InternQuote);
                SetPercentCell(ws.Cell(row, 15), item.BesteQuote);
                SetPercentCell(ws.Cell(row, 16), item.SchlechtesteQuote);
                SetPercentCell(ws.Cell(row, 17), item.Quotenspanne);
                ws.Cell(row, 18).Value = item.AuffaelligerMitarbeiter;
                ws.Cell(row, 19).Value = item.HaeufigsterFehler;
                row++;
            }

            FinalizeWorksheet(ws, 19, false, freezeRow: headerRow);
        }

        private static void BuildEintraegeWorksheetReadable(XLWorkbook workbook, IReadOnlyList<FehlerRow> rows)
        {
            var ws = workbook.Worksheets.Add("Einzelne Einträge");
            ws.Range("A1:AF1").Merge().Value = "Einzelne Einträge";
            ApplyTitleStyle(ws.Range("A1:AF1"), XLColor.FromHtml("#7C3AED"));
            ws.Range("A2:AF2").Merge().Value = "Rohdaten mit allen Schlechtteile-Werten und klaren Prozentspalten.";
            ApplySubtitleStyle(ws.Range("A2:AF2"));

            const int headerRow = 4;
            WriteHeaderRow(ws, headerRow, new[]
            {
                "Datum", "Charge", "Kunde", "Projekt", "Artikel", "Dekor", "Personalnr.", "Mitarbeiter", "Gutteile",
                "Fusseln", "Nadelstiche", "Pickel", "Dekorfehler", "Farbfehler", "Flecken", "Nebel", "Vertiefung",
                "Ölflecken", "Tiefziehfehler", "Stanz-/Fräsfehler", "Knicke", "Kratzer", "Schlecht extern", "Schlecht intern",
                "Schlechtteile", "Gesamt", "Schlechtteile %", "Schlecht extern %", "Schlecht intern %", "Gutteile %", "Hauptfehler", "Bemerkungen"
            }, 1, 32);

            var row = headerRow + 1;
            foreach (var item in rows)
            {
                ws.Cell(row, 1).Value = item.FSKdate;
                ws.Cell(row, 1).Style.DateFormat.Format = "dd.MM.yyyy";
                ws.Cell(row, 2).Value = item.Charge;
                ws.Cell(row, 3).Value = item.Kunde;
                ws.Cell(row, 4).Value = item.Projekt;
                ws.Cell(row, 5).Value = item.Artikel;
                ws.Cell(row, 6).Value = item.Dekor;
                ws.Cell(row, 7).Value = item.Personalnummer;
                ws.Cell(row, 8).Value = item.PersonalName;
                ws.Cell(row, 9).Value = item.Gutteile;
                ws.Cell(row, 10).Value = item.Fusseln;
                ws.Cell(row, 11).Value = item.Nadelstiche;
                ws.Cell(row, 12).Value = item.Pickel;
                ws.Cell(row, 13).Value = item.Dekorfehler;
                ws.Cell(row, 14).Value = item.Farbfehler;
                ws.Cell(row, 15).Value = item.Flecken;
                ws.Cell(row, 16).Value = item.Nebel;
                ws.Cell(row, 17).Value = item.Vertiefung;
                ws.Cell(row, 18).Value = item.Oelflecken;
                ws.Cell(row, 19).Value = item.Tiefziehfehler;
                ws.Cell(row, 20).Value = item.Fraesfehler;
                ws.Cell(row, 21).Value = item.Knicke;
                ws.Cell(row, 22).Value = item.Kratzer;
                ws.Cell(row, 23).Value = item.SchlechtExtern;
                ws.Cell(row, 24).Value = item.SchlechtIntern;
                ws.Cell(row, 25).Value = item.Schlechtteile;
                ws.Cell(row, 26).Value = item.Gesamt;
                SetPercentCell(ws.Cell(row, 27), CalculateQuote(item.Schlechtteile, item.Gesamt));
                SetPercentCell(ws.Cell(row, 28), CalculateQuote(item.SchlechtExtern, item.Gesamt));
                SetPercentCell(ws.Cell(row, 29), CalculateQuote(item.SchlechtIntern, item.Gesamt));
                SetPercentCell(ws.Cell(row, 30), CalculateQuote(item.Gutteile, item.Gesamt));
                ws.Cell(row, 31).Value = GetTopFehlerLabels(item, 1).FirstOrDefault() ?? "-";
                ws.Cell(row, 32).Value = item.Bemerkungen;
                row++;
            }

            ws.Column(32).Style.Alignment.WrapText = true;
            FinalizeWorksheet(ws, 32, false, freezeRow: headerRow);
        }

        private static List<FehlerartExportRow> CreateFehlerartRows(FehlerAnalyseResult result)
        {
            return FehlerartenDefinitionen
                .Select(definition =>
                {
                    var anzahl = definition.Selector(result);
                    return new FehlerartExportRow
                    {
                        Bereich = definition.Bereich,
                        Fehlerart = definition.Name,
                        Anzahl = anzahl,
                        AnteilGesamtmenge = CalculateQuote(anzahl, result.Gesamt),
                        AnteilSchlechtteile = CalculateQuote(anzahl, result.Schlechtteile)
                    };
                })
                .Where(row => row.Anzahl > 0)
                .OrderByDescending(row => row.Anzahl)
                .ThenBy(row => row.Fehlerart, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        private static List<FehleranalyseMitarbeiterMaterialRow> CreateMitarbeiterExportRows(IReadOnlyCollection<FehlerRow> rows)
        {
            return rows
                .GroupBy(row => new
                {
                    Mitarbeiter = NormalizeText(row.PersonalName, row.Personalnummer),
                    Personalnummer = NormalizeText(row.Personalnummer),
                    Kunde = NormalizeText(row.Kunde),
                    Projekt = NormalizeText(row.Projekt),
                    Artikel = NormalizeText(row.Artikel),
                    Dekor = NormalizeText(row.Dekor)
                })
                .Select(group =>
                {
                    var summary = SummarizeResults(group);
                    var topFehler = GetTopFehlerLabels(summary, 3);
                    var chargen = group
                        .Select(row => NormalizeText(row.Charge))
                        .Where(value => value != "-")
                        .Distinct(StringComparer.CurrentCultureIgnoreCase)
                        .ToList();

                    return new FehleranalyseMitarbeiterMaterialRow
                    {
                        Mitarbeiter = group.Key.Mitarbeiter,
                        Personalnummer = group.Key.Personalnummer,
                        Kunde = group.Key.Kunde,
                        Projekt = group.Key.Projekt,
                        Artikel = group.Key.Artikel,
                        Dekor = group.Key.Dekor,
                        Eintraege = group.Count(),
                        Chargen = chargen.Count,
                        ChargeBeispiele = BuildExampleList(chargen),
                        Gutteile = summary.Gutteile,
                        SchlechtExtern = summary.SchlechtExtern,
                        SchlechtIntern = summary.SchlechtIntern,
                        Schlechtteile = summary.Schlechtteile,
                        Gesamt = summary.Gesamt,
                        Ausschussquote = CalculateQuote(summary.Schlechtteile, summary.Gesamt),
                        Gutquote = CalculateQuote(summary.Gutteile, summary.Gesamt),
                        ExternQuote = CalculateQuote(summary.SchlechtExtern, summary.Gesamt),
                        InternQuote = CalculateQuote(summary.SchlechtIntern, summary.Gesamt),
                        TopFehler1 = topFehler.ElementAtOrDefault(0) ?? "-",
                        TopFehler2 = topFehler.ElementAtOrDefault(1) ?? "-",
                        TopFehler3 = topFehler.ElementAtOrDefault(2) ?? "-"
                    };
                })
                .OrderByDescending(row => row.Ausschussquote)
                .ThenByDescending(row => row.Schlechtteile)
                .ThenByDescending(row => row.Gesamt)
                .ThenBy(row => row.Mitarbeiter, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        private static List<MaterialExportRow> CreateMaterialExportRows(IReadOnlyCollection<FehlerRow> rows, IReadOnlyCollection<FehleranalyseMitarbeiterMaterialRow> mitarbeiterRows)
        {
            return rows
                .GroupBy(row => new
                {
                    Kunde = NormalizeText(row.Kunde),
                    Projekt = NormalizeText(row.Projekt),
                    Artikel = NormalizeText(row.Artikel),
                    Dekor = NormalizeText(row.Dekor)
                })
                .Select(group =>
                {
                    var summary = SummarizeResults(group);
                    var relatedEmployees = mitarbeiterRows
                        .Where(item =>
                            string.Equals(item.Kunde, group.Key.Kunde, StringComparison.CurrentCultureIgnoreCase) &&
                            string.Equals(item.Projekt, group.Key.Projekt, StringComparison.CurrentCultureIgnoreCase) &&
                            string.Equals(item.Artikel, group.Key.Artikel, StringComparison.CurrentCultureIgnoreCase) &&
                            string.Equals(item.Dekor, group.Key.Dekor, StringComparison.CurrentCultureIgnoreCase))
                        .ToList();

                    var bestEmployee = relatedEmployees
                        .OrderBy(item => item.Ausschussquote)
                        .ThenByDescending(item => item.Gesamt)
                        .FirstOrDefault();

                    var worstEmployee = relatedEmployees
                        .OrderByDescending(item => item.Ausschussquote)
                        .ThenByDescending(item => item.Schlechtteile)
                        .FirstOrDefault();

                    return new MaterialExportRow
                    {
                        Kunde = group.Key.Kunde,
                        Projekt = group.Key.Projekt,
                        Artikel = group.Key.Artikel,
                        Dekor = group.Key.Dekor,
                        Mitarbeitende = relatedEmployees.Select(item => item.Personalnummer + "|" + item.Mitarbeiter).Distinct(StringComparer.CurrentCultureIgnoreCase).Count(),
                        Eintraege = group.Count(),
                        Gutteile = summary.Gutteile,
                        SchlechtExtern = summary.SchlechtExtern,
                        SchlechtIntern = summary.SchlechtIntern,
                        Schlechtteile = summary.Schlechtteile,
                        Gesamt = summary.Gesamt,
                        Ausschussquote = CalculateQuote(summary.Schlechtteile, summary.Gesamt),
                        ExternQuote = CalculateQuote(summary.SchlechtExtern, summary.Gesamt),
                        InternQuote = CalculateQuote(summary.SchlechtIntern, summary.Gesamt),
                        BesteQuote = bestEmployee?.Ausschussquote ?? 0d,
                        SchlechtesteQuote = worstEmployee?.Ausschussquote ?? 0d,
                        Quotenspanne = Math.Max((worstEmployee?.Ausschussquote ?? 0d) - (bestEmployee?.Ausschussquote ?? 0d), 0d),
                        AuffaelligerMitarbeiter = worstEmployee?.Mitarbeiter ?? "-",
                        HaeufigsterFehler = GetTopFehlerLabels(summary, 1).FirstOrDefault() ?? "-"
                    };
                })
                .OrderByDescending(row => row.Quotenspanne)
                .ThenByDescending(row => row.Ausschussquote)
                .ThenByDescending(row => row.Schlechtteile)
                .ThenBy(row => row.Artikel, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        private static FehlerAnalyseResult SummarizeResults(IEnumerable<FehlerAnalyseResult> rows)
        {
            return new FehlerAnalyseResult
            {
                Fusseln = rows.Sum(item => item.Fusseln),
                Nadelstiche = rows.Sum(item => item.Nadelstiche),
                Pickel = rows.Sum(item => item.Pickel),
                Dekorfehler = rows.Sum(item => item.Dekorfehler),
                Farbfehler = rows.Sum(item => item.Farbfehler),
                Flecken = rows.Sum(item => item.Flecken),
                Nebel = rows.Sum(item => item.Nebel),
                Vertiefung = rows.Sum(item => item.Vertiefung),
                Oelflecken = rows.Sum(item => item.Oelflecken),
                Tiefziehfehler = rows.Sum(item => item.Tiefziehfehler),
                Fraesfehler = rows.Sum(item => item.Fraesfehler),
                Knicke = rows.Sum(item => item.Knicke),
                Kratzer = rows.Sum(item => item.Kratzer),
                Gutteile = rows.Sum(item => item.Gutteile)
            };
        }

        private static List<string> GetTopFehlerLabels(FehlerAnalyseResult result, int maxCount)
        {
            return FehlerartenDefinitionen
                .Select(definition => new
                {
                    definition.Name,
                    Anzahl = definition.Selector(result)
                })
                .Where(item => item.Anzahl > 0)
                .OrderByDescending(item => item.Anzahl)
                .ThenBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
                .Take(maxCount)
                .Select(item => $"{item.Name} ({item.Anzahl.ToString("N0", ExportCulture)})")
                .ToList();
        }

        private static string BuildExampleList(IReadOnlyList<string> values)
        {
            if (values.Count == 0)
            {
                return "-";
            }

            const int maxVisible = 3;
            var examples = values.Take(maxVisible).ToList();
            var result = string.Join(", ", examples);
            return values.Count > maxVisible ? $"{result} +{values.Count - maxVisible}" : result;
        }

        private static string JoinTopFehler(params string[] values)
        {
            var filtered = values
                .Where(value => !string.IsNullOrWhiteSpace(value) && value != "-")
                .ToList();

            return filtered.Count == 0 ? "-" : string.Join(" | ", filtered);
        }

        private static double CalculateQuote(int value, int total) => total > 0 ? (double)value / total : 0d;

        private static string NormalizeText(string? value, string? fallback = null)
        {
            var text = string.IsNullOrWhiteSpace(value) ? fallback : value;
            return string.IsNullOrWhiteSpace(text) ? "-" : text.Trim();
        }

        private static string BuildExportFileName(FehleranalyseExportRequest request, DateTime createdAt)
        {
            if (request.StartDatum.Date == request.EndDatum.Date)
            {
                return $"Fehleranalyse_QS_{request.StartDatum:yyyy-MM-dd}.xlsx";
            }

            return $"Fehleranalyse_QS_{request.StartDatum:yyyy-MM-dd}_bis_{request.EndDatum:yyyy-MM-dd}_{createdAt:HHmm}.xlsx";
        }

        private static void WriteFilterRow(IXLWorksheet ws, int row, string label, string? value)
        {
            ws.Cell(row, 1).Value = label;
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 2).Value = string.IsNullOrWhiteSpace(value) ? "Alle" : value;
            ws.Range(row, 1, row, 4).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            ws.Range(row, 1, row, 4).Style.Border.BottomBorderColor = XLColor.FromHtml("#D6DEE8");
        }

        private static void WriteFilterRow(IXLWorksheet ws, int row, string label, string? value, int startColumn, int endColumn)
        {
            WriteFilterRowExtended(ws, row, label, value, startColumn, endColumn);
        }

        private static void WriteFilterRowExtended(IXLWorksheet ws, int row, string label, string? value, int startColumn, int endColumn)
        {
            ws.Cell(row, startColumn).Value = label;
            ws.Cell(row, startColumn).Style.Font.Bold = true;
            ws.Cell(row, startColumn).Style.Font.FontColor = XLColor.FromHtml("#334155");

            ws.Range(row, startColumn + 1, row, endColumn).Merge().Value = string.IsNullOrWhiteSpace(value) ? "Alle" : value;
            ws.Range(row, startColumn, row, endColumn).Style.Fill.BackgroundColor = XLColor.White;
            ws.Range(row, startColumn, row, endColumn).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            ws.Range(row, startColumn, row, endColumn).Style.Border.BottomBorderColor = XLColor.FromHtml("#D6DEE8");
        }

        private static void WriteMetricCard(
            IXLWorksheet ws,
            int firstRow,
            int firstColumn,
            int lastRow,
            int lastColumn,
            string label,
            object value,
            XLColor backgroundColor,
            bool isPercent = false,
            string? emptyText = null)
        {
            var labelRange = ws.Range(firstRow, firstColumn, firstRow, lastColumn);
            labelRange.Merge().Value = label;
            labelRange.Style.Font.Bold = true;
            labelRange.Style.Font.FontColor = XLColor.FromHtml("#334155");
            labelRange.Style.Fill.BackgroundColor = backgroundColor;
            labelRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            var valueRange = ws.Range(firstRow + 1, firstColumn, lastRow, lastColumn);
            valueRange.Merge();
            valueRange.Style.Fill.BackgroundColor = backgroundColor;
            valueRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            valueRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            valueRange.Style.Font.FontSize = 16;
            valueRange.Style.Font.Bold = true;
            valueRange.Style.Font.FontColor = XLColor.FromHtml("#0F172A");

            var valueCell = ws.Cell(firstRow + 1, firstColumn);
            if (isPercent && value is double percentValue)
            {
                SetPercentCell(valueCell, percentValue);
            }
            else if (!isPercent && value is int intValue)
            {
                valueCell.Value = intValue;
            }
            else if (!isPercent && value is double doubleValue)
            {
                valueCell.Value = doubleValue;
            }
            else
            {
                valueCell.Value = emptyText ?? value?.ToString() ?? string.Empty;
            }

            if (emptyText != null && value is int numericValue && numericValue == 0)
            {
                valueCell.Value = emptyText;
            }
        }

        private static void WriteMetricRow(
            IXLWorksheet ws,
            int row,
            string label1,
            object value1,
            string label2,
            object value2,
            string label3,
            object value3,
            bool value1IsPercent = false,
            bool value2IsPercent = false,
            bool value3IsPercent = false,
            bool percentFirst = false)
        {
            WriteMetricCell(ws, row, 6, label1, value1, value1IsPercent || percentFirst);
            WriteMetricCell(ws, row, 8, label2, value2, value2IsPercent);
            WriteMetricCell(ws, row, 10, label3, value3, value3IsPercent);
        }

        private static void WriteMetricCell(IXLWorksheet ws, int row, int column, string label, object value, bool isPercent = false)
        {
            ws.Cell(row, column - 1).Value = label;
            ws.Cell(row, column - 1).Style.Font.Bold = true;

            var valueCell = ws.Cell(row, column);
            if (isPercent && value is double percentValue)
            {
                SetPercentCell(valueCell, percentValue);
            }
            else
            {
                switch (value)
                {
                    case int intValue:
                        valueCell.Value = intValue;
                        break;
                    case double doubleValue:
                        valueCell.Value = doubleValue;
                        break;
                    case string stringValue:
                        valueCell.Value = stringValue;
                        break;
                    default:
                        valueCell.Value = value?.ToString() ?? string.Empty;
                        break;
                }
            }

            ws.Range(row, column - 1, row, column).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            ws.Range(row, column - 1, row, column).Style.Border.BottomBorderColor = XLColor.FromHtml("#D6DEE8");
        }

        private static void WriteHeaderRow(IXLWorksheet ws, int row, IReadOnlyList<string> headers, int startColumn = 1, int? endColumn = null)
        {
            for (var index = 0; index < headers.Count; index++)
            {
                ws.Cell(row, startColumn + index).Value = headers[index];
            }

            var lastColumn = endColumn ?? (startColumn + headers.Count - 1);
            var range = ws.Range(row, startColumn, row, lastColumn);
            range.Style.Font.Bold = true;
            range.Style.Font.FontColor = XLColor.White;
            range.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F2937");
            range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            range.Style.Alignment.WrapText = true;
        }

        private static void SetPercentCell(IXLCell cell, double value)
        {
            cell.Value = value;
            cell.Style.NumberFormat.Format = "0.00%";
        }

        private static void ApplyTitleStyle(IXLRange range, XLColor color)
        {
            range.Style.Font.Bold = true;
            range.Style.Font.FontColor = XLColor.White;
            range.Style.Font.FontSize = 18;
            range.Style.Fill.BackgroundColor = color;
            range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            range.Style.Alignment.Indent = 1;
        }

        private static void ApplySubtitleStyle(IXLRange range)
        {
            range.Style.Font.FontColor = XLColor.FromHtml("#334155");
            range.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
            range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            range.Style.Alignment.Indent = 1;
        }

        private static void ApplySectionStyle(IXLRange range)
        {
            range.Style.Font.Bold = true;
            range.Style.Fill.BackgroundColor = XLColor.FromHtml("#DBEAFE");
            range.Style.Font.FontColor = XLColor.FromHtml("#1E3A8A");
            range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
        }

        private static void ApplyEmptyRowStyle(IXLRange range)
        {
            range.Style.Font.Italic = true;
            range.Style.Font.FontColor = XLColor.FromHtml("#64748B");
            range.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
            range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        private static void FinalizeWorksheet(IXLWorksheet ws, int lastColumn, bool landscape, int freezeRow = 4)
        {
            ws.SheetView.FreezeRows(freezeRow);

            var usedRange = ws.RangeUsed();
            if (usedRange != null)
            {
                usedRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                usedRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                usedRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                usedRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#E2E8F0");
                usedRange.Style.Border.InsideBorderColor = XLColor.FromHtml("#E2E8F0");
            }

            ws.Columns(1, lastColumn).AdjustToContents();

            if (lastColumn >= 9)
            {
                ws.Column(9).Width = Math.Min(ws.Column(9).Width, 28d);
            }

            if (lastColumn >= 30)
            {
                ws.Column(30).Width = 42d;
            }
        }

        private async Task<List<FehleranalyseSchichtplanZielRow>> GetSchichtplanZielRowsAsync(DateTime von, DateTime bis)
        {
            await SchichtplanSchemaService.EnsureZielSnapshotColumnsAsync(SqlManager.FertigungConnectionString);

            var rows = new List<FehleranalyseSchichtplanZielRow>();

            const string query = @"
WITH MaterialAssignments AS
(
    SELECT
        p.PlanDatum,
        COALESCE(m1.Material, e.Material) AS Material,
        CASE
            WHEN e.MaterialZielMenge IS NOT NULL THEN e.MaterialZielMenge
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
            WHEN e.MaterialZielMenge IS NOT NULL THEN e.MaterialZielMenge
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
            WHEN e.Material2ZielMenge IS NOT NULL THEN e.Material2ZielMenge
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
            WHEN e.Material2ZielMenge IS NOT NULL THEN e.Material2ZielMenge
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
