using Dapper;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QIN_Production_Web.Data
{
    public class AnalyseFilters
    {
        public string? Charge { get; set; }
        public string? FANr { get; set; }
        public DateTime? DateVon { get; set; }
        public DateTime? DateBis { get; set; }
        public string? Artikel { get; set; }
    }

    public class AnalyseSummary
    {
        public int? Thermo_Gutteile { get; set; }
        public int? Thermo_Schlechtteile { get; set; }
        public int? Thermo_SchlechtExtern { get; set; }
        public int? Panther_Gutteile { get; set; }
        public int? Panther_Schlechtteile { get; set; }
        public int? Panther_SchlechtExtern { get; set; }
        public int? UV_Menge { get; set; }
        public int? UV_Ausschuss { get; set; }
        public int? Stanzen_Menge { get; set; }
        public int? Stanzen_Ausschuss { get; set; }
        public int? Fraesen_Menge { get; set; }
        public int? Fraesen_Ausschuss { get; set; }
        public int? Sauberraum_Gutteile { get; set; }
        public int? Sauberraum_Schlechtteile { get; set; }
        public int? Sauberraum_SchlechtExtern { get; set; }
    }

    public class AnalyseResult
    {
        public string Charge { get; set; } = "";
        public string FA_Nr { get; set; } = "";
        public string Artikel { get; set; } = "";
        public DateTime? WareneingangDatum { get; set; }
    }

    public class ThermoRow
    {
        public string FA_Nr { get; set; } = "";
        public DateTime? DatumStart { get; set; }
        public DateTime? DatumEnde { get; set; }
        public string Pausenzeit { get; set; } = "";
        public string Ausfallzeit { get; set; } = "";
        public string MaschinenNr { get; set; } = "";
        public string Bearbeiter { get; set; } = "";
        public string Bemerkung { get; set; } = "";
        public DateTime? Erstelldatum { get; set; }
    }

    public class UvRow
    {
        public DateTime? Erstelldatum { get; set; }
        public int? Menge { get; set; }
        public int? Ausschuss { get; set; }
        public string Bearbeiter { get; set; } = "";
    }

    public class StanzenRow
    {
        public string FA_Nr { get; set; } = "";
        public DateTime? Erstelldatum { get; set; }
        public int? Menge { get; set; }
        public int? Ausschuss { get; set; }
        public string Bearbeiter { get; set; } = "";
    }

    public class EndkontrolleRow
    {
        public int? Gutteile { get; set; }
        public int? Fusseln { get; set; }
        public int? Nadelstiche { get; set; }
        public int? Pickel { get; set; }
        public int? Dekorfehler { get; set; }
        public int? Color { get; set; }
        public int? Flecken { get; set; }
        public int? Nebel { get; set; }
        public int? Vertiefung { get; set; }
        public int? Oelflecken { get; set; }
        public int? Tiefziehfehler { get; set; }
        public int? Fraesfehler { get; set; }
        public int? Knicke { get; set; }
        public int? Kratzer { get; set; }
        public DateTime? Speicherdatum { get; set; }
        public string Bearbeiter { get; set; } = "";
        public string Bemerkung { get; set; } = "";
        
        // Computed Property
        // Computed Property
        public int Schlechtteile => (Fusseln ?? 0) + (Nadelstiche ?? 0) + (Pickel ?? 0) + 
                                    (Dekorfehler ?? 0) + (Color ?? 0) + (Flecken ?? 0) + 
                                    (Nebel ?? 0) + (Vertiefung ?? 0) + (Oelflecken ?? 0) + 
                                    (Tiefziehfehler ?? 0) + (Fraesfehler ?? 0) + (Knicke ?? 0) + (Kratzer ?? 0);
                                    
        public int Schlecht_Int => (Fusseln ?? 0) + (Nadelstiche ?? 0) + (Pickel ?? 0) + 
                                   (Dekorfehler ?? 0) + (Color ?? 0) + (Flecken ?? 0) + 
                                   (Nebel ?? 0) + (Vertiefung ?? 0);
                                   
        public int Schlecht_Ext => (Oelflecken ?? 0) + (Tiefziehfehler ?? 0) + (Fraesfehler ?? 0) + 
                                   (Knicke ?? 0) + (Kratzer ?? 0);
                                   
        public int Gesamt => (Gutteile ?? 0) + Schlechtteile;
        
        public double Int_Prozent => Gesamt == 0 ? 0 : Math.Round((double)Schlecht_Int / Gesamt * 100, 2);
        public double Ext_Prozent => Gesamt == 0 ? 0 : Math.Round((double)Schlecht_Ext / Gesamt * 100, 2);
    }

    public class HeroData
    {
        public string WE_ID { get; set; } = "-";
        public string Standort { get; set; } = "-";
        public string Material { get; set; } = "-";
        public string Thermo_Status { get; set; } = "Offen";
        public string UV_Status { get; set; } = "Offen";
        public string Stanzen_Status { get; set; } = "Offen";
        public string Endkontrolle_Status { get; set; } = "Offen";
        public string Gesamttatus { get; set; } = "Offen";
        public bool IsWareneingangAvailable { get; set; }

        public int Thermo_Gutteile { get; set; }
        public int Thermo_Schlecht_Int { get; set; }
        public int Thermo_Schlecht_Ext { get; set; }
        
        public int UV_Menge { get; set; }
        public int UV_Ausschuss { get; set; }
        
        public int Stanzen_Menge { get; set; }
        public int Stanzen_Ausschuss { get; set; }
        
        public int Endkontrolle_Gutteile { get; set; }
        public int Endkontrolle_Schlecht_Int { get; set; }
        public int Endkontrolle_Schlecht_Ext { get; set; }
    }

    public class WareneingangRow
    {
        public string Lieferant { get; set; } = "";
        public string Lieferscheinnummer { get; set; } = "";
        public string EBENummer { get; set; } = "";
        public string Zustand { get; set; } = "";
        public bool? Palettentausch { get; set; }
        public bool? Gebucht { get; set; }
        public DateTime? Wareneingangdatum { get; set; }
        public string Material { get; set; } = "";
        public double? Menge { get; set; }
        public string Bearbeiter { get; set; } = "";
        public string Bemerkung { get; set; } = "";
    }

    public class ChargenanalyseService
    {
        private Dictionary<string, string> _bearbeiterCache = new();

        private async Task<string> GetBearbeiterNameAsync(string idOrName)
        {
            if (string.IsNullOrWhiteSpace(idOrName)) return "-";
            if (_bearbeiterCache.TryGetValue(idOrName, out var cached)) return cached;

            using var conn = new SqlConnection(SqlManager.connectionString);
            var name = await conn.QueryFirstOrDefaultAsync<string>(
                "SELECT TOP 1 [Benutzer] FROM dbo.LoginDaten WHERE Personalnummer = @p OR Anmeldename = @p OR Benutzer = @p",
                new { p = idOrName });

            var resolved = !string.IsNullOrWhiteSpace(name) ? name : idOrName;
            _bearbeiterCache[idOrName] = resolved;
            return resolved;
        }

        // 1. Suche nach Chargen und Aggregation
        public async Task<(List<AnalyseResult> Rows, AnalyseSummary Summary)> GetChargenAsync(AnalyseFilters f)
        {
            var sb = new StringBuilder();
            var args = new DynamicParameters();
            args.Add("@pLimit", 200);

            var wh = new List<string>();

            if (!string.IsNullOrWhiteSpace(f.Charge))
            {
                var chargeParts = f.Charge.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                          .Select(p => p.Trim())
                                          .Where(p => !string.IsNullOrEmpty(p))
                                          .ToList();

                if (chargeParts.Count == 1)
                {
                    wh.Add("c.[Charge] LIKE '%' + @pCharge + '%'");
                    args.Add("@pCharge", chargeParts[0]);
                }
                else if (chargeParts.Count > 1)
                {
                    var orConditions = new List<string>();
                    for (int i = 0; i < chargeParts.Count; i++)
                    {
                        string pName = $"@pCharge{i}";
                        orConditions.Add($"c.[Charge] LIKE '%' + {pName} + '%'");
                        args.Add(pName, chargeParts[i]);
                    }
                    wh.Add($"({string.Join(" OR ", orConditions)})");
                }
            }

            if (!string.IsNullOrWhiteSpace(f.FANr))
            {
                wh.Add(@"
                EXISTS (
                    SELECT 1
                    FROM dbo.[Thermo_Auftrag] ta
                    WHERE ta.[FA_Nr] = @pFANr
                      AND ta.[FA_Nr] = c.[FA_Nr]
                )");
                args.Add("@pFANr", f.FANr);
            }

            if (f.DateVon.HasValue)
            {
                wh.Add($@"
                EXISTS (
                    SELECT 1
                    FROM dbo.[Wareneingang] we_dz
                    WHERE we_dz.[ID] = c.[Wareneingang_ID]
                      AND we_dz.[Eingangsdatum] >= @pVon
                )");
                args.Add("@pVon", f.DateVon.Value.Date);
            }

            if (f.DateBis.HasValue)
            {
                wh.Add($@"
                EXISTS (
                    SELECT 1
                    FROM dbo.[Wareneingang] we_dz2
                    WHERE we_dz2.[ID] = c.[Wareneingang_ID]
                      AND we_dz2.[Eingangsdatum] < @pBisPlus
                )");
                args.Add("@pBisPlus", f.DateBis.Value.Date.AddDays(1));
            }

            if (!string.IsNullOrWhiteSpace(f.Artikel))
            {
                wh.Add($@"
                EXISTS (
                    SELECT 1
                    FROM dbo.[Wareneingang] we_dt
                    WHERE we_dt.[ID] = c.[Wareneingang_ID]
                      AND we_dt.[Artikel] = @pArtikel
                )");
                args.Add("@pArtikel", f.Artikel);
            }

            sb.AppendLine("DECLARE @BaseCharges TABLE (Charge nvarchar(100) PRIMARY KEY);");
            sb.AppendLine(";WITH Base0 AS (");
            sb.AppendLine($"    SELECT c.[Charge] AS Charge,");
            sb.AppendLine($"        ISNULL((SELECT MAX(we_x.[Eingangsdatum]) FROM dbo.[Wareneingang] we_x WHERE we_x.[ID] = c.[Wareneingang_ID]), CONVERT(datetime,'19000101',112)) AS RowMaxWE");
            sb.AppendLine($"    FROM dbo.[Chargen] AS c");
            if (wh.Count > 0) sb.AppendLine("    WHERE " + string.Join(" AND ", wh));
            sb.AppendLine("), Base AS (");
            sb.AppendLine("    SELECT b0.Charge, MAX(b0.RowMaxWE) AS MaxWE FROM Base0 b0 GROUP BY b0.Charge");
            sb.AppendLine(")");
            sb.AppendLine("INSERT INTO @BaseCharges(Charge)");
            sb.AppendLine("SELECT TOP (@pLimit) b.Charge FROM Base b ORDER BY b.MaxWE DESC, b.Charge;");

            // DataGrid Query
            sb.AppendLine("SELECT bc.[Charge] AS Charge,");
            sb.AppendLine($"  (SELECT TOP 1 cfa.[FA_Nr] FROM dbo.[Chargen] cfa WHERE cfa.[Charge] = bc.[Charge]) AS FA_Nr,");
            sb.AppendLine($"  (SELECT TOP 1 we.[Artikel] FROM dbo.[Wareneingang] we WHERE we.[ID] IN (SELECT DISTINCT c.[Wareneingang_ID] FROM dbo.[Chargen] c WHERE c.[Charge] = bc.[Charge] AND c.[Wareneingang_ID] IS NOT NULL) ORDER BY we.[Eingangsdatum] DESC) AS Artikel,");
            sb.AppendLine($"  (SELECT TOP 1 we2.[Eingangsdatum] FROM dbo.[Wareneingang] we2 WHERE we2.[ID] IN (SELECT DISTINCT c2.[Wareneingang_ID] FROM dbo.[Chargen] c2 WHERE c2.[Charge] = bc.[Charge] AND c2.[Wareneingang_ID] IS NOT NULL) ORDER BY we2.[Eingangsdatum] DESC) AS WareneingangDatum");
            sb.AppendLine("FROM @BaseCharges bc ORDER BY WareneingangDatum DESC, bc.[Charge];");

            // Summary Query
            sb.AppendLine("SELECT");
            sb.AppendLine("  (SELECT SUM(c2.[Gutteile]) FROM dbo.[Chargen] c2 WHERE c2.[Charge] IN (SELECT Charge FROM @BaseCharges)) AS Thermo_Gutteile,");
            sb.AppendLine("  (SELECT SUM(c3.[Schlechtteile]) FROM dbo.[Chargen] c3 WHERE c3.[Charge] IN (SELECT Charge FROM @BaseCharges)) AS Thermo_Schlechtteile,");
            sb.AppendLine("  (SELECT SUM(c4.[Schlechtteile_Ext]) FROM dbo.[Chargen] c4 WHERE c4.[Charge] IN (SELECT Charge FROM @BaseCharges)) AS Thermo_SchlechtExtern,");
            sb.AppendLine("  (SELECT SUM(CASE WHEN c5.[Panther_OK] = 1 THEN 1 ELSE 0 END) FROM dbo.[Chargen] c5 WHERE c5.[Charge] IN (SELECT Charge FROM @BaseCharges)) AS Panther_Gutteile,");
            sb.AppendLine("  (SELECT SUM(CASE WHEN c6.[Panther_OK] = 0 THEN 1 ELSE 0 END) FROM dbo.[Chargen] c6 WHERE c6.[Charge] IN (SELECT Charge FROM @BaseCharges)) AS Panther_Schlechtteile,");
            sb.AppendLine("  (SELECT SUM(CASE WHEN c7.[Panther_OK] = 0 AND c7.[Panther_Intern] = 0 THEN 1 ELSE 0 END) FROM dbo.[Chargen] c7 WHERE c7.[Charge] IN (SELECT Charge FROM @BaseCharges)) AS Panther_SchlechtExtern,");
            sb.AppendLine("  (SELECT SUM(c8.[UVMenge]) FROM dbo.[Chargen] c8 WHERE c8.[Charge] IN (SELECT Charge FROM @BaseCharges)) AS UV_Menge,");
            sb.AppendLine("  (SELECT SUM(c9.[UVAuschuss]) FROM dbo.[Chargen] c9 WHERE c9.[Charge] IN (SELECT Charge FROM @BaseCharges)) AS UV_Ausschuss,");
            sb.AppendLine("  (SELECT SUM(c10.[Stanzen_Menge]) FROM dbo.[Chargen] c10 WHERE c10.[Charge] IN (SELECT Charge FROM @BaseCharges)) AS Stanzen_Menge,");
            sb.AppendLine("  (SELECT SUM(c11.[Stanzen_Ausschuss]) FROM dbo.[Chargen] c11 WHERE c11.[Charge] IN (SELECT Charge FROM @BaseCharges)) AS Stanzen_Ausschuss,");
            sb.AppendLine("  (SELECT SUM(c12.[Stanzen_Menge]) FROM dbo.[Chargen] c12 WHERE c12.[Charge] IN (SELECT Charge FROM @BaseCharges)) AS Fraesen_Menge,");
            sb.AppendLine("  (SELECT SUM(c13.[Stanzen_Ausschuss]) FROM dbo.[Chargen] c13 WHERE c13.[Charge] IN (SELECT Charge FROM @BaseCharges)) AS Fraesen_Ausschuss;");

            using var conn = new SqlConnection(SqlManager.FertigungConnectionString);
            await conn.OpenAsync();
            using var multi = await conn.QueryMultipleAsync(sb.ToString(), args);
            
            var rows = (await multi.ReadAsync<AnalyseResult>()).ToList();
            var summary = (await multi.ReadFirstOrDefaultAsync<AnalyseSummary>()) ?? new AnalyseSummary();

            var charges = rows.Select(r => r.Charge).Distinct().ToArray();
            if (charges.Length > 0)
            {
                using var conn2 = new SqlConnection(SqlManager.connectionString);
                await conn2.OpenAsync();
                var sqlSauber = @"
                    SELECT
                        ISNULL(SUM(t.[Gutteile]), 0) AS Gut,
                        ISNULL(SUM(COALESCE(t.[Oelflecken],0) + COALESCE(t.[Tiefziehfehler],0) + COALESCE(t.[Fraesfehler],0) + COALESCE(t.[Knicke],0) + COALESCE(t.[Kratzer],0)), 0) AS Schlecht,
                        ISNULL(SUM(COALESCE(t.[Fusseln],0) + COALESCE(t.[Nadelstiche],0) + COALESCE(t.[Pickel],0) + COALESCE(t.[Dekorfehler],0) + COALESCE(t.[Flecken],0) + COALESCE(t.[Nebel],0) + COALESCE(t.[Vertiefung],0)), 0) AS Extern
                    FROM dbo.[Table1] AS t WHERE t.[Charge] IN @charges;";

                var sauber = await conn2.QueryFirstOrDefaultAsync<(int Gut, int Schlecht, int Extern)>(sqlSauber, new { charges });
                summary.Sauberraum_Gutteile = sauber.Gut;
                summary.Sauberraum_Schlechtteile = sauber.Schlecht;
                summary.Sauberraum_SchlechtExtern = sauber.Extern;
            }

            return (rows, summary);
        }

        // 2. Load Hero Data 
        public async Task<HeroData> LoadHeroAsync(string selectedCharge)
        {
            var hero = new HeroData();
            if (string.IsNullOrWhiteSpace(selectedCharge)) return hero;

            using (var conn = new SqlConnection(SqlManager.FertigungConnectionString))
            {
                await conn.OpenAsync();
                var chargeInfo = await conn.QueryFirstOrDefaultAsync<(int? WE_ID, string FA_Nr)>(
                    "SELECT TOP 1 Wareneingang_ID, FA_Nr FROM dbo.Chargen WHERE Charge = @c", new { c = selectedCharge });

                hero.WE_ID = chargeInfo.WE_ID?.ToString() ?? "-";
                string faNr = chargeInfo.FA_Nr;

                if (!string.IsNullOrEmpty(faNr))
                {
                    hero.Material = await conn.QueryFirstOrDefaultAsync<string>("SELECT TOP 1 Material FROM Thermo_Auftrag WHERE FA_Nr = @fa", new { fa = faNr }) ?? "-";

                    var thermoSent = await conn.QueryFirstOrDefaultAsync<int?>("SELECT TOP 1 Abgesendet FROM Thermo_Auftrag WHERE FA_Nr = @fa ORDER BY ID DESC", new { fa = faNr });
                    if (thermoSent == 1) hero.Thermo_Status = "Fertig";
                    else hero.Thermo_Status = (await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Thermo_Auftrag WHERE FA_Nr = @fa", new { fa = faNr }) > 0) ? "In Arbeit" : "Offen";

                    var uvSent = await conn.QueryFirstOrDefaultAsync<int?>("SELECT TOP 1 Abgesendet FROM UV_Auftrag WHERE FA_Nr = @fa ORDER BY ID DESC", new { fa = faNr });
                    if (uvSent == 1) hero.UV_Status = "Fertig";
                    else hero.UV_Status = (await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM UV_Auftrag WHERE FA_Nr = @fa", new { fa = faNr }) > 0) ? "In Arbeit" : "Offen";

                    var stanzenSent = await conn.QueryFirstOrDefaultAsync<int?>("SELECT TOP 1 Abgesendet FROM Stanzen_Auftrag WHERE FA_Nr = @fa ORDER BY ID DESC", new { fa = faNr });
                    if (stanzenSent == 1) hero.Stanzen_Status = "Fertig";
                    else hero.Stanzen_Status = (await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Stanzen_Auftrag WHERE FA_Nr = @fa", new { fa = faNr }) > 0) ? "In Arbeit" : "Offen";
                }

                var agg = await conn.QueryFirstOrDefaultAsync<(int Gut, int SchlechtInt, int SchlechtExt, int UVMenge, int UV_Aus, int StanzenMenge, int StanzenAus)>(
                    @"SELECT 
                        ISNULL(SUM(Gutteile), 0) AS Gut,
                        ISNULL(SUM(Schlechtteile), 0) AS SchlechtInt,
                        ISNULL(SUM(Schlechtteile_Ext), 0) AS SchlechtExt,
                        ISNULL(SUM(UVMenge), 0) AS UVMenge,
                        ISNULL(SUM(UVAuschuss), 0) AS UV_Aus,
                        ISNULL(SUM(Stanzen_Menge), 0) AS StanzenMenge,
                        ISNULL(SUM(Stanzen_Ausschuss), 0) AS StanzenAus
                      FROM dbo.Chargen WHERE Charge = @c", new { c = selectedCharge });

                hero.Thermo_Gutteile = agg.Gut;
                hero.Thermo_Schlecht_Int = agg.SchlechtInt;
                hero.Thermo_Schlecht_Ext = agg.SchlechtExt;
                hero.UV_Menge = agg.UVMenge;
                hero.UV_Ausschuss = agg.UV_Aus;
                hero.Stanzen_Menge = agg.StanzenMenge;
                hero.Stanzen_Ausschuss = agg.StanzenAus;
            }

            using (var conn = new SqlConnection(SqlManager.connectionString))
            {
                await conn.OpenAsync();
                var loc = await conn.QueryFirstOrDefaultAsync<string>("SELECT TOP 1 QRCode FROM Lagerorte WHERE AktuelleCharge LIKE '%' + @c + '%'", new { c = selectedCharge.Trim() });
                
                string parsedLoc = loc ?? "-";
                if (parsedLoc.StartsWith("H") && parsedLoc.Contains("R") && parsedLoc.Contains("P"))
                {
                    try {
                        var match = System.Text.RegularExpressions.Regex.Match(parsedLoc, @"^H(\d+)R(\d+)P(\d+)$");
                        if(match.Success) {
                            parsedLoc = $"Halle: {match.Groups[1].Value} Regal: {match.Groups[2].Value} Platz: {match.Groups[3].Value}";
                        }
                    } catch { }
                }
                hero.Standort = parsedLoc;

                var ekExists = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Table1 WHERE Charge = @c", new { c = selectedCharge });
                hero.Endkontrolle_Status = (ekExists > 0) ? "Fertig" : "Offen";

                var sqlSauber = @"
                    SELECT
                        ISNULL(SUM(Gutteile), 0) AS Gut,
                        ISNULL(SUM(COALESCE(Oelflecken,0) + COALESCE(Tiefziehfehler,0) + COALESCE(Fraesfehler,0) + COALESCE(Knicke,0) + COALESCE(Kratzer,0)), 0) AS Ext,
                        ISNULL(SUM(COALESCE(Fusseln,0) + COALESCE(Nadelstiche,0) + COALESCE(Pickel,0) + COALESCE(Dekorfehler,0) + COALESCE(Color, 0) + COALESCE(Flecken,0) + COALESCE(Nebel,0) + COALESCE(Vertiefung,0)), 0) AS Int
                    FROM dbo.Table1 WHERE Charge = @c;";
                var sauber = await conn.QueryFirstOrDefaultAsync<(int Gut, int Ext, int Int)>(sqlSauber, new { c = selectedCharge });
                hero.Endkontrolle_Gutteile = sauber.Gut;
                hero.Endkontrolle_Schlecht_Int = sauber.Int;
                hero.Endkontrolle_Schlecht_Ext = sauber.Ext;
            }

            if (hero.Thermo_Status == "Fertig" && hero.Stanzen_Status == "Fertig" && hero.Endkontrolle_Status == "Fertig") hero.Gesamttatus = "Fertig";
            else if (hero.Thermo_Status == "Offen" && hero.UV_Status == "Offen" && hero.Stanzen_Status == "Offen" && hero.Endkontrolle_Status == "Offen") hero.Gesamttatus = "Offen";
            else hero.Gesamttatus = "In Produktion";

            if (int.TryParse(hero.WE_ID, out _)) hero.IsWareneingangAvailable = true;

            return hero;
        }

        // 3. Load Thermo
        public async Task<List<ThermoRow>> LoadThermoAsync(string selectedCharge)
        {
            var list = new List<ThermoRow>();
            if (string.IsNullOrWhiteSpace(selectedCharge)) return list;

            using var conn = new SqlConnection(SqlManager.FertigungConnectionString);
            var faNr = await conn.QueryFirstOrDefaultAsync<string>("SELECT TOP 1 FA_Nr FROM dbo.Chargen WHERE Charge = @c ORDER BY ID DESC", new { c = selectedCharge });

            if (!string.IsNullOrEmpty(faNr))
            {
                var rows = await conn.QueryAsync<ThermoRow>(
                    @"SELECT FA_Nr, Personal_Nr as Bearbeiter, Maschinen_Nr as MaschinenNr, Begin_Datum as DatumStart, End_Datum as DatumEnde, 
                      Pausenzeit, Ausfallzeit, Bemerkung, Erstellungsdatum AS Erstelldatum 
                      FROM Thermo_Auftrag WHERE FA_Nr = @fanr ORDER BY ID DESC;", new { fanr = faNr });
                list.AddRange(rows);
            }
            
            foreach (var r in list) r.Bearbeiter = await GetBearbeiterNameAsync(r.Bearbeiter);
            return list;
        }

        // 4. Load UV
        public async Task<List<UvRow>> LoadUVAsync(string selectedCharge)
        {
            using var conn = new SqlConnection(SqlManager.FertigungConnectionString);
            var uvRecords = await conn.QueryAsync<(string FA_Nr, int? UVMenge, int? UVAuschuss)>(
                "SELECT FA_Nr, UVMenge, UVAuschuss FROM dbo.Chargen WHERE Charge = @charge AND (UVMenge IS NOT NULL OR UVAuschuss IS NOT NULL) ORDER BY ID DESC",
                new { charge = selectedCharge });

            var list = new List<UvRow>();
            foreach (var rec in uvRecords)
            {
                var uvUser = await conn.QuerySingleOrDefaultAsync<(string Benutzer, DateTime? Erstelldatum)>(
                    "SELECT TOP 1 Benutzer, Erstelldatum FROM UV_Auftrag WHERE FA_Nr = @fanr ORDER BY ID DESC", new { fanr = rec.FA_Nr });

                list.Add(new UvRow {
                    Erstelldatum = uvUser.Erstelldatum,
                    Menge = rec.UVMenge,
                    Ausschuss = rec.UVAuschuss,
                    Bearbeiter = await GetBearbeiterNameAsync(uvUser.Benutzer)
                });
            }
            return list;
        }

        // 5. Load Stanzen
        public async Task<List<StanzenRow>> LoadStanzenAsync(string selectedCharge)
        {
            using var conn = new SqlConnection(SqlManager.FertigungConnectionString);
            var stanzenRecords = await conn.QueryAsync<(string FA_Nr, int? Stanzen_Menge, int? Stanzen_Ausschuss)>(
                "SELECT FA_Nr, Stanzen_Menge, Stanzen_Ausschuss FROM dbo.Chargen WHERE Charge = @charge AND (Stanzen_Menge IS NOT NULL OR Stanzen_Ausschuss IS NOT NULL) ORDER BY ID DESC",
                new { charge = selectedCharge });

            var list = new List<StanzenRow>();
            foreach (var rec in stanzenRecords)
            {
                var stanzenUser = await conn.QuerySingleOrDefaultAsync<(string Benutzer, DateTime? Erstelldatum)>(
                    "SELECT TOP 1 Benutzer, Erstelldatum FROM Stanzen_Auftrag WHERE FA_Nr = @fanr ORDER BY ID DESC", new { fanr = rec.FA_Nr });

                list.Add(new StanzenRow {
                    FA_Nr = rec.FA_Nr,
                    Erstelldatum = stanzenUser.Erstelldatum,
                    Menge = rec.Stanzen_Menge,
                    Ausschuss = rec.Stanzen_Ausschuss,
                    Bearbeiter = await GetBearbeiterNameAsync(stanzenUser.Benutzer)
                });
            }
            return list;
        }

        // 6. Load Endkontrolle
        public async Task<List<EndkontrolleRow>> LoadEndkontrolleAsync(string selectedCharge)
        {
            using var conn = new SqlConnection(SqlManager.connectionString);
            var query = @"SELECT FSKdate AS Speicherdatum, Gutteile, Fusseln, Nadelstiche, Pickel, Dekorfehler, Color, 
                          Flecken, Nebel, Vertiefung, Oelflecken, Tiefziehfehler, Fraesfehler, Knicke, Kratzer, 
                          Personalnummer AS Bearbeiter, Bemerkungen AS Bemerkung 
                          FROM dbo.Table1 WHERE Charge = @charge ORDER BY ID DESC";
            
            var rows = (await conn.QueryAsync<EndkontrolleRow>(query, new { charge = selectedCharge })).ToList();
            foreach (var r in rows) r.Bearbeiter = await GetBearbeiterNameAsync(r.Bearbeiter);
            return rows;
        }

        // 7. Load Wareneingang
        public async Task<WareneingangRow?> LoadWareneingangAsync(string weId)
        {
            if (!int.TryParse(weId, out int id)) return null;

            using var conn = new SqlConnection(SqlManager.FertigungConnectionString);
            var query = @"SELECT 
                              Lieferant, 
                              LS_Nr AS Lieferscheinnummer, 
                              EBE_Nr AS EBENummer, 
                              Zustand, 
                              Palettentausch, 
                              Gebucht, 
                              Eingangsdatum AS Wareneingangdatum, 
                              Artikel AS Material, 
                              Menge, 
                              Benutzer AS Bearbeiter, 
                              Bemerkung 
                          FROM dbo.[Wareneingang] WHERE ID = @id";
                          
            var we = await conn.QueryFirstOrDefaultAsync<WareneingangRow>(query, new { id });
            if (we != null) we.Bearbeiter = await GetBearbeiterNameAsync(we.Bearbeiter);
            return we;
        }
    }
}
