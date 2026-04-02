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
        public long SumAktuelleMenge { get; set; }
        public long SumEchteMenge { get; set; }
        public List<WareneingangInfo> Wareneingaenge { get; set; } = new();
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
                        SELECT QRCode, AktuelleCharge
                        FROM dbo.Lagerorte
                        WHERE QRCode IN ({string.Join(", ", paramNames)});
                    ";

                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var qr = reader.GetString(0);
                        var chargeRaw = reader.IsDBNull(1) ? null : reader.GetString(1);

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
    }
}
