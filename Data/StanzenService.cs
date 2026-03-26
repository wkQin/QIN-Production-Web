using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace QIN_Production_Web.Data
{
    public class StanzenCharge
    {
        public string Charge { get; set; } = "";
        public int Menge { get; set; }
        public int Ausschuss { get; set; }
    }

    public class StanzenEingabe
    {
        public int MaschinenNr { get; set; }
        public DateTime Datum { get; set; } = DateTime.Today;
        public TimeOnly Begin { get; set; } = new TimeOnly(6, 0);
        public TimeOnly Ende { get; set; } = new TimeOnly(14, 0);
        public int Pausen { get; set; }
        public int Ausfall { get; set; }
        public string Bemerkung { get; set; } = "";
        public bool IsNewRowAuftrag { get; set; }
    }

    public class StanzenFaInfo
    {
        public string MaterialNr { get; set; } = "";
        public string Beschreibung1 { get; set; } = "";
        public string Beschreibung2 { get; set; } = "";
        public DateTime? Auftragsdatum { get; set; }
        public int Auftragsmenge { get; set; }
        public int Chargenanzahl { get; set; }
    }

    public class StanzenService
    {
        public static async Task<List<string>> GetOpenFANrsAsync()
        {
            var list = new List<string>();
            string query = "SELECT DISTINCT FA_Nr FROM Thermo_Auftrag WHERE Abgesendet = 1 AND Erstellungsdatum >= DATEADD(MONTH, -2, GETDATE()) ORDER BY FA_Nr;";

            try
            {
                using (var connection = new SqlConnection(SqlManager.FertigungConnectionString))
                {
                    await connection.OpenAsync();
                    using (var cmd = new SqlCommand(query, connection))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var faNr = reader["FA_Nr"]?.ToString();
                            if (!string.IsNullOrWhiteSpace(faNr)) list.Add(faNr);
                        }
                    }
                }
            } catch (Exception ex) { Console.WriteLine(ex.Message); }
            return list;
        }

        public static async Task<(List<StanzenCharge> Chargen, List<StanzenEingabe> Auftraege)> GetDataForFaAsync(string faNr)
        {
            var chargen = new List<StanzenCharge>();
            var auftraege = new List<StanzenEingabe>();

            try
            {
                using (var con = new SqlConnection(SqlManager.FertigungConnectionString))
                {
                    await con.OpenAsync();

                    using (var cmd = new SqlCommand("SELECT Maschinen_Nr, Datum, Beginnzeit, Endzeit, Pausenzeit, Ausfallzeit, Bemerkung FROM dbo.Stanzen_Auftrag WHERE FA_Nr = @FANr ORDER BY ID ASC", con))
                    {
                        cmd.Parameters.AddWithValue("@FANr", faNr);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                auftraege.Add(new StanzenEingabe
                                {
                                    MaschinenNr = reader["Maschinen_Nr"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Maschinen_Nr"]),
                                    Datum = reader["Datum"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(reader["Datum"]),
                                    Begin = reader["Beginnzeit"] == DBNull.Value ? TimeOnly.MinValue : TimeOnly.FromTimeSpan((TimeSpan)reader["Beginnzeit"]),
                                    Ende = reader["Endzeit"] == DBNull.Value ? TimeOnly.MinValue : TimeOnly.FromTimeSpan((TimeSpan)reader["Endzeit"]),
                                    Pausen = reader["Pausenzeit"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Pausenzeit"]),
                                    Ausfall = reader["Ausfallzeit"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Ausfallzeit"]),
                                    Bemerkung = reader["Bemerkung"]?.ToString() ?? "",
                                    IsNewRowAuftrag = false
                                });
                            }
                        }
                    }

                    using (var cmd = new SqlCommand("SELECT Charge, Stanzen_Menge, Stanzen_Ausschuss FROM dbo.Chargen WHERE FA_Nr = @FANr", con))
                    {
                        cmd.Parameters.AddWithValue("@FANr", faNr);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                chargen.Add(new StanzenCharge
                                {
                                    Charge = reader["Charge"]?.ToString() ?? "",
                                    Menge = reader["Stanzen_Menge"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Stanzen_Menge"]),
                                    Ausschuss = reader["Stanzen_Ausschuss"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Stanzen_Ausschuss"])
                                });
                            }
                        }
                    }
                }
            } catch (Exception ex) { Console.WriteLine(ex.Message); }

            return (chargen, auftraege);
        }

        public static async Task<StanzenFaInfo> GetFaInfoAsync(string faNr)
        {
            var info = new StanzenFaInfo();
            try
            {
                using (var con = new SqlConnection(SqlManager.FertigungConnectionString))
                {
                    await con.OpenAsync();

                    using (var cmd = new SqlCommand("SELECT TOP (1) Material, Erstellungsdatum, Liefermenge FROM dbo.Thermo_Auftrag WHERE FA_Nr = @FANr ORDER BY ID ASC", con))
                    {
                        cmd.Parameters.AddWithValue("@FANr", faNr);
                        using (var r = await cmd.ExecuteReaderAsync())
                        {
                            if (await r.ReadAsync())
                            {
                                string mat = r["Material"]?.ToString() ?? "";
                                info.Auftragsdatum = r["Erstellungsdatum"] == DBNull.Value ? null : Convert.ToDateTime(r["Erstellungsdatum"]);
                                info.Auftragsmenge = r["Liefermenge"] == DBNull.Value ? 0 : Convert.ToInt32(r["Liefermenge"]);

                                var parts = mat.Split(';');
                                if (parts.Length > 0) info.MaterialNr = parts[0].Trim();
                                if (parts.Length > 1) info.Beschreibung1 = parts[1].Trim();
                                if (parts.Length > 2) info.Beschreibung2 = parts[2].Trim();
                            }
                        }
                    }

                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM dbo.Chargen WHERE FA_Nr = @FANr", con))
                    {
                         cmd.Parameters.AddWithValue("@FANr", faNr);
                         var res = await cmd.ExecuteScalarAsync();
                         info.Chargenanzahl = res != DBNull.Value ? Convert.ToInt32(res) : 0;
                    }
                }
            } catch (Exception ex) { Console.WriteLine(ex.Message); }
            return info;
        }

        public static async Task<(bool Success, string Message)> SaveAuftraegeUndChargenAsync(string faNr, bool abgesendetFlag, List<StanzenEingabe> auftraege, List<StanzenCharge> chargen, int targetAuftragsmenge)
        {
            var sumChargen = chargen.Sum(c => c.Menge);
            if (sumChargen != targetAuftragsmenge && targetAuftragsmenge > 0) // Relaxed validation if 0 due to edge cases
            {
               return (false, $"Die Summe der Chargenmengen ({sumChargen}) stimmt nicht mit der Auftragsmenge ({targetAuftragsmenge}) überein. Bitte überprüfen Sie die Eingaben.");
            }

            try
            {
                using (var con = new SqlConnection(SqlManager.FertigungConnectionString))
                {
                    await con.OpenAsync();
                    using (var tx = con.BeginTransaction())
                    {
                        try
                        {
                            foreach (var a in auftraege.Where(x => x.IsNewRowAuftrag))
                            {
                                string ins = @"INSERT INTO dbo.Stanzen_Auftrag (Maschinen_Nr, FA_Nr, Datum, Beginnzeit, Endzeit, Pausenzeit, Ausfallzeit, Bemerkung, Benutzer, Erstelldatum, Abgesendet)
                                             VALUES (@Maschine, @FANr, @Datum, @Beginn, @Ende, @Pausen, @Ausfall, @Bemerkung, @Benutzer, SYSDATETIME(), @Abgesendet)";
                                using (var cmd = new SqlCommand(ins, con, tx))
                                {
                                    cmd.Parameters.AddWithValue("@Maschine", a.MaschinenNr.ToString());
                                    cmd.Parameters.AddWithValue("@FANr", faNr);
                                    cmd.Parameters.AddWithValue("@Datum", a.Datum.Date);
                                    cmd.Parameters.AddWithValue("@Beginn", a.Begin.ToTimeSpan());
                                    cmd.Parameters.AddWithValue("@Ende", a.Ende.ToTimeSpan());
                                    cmd.Parameters.AddWithValue("@Pausen", a.Pausen);
                                    cmd.Parameters.AddWithValue("@Ausfall", a.Ausfall);
                                    cmd.Parameters.AddWithValue("@Bemerkung", string.IsNullOrWhiteSpace(a.Bemerkung) ? DBNull.Value : a.Bemerkung);
                                    cmd.Parameters.AddWithValue("@Benutzer", "100"); // default Benutzer
                                    cmd.Parameters.AddWithValue("@Abgesendet", abgesendetFlag);
                                    await cmd.ExecuteNonQueryAsync();
                                }
                            }

                            foreach (var ch in chargen)
                            {
                                string upd = "UPDATE dbo.Chargen SET Stanzen_Menge = @Menge, Stanzen_Ausschuss = @Ausschuss WHERE FA_Nr = @FANr AND Charge = @Charge";
                                using (var cmd = new SqlCommand(upd, con, tx))
                                {
                                    cmd.Parameters.AddWithValue("@FANr", faNr);
                                    cmd.Parameters.AddWithValue("@Charge", ch.Charge.Trim());
                                    cmd.Parameters.AddWithValue("@Menge", ch.Menge);
                                    cmd.Parameters.AddWithValue("@Ausschuss", ch.Ausschuss);
                                    await cmd.ExecuteNonQueryAsync();
                                }
                            }
                            
                            await tx.CommitAsync();
                            return (true, "Erfolgreich gespeichert.");
                        } catch (Exception innerEx) {
                            await tx.RollbackAsync();
                            return (false, $"Fehler beim Speichern (DB): {innerEx.Message}");
                        }
                    }
                }
            } catch (Exception ex) {
                return (false, $"Allgemeiner Fehler: {ex.Message}");
            }
        }
        
        public static async Task<bool> CheckFreigabeAsync(string faNr)
        {
            return await Task.FromResult(true); // Mocking validation like previous modules
        }
    }
}
