using ClosedXML.Excel;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
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

        public int SchlechtIntern => Fusseln + Nadelstiche + Pickel + Dekorfehler + Farbfehler + Flecken + Nebel + Vertiefung;
        public int SchlechtExtern => Oelflecken + Tiefziehfehler + Fraesfehler + Knicke + Kratzer;
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

    public class FehleranalyseService
    {
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
                             t.Oelflecken, t.Tiefziehfehler, t.Fraesfehler, t.Knicke, t.Kratzer, t.Gutteile, t.Artikel, t.Personalnummer, t.Dekor, t.Charge, t.Projekt, t.Kunde, l.Benutzer
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
                        Dekor = reader["Dekor"]?.ToString() ?? ""
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting fehler rows: {ex.Message}");
            }

            return daten;
        }

        public async Task<string> ExportToExcelAsync(List<FehlerRow> fehlerListe)
        {
            if (fehlerListe == null || !fehlerListe.Any())
                return "Keine Daten vorhanden";

            string exportDirectory = @"N:\tmp";
            if (!Directory.Exists(exportDirectory))
            {
                try { Directory.CreateDirectory(exportDirectory); }
                catch { exportDirectory = Path.GetTempPath(); } // Fallback to local temp if N: is missing
            }

            string filePath = Path.Combine(exportDirectory, $"Auswertung_{DateTime.Today:yyyy-MM-dd}.xlsx");

            try
            {
                using var workbook = new XLWorkbook();
                var ws = workbook.Worksheets.Add("Auswertung");

                ws.Range("A1:I1").Merge().Value = $"Ausschusswerte {DateTime.Today:MMMM yyyy}";
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
                
                workbook.SaveAs(filePath);
                return $"Erfolgreich gespeichert unter: {filePath}";
            }
            catch (Exception ex)
            {
                return $"Fehler beim Export: {ex.Message}";
            }
        }
    }
}
