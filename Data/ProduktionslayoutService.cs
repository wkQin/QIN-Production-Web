using Dapper;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QIN_Production_Web.Data
{
    public class WareneingangInfo
    {
        public string Artikel { get; set; } = "";
        public DateTime Eingangsdatum { get; set; }
    }

    public class PlatzInfo
    {
        public string QRCode { get; set; } = "";
        public List<string> Charges { get; set; } = new();
        public List<PlatzChargeInfo> PlatzChargen { get; set; } = new();
        public long SumAktuelleMenge { get; set; }
        public long SumEchteMenge { get; set; }
        public List<WareneingangInfo> Wareneingaenge { get; set; } = new();
    }

    public class PlatzChargeInfo
    {
        public string Charge { get; set; } = "";
        public DateTime? EingelagertAm { get; set; }
    }

    public class SperrlagerChargeInfo
    {
        public int ID { get; set; }
        public string Charge { get; set; } = "";
        public string Artikel { get; set; } = "";
        public int AktuelleMenge { get; set; }
        public int EchteMenge { get; set; }
        public string Einheit { get; set; } = "";
        public DateTime? Datum { get; set; }
        public DateTime? Eingangsdatum { get; set; }
    }

    public class ChargeDetailInfo
    {
        public string Charge { get; set; } = "";
        public string Artikel { get; set; } = "";
        public string Lieferant { get; set; } = "";
        public string LSNr { get; set; } = "";
        public string EBENr { get; set; } = "";
        public string Zustand { get; set; } = "";
        public string Bemerkung { get; set; } = "";
        public string Pos { get; set; } = "";
        public string Dickenmessung { get; set; } = "";
        public int AktuelleMenge { get; set; }
        public int EchteMenge { get; set; }
        public string Einheit { get; set; } = "";
        public bool Gesperrt { get; set; }
        public DateTime? ChargenDatum { get; set; }
        public DateTime? Eingangsdatum { get; set; }
        public string Lagerort { get; set; } = "";
        public DateTime? EingelagertAm { get; set; }
        public string SperrlagerAktion { get; set; } = "";
        public string SperrlagerBereich { get; set; } = "";
        public string SperrlagerGrund { get; set; } = "";
        public string SperrlagerBenutzer { get; set; } = "";
        public DateTime? SperrlagerDatum { get; set; }
    }

    public class SperrlagerActionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public int BetroffeneChargen { get; set; }
    }

    internal class SperrlagerChargeUpdateResult
    {
        public List<(int ID, string Charge)> Found { get; set; } = new();
        public List<string> Missing { get; set; } = new();
    }

    public class MachineInfo
    {
        public string? AktuelleCharge { get; set; }
        public DateTime? LetzteNutzung { get; set; }
        public string? Artikel { get; set; }
    }

    public class ProduktionslayoutService
    {
        public async Task<MachineInfo> GetMachineInfoAsync(string qrCode)
        {
            string? aktuelleCharge = null;
            DateTime? letzteNutzung = null;

            // 1. Hole AktuelleCharge + LetzteNutzung von Lagerorte
            using (var conn = new SqlConnection(SqlManager.connectionString))
            {
                var query = "SELECT AktuelleCharge, LetzteNutzung FROM dbo.Lagerorte WHERE QRCode = @qrCode";
                var row = await conn.QueryFirstOrDefaultAsync<dynamic>(query, new { qrCode });
                
                if (row != null)
                {
                    aktuelleCharge = row.AktuelleCharge as string;
                    letzteNutzung = row.LetzteNutzung as DateTime?;
                }
            }

            // 2. Hole den Artikelname über die Charge (aus Chargen -> Wareneingang)
            string? artikel = null;
            if (!string.IsNullOrWhiteSpace(aktuelleCharge))
            {
                var firstCharge = aktuelleCharge.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim();
                using (var conn = new SqlConnection(SqlManager.FertigungConnectionString))
                {
                    var query = @"
                        SELECT TOP 1 w.Artikel 
                        FROM dbo.Wareneingang w
                        JOIN dbo.Chargen c ON c.Wareneingang_ID = w.ID
                        WHERE c.Charge = @charge";
                    if (!string.IsNullOrWhiteSpace(firstCharge))
                    {
                        artikel = await conn.QueryFirstOrDefaultAsync<string>(query, new { charge = firstCharge });
                        aktuelleCharge = firstCharge; // Ensure only the single charge is sent to the UI
                    }
                }
            }

            return new MachineInfo
            {
                AktuelleCharge = aktuelleCharge,
                LetzteNutzung = letzteNutzung,
                Artikel = artikel
            };
        }

        public async Task<Dictionary<string, PlatzInfo>> GetRegalInfosAsync(IEnumerable<string> qrCodes)
        {
            var result = new Dictionary<string, PlatzInfo>(StringComparer.OrdinalIgnoreCase);
            var qrList = qrCodes?.Where(q => !string.IsNullOrWhiteSpace(q)).Distinct().ToList() ?? new List<string>();
            if (qrList.Count == 0) return result;

            foreach (var qr in qrList)
                result[qr] = new PlatzInfo { QRCode = qr };

            var chargeToPlaces = new Dictionary<string, List<PlatzInfo>>(StringComparer.OrdinalIgnoreCase);
            var activeCharges = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using (var conn = new SqlConnection(SqlManager.connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    var paramNames = new List<string>();
                    for (int i = 0; i < qrList.Count; i++)
                    {
                        string p = "@p" + i;
                        paramNames.Add(p);
                        cmd.Parameters.AddWithValue(p, qrList[i]);
                    }

                    cmd.CommandText = $@"
                        SELECT QRCode, AktuelleCharge, LetzteNutzung
                        FROM dbo.Lagerorte
                        WHERE QRCode IN ({string.Join(", ", paramNames)});
                    ";

                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var qr = reader.GetString(0);
                        var chargeRaw = reader.IsDBNull(1) ? null : reader.GetString(1);
                        var letzteNutzung = reader.IsDBNull(2) ? (DateTime?)null : reader.GetDateTime(2);

                        if (!result.TryGetValue(qr, out var info)) continue;

                        if (!string.IsNullOrWhiteSpace(chargeRaw))
                        {
                            var parts = chargeRaw.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var part in parts)
                            {
                                var charge = part.Trim();
                                if (string.IsNullOrWhiteSpace(charge)) continue;

                                activeCharges.Add(charge);
                                if (!chargeToPlaces.TryGetValue(charge, out var list))
                                {
                                    list = new List<PlatzInfo>();
                                    chargeToPlaces[charge] = list;
                                }
                                list.Add(info);

                                if (!info.PlatzChargen.Any(c => string.Equals(c.Charge, charge, StringComparison.OrdinalIgnoreCase)))
                                {
                                    info.PlatzChargen.Add(new PlatzChargeInfo
                                    {
                                        Charge = charge,
                                        EingelagertAm = letzteNutzung
                                    });
                                }
                            }
                        }
                    }
                }
            }

            if (activeCharges.Count == 0) return result;
            var chargeList = activeCharges.ToList();

            using (var conn = new SqlConnection(SqlManager.FertigungConnectionString))
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    var pNames = new List<string>();
                    for (int i = 0; i < chargeList.Count; i++)
                    {
                        string p = "@c" + i;
                        pNames.Add(p);
                        cmd.Parameters.AddWithValue(p, chargeList[i]);
                    }

                    cmd.CommandText = $@"
                        SELECT c.Charge, c.Aktuelle_Menge, c.Echte_Menge, w.Artikel, w.Eingangsdatum
                        FROM dbo.Chargen c
                        LEFT JOIN dbo.Wareneingang w ON w.ID = c.Wareneingang_ID
                        WHERE c.Charge IN ({string.Join(", ", pNames)});
                    ";

                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        string charge = reader.GetString(0);
                        int aktMenge = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                        int echteMenge = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                        string artikel = reader.IsDBNull(3) ? "" : reader.GetString(3);
                        DateTime eingangsdatum = reader.IsDBNull(4) ? DateTime.MinValue : reader.GetDateTime(4);

                        if (chargeToPlaces.TryGetValue(charge, out var places))
                        {
                            foreach (var pi in places)
                            {
                                if (!pi.Charges.Contains(charge))
                                {
                                    pi.Charges.Add(charge);
                                    pi.SumAktuelleMenge += aktMenge;
                                    pi.SumEchteMenge += echteMenge;
                                }

                                // Avoid duplicate Wareneingang info for same article/date if multiple charges exist
                                if (!string.IsNullOrWhiteSpace(artikel) && !pi.Wareneingaenge.Any(w => w.Artikel == artikel && w.Eingangsdatum.Date == eingangsdatum.Date))
                                {
                                    pi.Wareneingaenge.Add(new WareneingangInfo { Artikel = artikel, Eingangsdatum = eingangsdatum });
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        public async Task<List<SperrlagerChargeInfo>> GetGesperrteNichtEingelagerteChargenAsync()
        {
            var eingelagerteChargen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using (var conn = new SqlConnection(SqlManager.connectionString))
            {
                var lagerCharges = await conn.QueryAsync<string?>(
                    @"SELECT AktuelleCharge
                      FROM dbo.Lagerorte
                      WHERE AktuelleCharge IS NOT NULL
                        AND LTRIM(RTRIM(AktuelleCharge)) <> ''");

                foreach (var chargeRaw in lagerCharges)
                {
                    if (string.IsNullOrWhiteSpace(chargeRaw)) continue;

                    var parts = chargeRaw.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var part in parts)
                    {
                        var charge = part.Trim();
                        if (!string.IsNullOrWhiteSpace(charge))
                        {
                            eingelagerteChargen.Add(charge);
                        }
                    }
                }
            }

            List<SperrlagerChargeInfo> gesperrteChargen;
            using (var conn = new SqlConnection(SqlManager.FertigungConnectionString))
            {
                var rows = await conn.QueryAsync<SperrlagerChargeInfo>(
                    @"SELECT
                          c.ID,
                          ISNULL(c.Charge, '') AS Charge,
                          ISNULL(w.Artikel, '') AS Artikel,
                          ISNULL(c.Aktuelle_Menge, 0) AS AktuelleMenge,
                          ISNULL(c.Echte_Menge, 0) AS EchteMenge,
                          ISNULL(c.Einheit, '') AS Einheit,
                          c.Datum,
                          w.Eingangsdatum
                      FROM dbo.Chargen c
                      LEFT JOIN dbo.Wareneingang w ON w.ID = c.Wareneingang_ID
                      WHERE c.Gesperrt = 1
                        AND ISNULL(c.Charge, '') <> ''
                        AND ISNULL(c.Status_ID, 0) <> 3
                        AND ISNULL(c.Zustand, '') <> N'Vermüllt'
                      ORDER BY COALESCE(c.Datum, w.Eingangsdatum) DESC, c.ID DESC");

                gesperrteChargen = rows.ToList();
            }

            return gesperrteChargen
                .Where(c => !eingelagerteChargen.Contains(c.Charge))
                .ToList();
        }

        public async Task<ChargeDetailInfo?> GetChargeDetailsAsync(string charge)
        {
            if (string.IsNullOrWhiteSpace(charge)) return null;

            var cleanCharge = charge.Trim();
            string lagerort = "";
            DateTime? eingelagertAm = null;

            using (var conn = new SqlConnection(SqlManager.connectionString))
            {
                var lagerorte = await conn.QueryAsync<(string QRCode, string? AktuelleCharge, DateTime? LetzteNutzung)>(
                    @"SELECT QRCode, AktuelleCharge, LetzteNutzung
                      FROM dbo.Lagerorte
                      WHERE AktuelleCharge IS NOT NULL
                        AND LTRIM(RTRIM(AktuelleCharge)) <> ''");

                foreach (var ort in lagerorte)
                {
                    var parts = (ort.AktuelleCharge ?? "").Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Any(p => string.Equals(p.Trim(), cleanCharge, StringComparison.OrdinalIgnoreCase)))
                    {
                        lagerort = ort.QRCode;
                        eingelagertAm = ort.LetzteNutzung;
                        break;
                    }
                }
            }

            using (var conn = new SqlConnection(SqlManager.FertigungConnectionString))
            {
                var detail = await conn.QueryFirstOrDefaultAsync<ChargeDetailInfo>(
                    @"SELECT TOP 1
                          ISNULL(c.Charge, '') AS Charge,
                          ISNULL(w.Artikel, '') AS Artikel,
                          ISNULL(w.Lieferant, '') AS Lieferant,
                          ISNULL(w.LS_Nr, '') AS LSNr,
                          ISNULL(w.EBE_Nr, '') AS EBENr,
                          ISNULL(w.Zustand, '') AS Zustand,
                          ISNULL(w.Bemerkung, '') AS Bemerkung,
                          ISNULL(w.Pos, '') AS Pos,
                          ISNULL(w.Dickenmessung, '') AS Dickenmessung,
                          ISNULL(c.Aktuelle_Menge, 0) AS AktuelleMenge,
                          ISNULL(c.Echte_Menge, 0) AS EchteMenge,
                          ISNULL(c.Einheit, '') AS Einheit,
                          CAST(c.Gesperrt AS bit) AS Gesperrt,
                          c.Datum AS ChargenDatum,
                          w.Eingangsdatum,
                          ISNULL(sl.Aktion, '') AS SperrlagerAktion,
                          ISNULL(sl.Bereich, '') AS SperrlagerBereich,
                          ISNULL(sl.Grund, '') AS SperrlagerGrund,
                          ISNULL(COALESCE(sl.GesperrtVon, sl.EntsperrtVon, sl.VermuelltVon), '') AS SperrlagerBenutzer,
                          COALESCE(sl.GesperrtAm, sl.EntsperrtAm, sl.VermuelltAm, sl.CreatedAt) AS SperrlagerDatum
                      FROM dbo.Chargen c
                      LEFT JOIN dbo.Wareneingang w ON w.ID = c.Wareneingang_ID
                      OUTER APPLY (
                          SELECT TOP 1 *
                          FROM dbo.Sperrlager sl
                          WHERE sl.Chargen_ID = c.ID OR sl.Charge = c.Charge
                          ORDER BY sl.CreatedAt DESC, sl.ID DESC
                      ) sl
                      WHERE c.Charge = @charge
                      ORDER BY c.ID DESC",
                    new { charge = cleanCharge });

                if (detail == null) return null;

                detail.Lagerort = lagerort;
                detail.EingelagertAm = eingelagertAm;
                return detail;
            }
        }

        public async Task<SperrlagerActionResult> SperreChargenImSperrlagerAsync(string chargeText, string lagerortQRCode, string benutzer, string personalnummer)
        {
            var charges = ParseChargeText(chargeText);
            if (charges.Count == 0)
            {
                return new SperrlagerActionResult { Message = "Keine Charge eingegeben." };
            }

            if (string.IsNullOrWhiteSpace(lagerortQRCode))
            {
                return new SperrlagerActionResult { Message = "Bitte zuerst einen Sperrlagerplatz auswählen." };
            }

            var updateResult = await SetChargenGesperrtAsync(charges, true, false, benutzer, personalnummer, "Sperrlager", "Manuelle Sperre über Sperrlager", lagerortQRCode);
            if (updateResult.Found.Count == 0)
            {
                return new SperrlagerActionResult { Message = BuildSperrlagerMessage("Keine passende Charge gefunden.", updateResult.Missing) };
            }

            await MoveChargesToLagerortAsync(updateResult.Found.Select(c => c.Charge).ToList(), lagerortQRCode);

            return new SperrlagerActionResult
            {
                Success = true,
                BetroffeneChargen = updateResult.Found.Count,
                Message = BuildSperrlagerMessage($"{updateResult.Found.Count} Charge(n) wurden auf {lagerortQRCode} gesperrt/eingelagert.", updateResult.Missing)
            };
        }

        public async Task<SperrlagerActionResult> EntsperreChargenAsync(string chargeText, string benutzer, string personalnummer)
        {
            var charges = ParseChargeText(chargeText);
            if (charges.Count == 0)
            {
                return new SperrlagerActionResult { Message = "Keine Charge eingegeben." };
            }

            var updateResult = await SetChargenGesperrtAsync(charges, false, false, benutzer, personalnummer, "Sperrlager", "Entsperrt über Sperrlager", null);
            if (updateResult.Found.Count == 0)
            {
                return new SperrlagerActionResult { Message = BuildSperrlagerMessage("Keine passende Charge gefunden.", updateResult.Missing) };
            }

            await RemoveChargesFromAllLagerorteAsync(updateResult.Found.Select(c => c.Charge).ToList());

            return new SperrlagerActionResult
            {
                Success = true,
                BetroffeneChargen = updateResult.Found.Count,
                Message = BuildSperrlagerMessage($"{updateResult.Found.Count} Charge(n) wurden entsperrt und aus dem Sperrlager entfernt.", updateResult.Missing)
            };
        }

        public async Task<SperrlagerActionResult> VermuelleChargenAsync(string chargeText, string benutzer, string personalnummer)
        {
            var charges = ParseChargeText(chargeText);
            if (charges.Count == 0)
            {
                return new SperrlagerActionResult { Message = "Keine Charge eingegeben." };
            }

            var updateResult = await SetChargenGesperrtAsync(charges, false, true, benutzer, personalnummer, "Sperrlager", "Vermüllt über Sperrlager", null);
            if (updateResult.Found.Count == 0)
            {
                return new SperrlagerActionResult { Message = BuildSperrlagerMessage("Keine passende Charge gefunden.", updateResult.Missing) };
            }

            await RemoveChargesFromAllLagerorteAsync(updateResult.Found.Select(c => c.Charge).ToList());

            return new SperrlagerActionResult
            {
                Success = true,
                BetroffeneChargen = updateResult.Found.Count,
                Message = BuildSperrlagerMessage($"{updateResult.Found.Count} Charge(n) wurden als vermüllt gebucht und aus dem Sperrlager entfernt.", updateResult.Missing)
            };
        }

        private static string BuildSperrlagerMessage(string baseMessage, List<string> missing)
        {
            if (missing.Count == 0)
            {
                return baseMessage;
            }

            return $"{baseMessage} Nicht gefunden in dbo.Chargen: {string.Join(", ", missing)}.";
        }

        private static List<string> ParseChargeText(string chargeText)
        {
            return (chargeText ?? "")
                .Split(new[] { ',', ';', '\r', '\n', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.Trim())
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private async Task<SperrlagerChargeUpdateResult> SetChargenGesperrtAsync(List<string> charges, bool gesperrt, bool vermuellt, string benutzer, string personalnummer, string bereich, string grund, string? lagerortQRCode)
        {
            var result = new SperrlagerChargeUpdateResult();

            using (var conn = new SqlConnection(SqlManager.FertigungConnectionString))
            {
                await conn.OpenAsync();

                foreach (var charge in charges)
                {
                    var row = await conn.QueryFirstOrDefaultAsync<(int ID, string Charge)>(
                        @"SELECT TOP 1 ID, Charge
                          FROM dbo.Chargen
                          WHERE Charge = @charge
                          ORDER BY ID DESC",
                        new { charge });

                    if (row.ID <= 0 || string.IsNullOrWhiteSpace(row.Charge))
                    {
                        result.Missing.Add(charge);
                        continue;
                    }

                    if (vermuellt)
                    {
                        await conn.ExecuteAsync(
                            @"UPDATE dbo.Chargen
                              SET Gesperrt = 1,
                                  Aktuelle_Menge = 0,
                                  Status_ID = 3,
                                  Zustand = N'Vermüllt'
                              WHERE ID = @id",
                            new { id = row.ID });

                        await InsertSperrlagerLogAsync(conn, row.ID, row.Charge, "Vermüllt", bereich, grund, benutzer, personalnummer, lagerortQRCode);
                    }
                    else if (gesperrt)
                    {
                        await conn.ExecuteAsync(
                            @"UPDATE dbo.Chargen
                              SET Gesperrt = 1
                              WHERE ID = @id",
                            new { id = row.ID });

                        await InsertSperrlagerLogAsync(conn, row.ID, row.Charge, "Gesperrt", bereich, grund, benutzer, personalnummer, lagerortQRCode);
                    }
                    else
                    {
                        await conn.ExecuteAsync(
                            @"UPDATE dbo.Chargen
                              SET Gesperrt = 0
                              WHERE ID = @id",
                            new { id = row.ID });

                        await InsertSperrlagerLogAsync(conn, row.ID, row.Charge, "Entsperrt", bereich, grund, benutzer, personalnummer, lagerortQRCode);
                    }

                    result.Found.Add(row);
                }
            }

            if (result.Found.Count > 0)
            {
                await ActivityLogService.InsertLogAsync(benutzer, $"[Sperrlager] {grund}: {string.Join(", ", result.Found.Select(c => c.Charge))}");
            }

            return result;
        }

        private static async Task InsertSperrlagerLogAsync(SqlConnection conn, int chargenId, string charge, string aktion, string bereich, string grund, string benutzer, string personalnummer, string? lagerortQRCode)
        {
            await conn.ExecuteAsync(
                @"INSERT INTO dbo.Sperrlager
                    (Chargen_ID, Charge, Aktion, Bereich, Grund, GesperrtVon, GesperrtVonPersonalnummer, GesperrtAm, EntsperrtVon, EntsperrtAm, VermuelltVon, VermuelltAm, LagerortQRCode, Bemerkung)
                  VALUES
                    (@ChargenId, @Charge, @Aktion, @Bereich, @Grund,
                     CASE WHEN @Aktion = N'Gesperrt' THEN @Benutzer ELSE NULL END,
                     CASE WHEN @Aktion = N'Gesperrt' THEN @Personalnummer ELSE NULL END,
                     SYSDATETIME(),
                     CASE WHEN @Aktion = N'Entsperrt' THEN @Benutzer ELSE NULL END,
                     CASE WHEN @Aktion = N'Entsperrt' THEN SYSDATETIME() ELSE NULL END,
                     CASE WHEN @Aktion = N'Vermüllt' THEN @Benutzer ELSE NULL END,
                     CASE WHEN @Aktion = N'Vermüllt' THEN SYSDATETIME() ELSE NULL END,
                     @LagerortQRCode, @Grund);",
                new
                {
                    ChargenId = chargenId,
                    Charge = charge,
                    Aktion = aktion,
                    Bereich = bereich,
                    Grund = grund,
                    Benutzer = string.IsNullOrWhiteSpace(benutzer) ? "System" : benutzer,
                    Personalnummer = string.IsNullOrWhiteSpace(personalnummer) ? "System" : personalnummer,
                    LagerortQRCode = lagerortQRCode
                });
        }

        private async Task MoveChargesToLagerortAsync(List<string> charges, string lagerortQRCode)
        {
            await RemoveChargesFromAllLagerorteAsync(charges);

            using var conn = new SqlConnection(SqlManager.connectionString);
            await conn.OpenAsync();

            var existing = await conn.QueryFirstOrDefaultAsync<string?>(
                "SELECT AktuelleCharge FROM dbo.Lagerorte WHERE QRCode = @qr",
                new { qr = lagerortQRCode });

            var merged = ParseChargeText(existing ?? "")
                .Concat(charges)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            await conn.ExecuteAsync(
                @"UPDATE dbo.Lagerorte
                  SET AktuelleCharge = @charges,
                      LetzteNutzung = SYSDATETIME()
                  WHERE QRCode = @qr",
                new { charges = string.Join(", ", merged), qr = lagerortQRCode });
        }

        private async Task RemoveChargesFromAllLagerorteAsync(List<string> charges)
        {
            if (charges.Count == 0) return;

            using var conn = new SqlConnection(SqlManager.connectionString);
            await conn.OpenAsync();

            var rows = await conn.QueryAsync<(string QRCode, string? AktuelleCharge)>(
                @"SELECT QRCode, AktuelleCharge
                  FROM dbo.Lagerorte
                  WHERE AktuelleCharge IS NOT NULL
                    AND LTRIM(RTRIM(AktuelleCharge)) <> ''");

            foreach (var row in rows)
            {
                var current = ParseChargeText(row.AktuelleCharge ?? "");
                var filtered = current
                    .Where(c => !charges.Contains(c, StringComparer.OrdinalIgnoreCase))
                    .ToList();

                if (filtered.Count == current.Count)
                {
                    continue;
                }

                await conn.ExecuteAsync(
                    @"UPDATE dbo.Lagerorte
                      SET AktuelleCharge = @charges,
                          LetzteNutzung = CASE WHEN @charges = '' THEN NULL ELSE LetzteNutzung END
                      WHERE QRCode = @qr",
                    new { charges = string.Join(", ", filtered), qr = row.QRCode });
            }
        }
    }
}
