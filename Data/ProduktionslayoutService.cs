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
        public string? PaletteCharge { get; set; }
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

            // 2. Hole den Artikelname aus dem Wareneingang
            string? artikel = null;
            if (!string.IsNullOrWhiteSpace(aktuelleCharge))
            {
                using (var conn = new SqlConnection(SqlManager.FertigungConnectionString))
                {
                    var query = "SELECT TOP 1 Artikel FROM dbo.Wareneingang WHERE Palette = @palette";
                    artikel = await conn.QueryFirstOrDefaultAsync<string>(query, new { palette = aktuelleCharge });
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

            var paletteToPlaces = new Dictionary<string, List<PlatzInfo>>(StringComparer.OrdinalIgnoreCase);
            var palettes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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
                        var palette = reader.IsDBNull(1) ? null : reader.GetString(1);

                        if (!result.TryGetValue(qr, out var info)) continue;
                        info.PaletteCharge = palette;

                        if (!string.IsNullOrWhiteSpace(palette))
                        {
                            palettes.Add(palette);
                            if (!paletteToPlaces.TryGetValue(palette, out var list))
                            {
                                list = new List<PlatzInfo>();
                                paletteToPlaces[palette] = list;
                            }
                            list.Add(info);
                        }
                    }
                }
            }

            if (palettes.Count == 0) return result;
            var paletteList = palettes.ToList();

            var paletteToWE = new Dictionary<string, List<(int id, string artikel, DateTime eingang)>>(StringComparer.OrdinalIgnoreCase);
            var weIdToChargeData = new Dictionary<int, List<(string Charge, int Aktuelle, int Echte)>>();

            using (var conn = new SqlConnection(SqlManager.FertigungConnectionString))
            {
                await conn.OpenAsync();
                var weIds = new HashSet<int>();

                using (var cmd = conn.CreateCommand())
                {
                    var pNames = new List<string>();
                    for (int i = 0; i < paletteList.Count; i++)
                    {
                        string p = "@pal" + i;
                        pNames.Add(p);
                        cmd.Parameters.AddWithValue(p, paletteList[i]);
                    }

                    cmd.CommandText = $@"
                        SELECT ID, Palette, Artikel, Eingangsdatum
                        FROM dbo.Wareneingang
                        WHERE Palette IN ({string.Join(", ", pNames)});
                    ";

                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        int id = reader.GetInt32(0);
                        string palette = reader.GetString(1);
                        string artikel = reader.IsDBNull(2) ? "" : reader.GetString(2);
                        DateTime eingang = reader.GetDateTime(3);

                        weIds.Add(id);
                        if (!paletteToWE.TryGetValue(palette, out var list))
                        {
                            list = new List<(int, string, DateTime)>();
                            paletteToWE[palette] = list;
                        }
                        list.Add((id, artikel, eingang));
                    }
                }

                if (weIds.Count > 0)
                {
                    var weIdList = weIds.ToList();
                    using (var cmd = conn.CreateCommand())
                    {
                        var pNames = new List<string>();
                        for (int i = 0; i < weIdList.Count; i++)
                        {
                            string p = "@we" + i;
                            pNames.Add(p);
                            cmd.Parameters.AddWithValue(p, weIdList[i]);
                        }

                        cmd.CommandText = $@"
                            SELECT Wareneingang_ID, Charge, Aktuelle_Menge, Echte_Menge
                            FROM dbo.Chargen
                            WHERE Wareneingang_ID IN ({string.Join(", ", pNames)});
                        ";

                        using var reader = await cmd.ExecuteReaderAsync();
                        while (await reader.ReadAsync())
                        {
                            int weId = reader.GetInt32(0);
                            string charge = reader.IsDBNull(1) ? "" : reader.GetString(1);
                            int akt = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                            int echt = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);

                            if (string.IsNullOrWhiteSpace(charge) || akt <= 0) continue;

                            if (!weIdToChargeData.TryGetValue(weId, out var list))
                            {
                                list = new List<(string, int, int)>();
                                weIdToChargeData[weId] = list;
                            }
                            list.Add((charge, akt, echt));
                        }
                    }
                }
            }

            foreach (var kvp in paletteToPlaces)
            {
                var palette = kvp.Key;
                var places = kvp.Value;
                if (paletteToWE.TryGetValue(palette, out var weRows))
                {
                    foreach (var pi in places)
                    {
                        foreach (var we in weRows.OrderByDescending(x => x.eingang))
                        {
                            pi.Wareneingaenge.Add(new WareneingangInfo { Artikel = we.artikel, Eingangsdatum = we.eingang });
                        }

                        var chargeSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        long sumAkt = 0, sumEcht = 0;

                        foreach (var we in weRows)
                        {
                            if (weIdToChargeData.TryGetValue(we.id, out var cDataList))
                            {
                                foreach (var item in cDataList)
                                {
                                    chargeSet.Add(item.Charge);
                                    sumAkt += item.Aktuelle;
                                    sumEcht += item.Echte;
                                }
                            }
                        }

                        pi.Charges.AddRange(chargeSet.OrderBy(c => c));
                        pi.SumAktuelleMenge = sumAkt;
                        pi.SumEchteMenge = sumEcht;
                    }
                }
            }

            return result;
        }
    }
}
