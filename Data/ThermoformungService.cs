using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QIN_Production_Web.Data
{
    public class EingabeDaten
    {
        public int? ID { get; set; }
        public string MaschinenNr { get; set; } = "";
        public string Begin { get; set; } = "";
        public string Ende { get; set; } = "";
        public string Pausen { get; set; } = "";
        public string Ausfall { get; set; } = "";
        public string Bemerkung { get; set; } = "";
        public bool IsNewRowAuftrag { get; set; }
    }

    public class FChargeDaten
    {
        public string Charge { get; set; } = "";
        public string Flfmlst { get; set; } = "";
        public string Verbraucht { get; set; } = "";
        public string Gutteile { get; set; } = "";
        public string Schlechtteile { get; set; } = "";
        public string SchlechtteileExt { get; set; } = "";
        public string WKZTemp { get; set; } = "";
        public string FolienTemp { get; set; } = "";
        public string FIFO { get; set; } = "";
        public bool IsNewRow { get; set; }
    }

    public class ChargeComboItem
    {
        public string Charge { get; set; } = "";
        public DateTime Eingangsdatum { get; set; }
        public string DisplayText { get; set; } = "";
        public bool IsRed { get; set; }
    }

    public class ThermoformungService
    {
        public static async Task<List<string>> GetOpenFANrsAsync()
        {
            var list = new List<string>();
            string query = @"SELECT FA_Nr FROM Thermo_Auftrag WHERE (Abgesendet = 0 OR Abgesendet IS NULL) AND Erstellungsdatum >= DATEADD(MONTH, -2, GETDATE()) GROUP BY FA_Nr ORDER BY MIN(ID) ASC;";
            using (SqlConnection connection = new SqlConnection(SqlManager.FertigungConnectionString))
            {
                await connection.OpenAsync();
                using (SqlCommand command = new SqlCommand(query, connection))
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync()) list.Add(reader["FA_Nr"]?.ToString() ?? "");
                }
            }
            return list;
        }

        public static async Task<(List<EingabeDaten> Eingaben, List<FChargeDaten> Chargen, string Material, string Liefermenge)> GetDataForFANrAsync(string faNr)
        {
            var eingaben = new List<EingabeDaten>();
            var chargen = new List<FChargeDaten>();
            string material = "Kein Material gefunden";
            string liefermenge = "0";

            string eingabeQuery = "SELECT ID, Maschinen_Nr, Begin_Datum, End_Datum, Pausenzeit, Ausfallzeit, Bemerkung, Material, Liefermenge FROM dbo.Thermo_Auftrag WHERE FA_Nr = @FANr ORDER BY ID ASC";
            string chargenQuery = "SELECT Charge, Echte_Menge, Lfm_Ist, Gutteile, Schlechtteile, Schlechtteile_Ext, WKZ_Temp, Folien_Temp, FIFO FROM dbo.Chargen WHERE FA_Nr = @FANr";

            try
            {
                using (SqlConnection con = new SqlConnection(SqlManager.FertigungConnectionString))
                {
                    await con.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand(eingabeQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@FANr", faNr);
                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                material = reader["Material"]?.ToString() ?? material;
                                liefermenge = reader["Liefermenge"]?.ToString() ?? liefermenge;
                                if (reader["Maschinen_Nr"] != DBNull.Value)
                                {
                                    eingaben.Add(new EingabeDaten
                                    {
                                        ID = Convert.ToInt32(reader["ID"]),
                                        MaschinenNr = reader["Maschinen_Nr"].ToString() ?? "",
                                        Begin = reader["Begin_Datum"].ToString() ?? "",
                                        Ende = reader["End_Datum"].ToString() ?? "",
                                        Pausen = reader["Pausenzeit"].ToString() ?? "",
                                        Ausfall = reader["Ausfallzeit"].ToString() ?? "",
                                        Bemerkung = reader["Bemerkung"].ToString() ?? "",
                                        IsNewRowAuftrag = false
                                    });
                                }
                            }
                        }
                    }

                    using (SqlCommand cmd = new SqlCommand(chargenQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@FANr", faNr);
                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                chargen.Add(new FChargeDaten
                                {
                                    Charge = reader["Charge"].ToString() ?? "",
                                    Flfmlst = reader["Lfm_Ist"].ToString() ?? "",
                                    Verbraucht = reader["Echte_Menge"].ToString() ?? "",
                                    Gutteile = reader["Gutteile"].ToString() ?? "",
                                    Schlechtteile = reader["Schlechtteile"].ToString() ?? "",
                                    SchlechtteileExt = reader["Schlechtteile_Ext"].ToString() ?? "",
                                    WKZTemp = reader["WKZ_Temp"].ToString() ?? "",
                                    FolienTemp = reader["Folien_Temp"].ToString() ?? "",
                                    FIFO = reader["FIFO"]?.ToString() ?? "",
                                    IsNewRow = false
                                });
                            }
                        }
                    }
                }
            } 
            catch (Exception ex) { Console.WriteLine(ex.Message); }
            return (eingaben, chargen, material, liefermenge);
        }

        public static async Task<List<ChargeComboItem>> GetAvailableChargenAsync(string faNr)
        {
            var list = new List<ChargeComboItem>();
            string materialNumber = "";
            string artikelToSearch = "";

            try {
                using (SqlConnection con = new SqlConnection(SqlManager.FertigungConnectionString))
                {
                    await con.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand("SELECT Material FROM dbo.Thermo_Auftrag WHERE FA_Nr = @FANr;", con))
                    {
                        cmd.Parameters.AddWithValue("@FANr", faNr);
                        var mat = await cmd.ExecuteScalarAsync();
                        if (mat != null) materialNumber = mat.ToString()?.Split(';')[0]?.Trim() ?? "";
                    }

                    if (string.IsNullOrEmpty(materialNumber)) return list;

                    using (SqlConnection conMain = new SqlConnection(SqlManager.connectionString))
                    {
                        await conMain.OpenAsync();
                        using (SqlCommand cmd = new SqlCommand("SELECT Suchbegriff FROM Materialliste WHERE Nr = @MaterialNumber;", conMain))
                        {
                            cmd.Parameters.AddWithValue("@MaterialNumber", materialNumber);
                            var art = await cmd.ExecuteScalarAsync();
                            if (art != null) artikelToSearch = art.ToString() ?? "";
                        }
                    }

                    if (string.IsNullOrEmpty(artikelToSearch)) return list;

                    string sq = @"SELECT c.Charge, w.Eingangsdatum FROM Chargen c JOIN Wareneingang w ON w.ID = c.Wareneingang_ID WHERE w.Artikel LIKE '%' + @Artikel + '%' AND (c.Gutteile IS NULL OR c.Gutteile = 0) AND c.Gesperrt = 0 AND w.Eingangsdatum >= DATEADD(MONTH, -6, GETDATE()) ORDER BY w.Eingangsdatum ASC, c.ID ASC";
                    using (SqlCommand cmd2 = new SqlCommand(sq, con))
                    {
                        cmd2.Parameters.AddWithValue("@Artikel", artikelToSearch);
                        using (SqlDataReader r = await cmd2.ExecuteReaderAsync())
                        {
                            while (await r.ReadAsync())
                            {
                                string c = r["Charge"]?.ToString() ?? "";
                                DateTime d = r["Eingangsdatum"] != DBNull.Value ? Convert.ToDateTime(r["Eingangsdatum"]) : DateTime.MaxValue;
                                list.Add(new ChargeComboItem { Charge = c, Eingangsdatum = d, DisplayText = $"{c} | {(d != DateTime.MaxValue ? d.ToString("dd.MM.yyyy") : "")}" });
                            }
                        }
                    }

                    // IsRed logic
                    DateTime oldest = DateTime.MaxValue;
                    foreach(var c in list) if(c.Eingangsdatum < oldest) oldest = c.Eingangsdatum;
                    if(oldest != DateTime.MaxValue) 
                    {
                        foreach(var c in list) c.IsRed = (c.Eingangsdatum > oldest.AddDays(2));
                    }
                }
            } catch (Exception ex) { Console.WriteLine(ex.Message); }
            return list;
        }

        public static async Task<bool> SaveDataAsync(string faNr, bool abgesendet, List<EingabeDaten> eingaben, List<FChargeDaten> chargen, string material, string personalNr, string userName)
        {
            Console.WriteLine($"SaveDataAsync started for {faNr}. Eingaben: {eingaben?.Count}, Chargen: {chargen?.Count}");
            try
            {
                using (SqlConnection con = new SqlConnection(SqlManager.FertigungConnectionString))
                {
                    await con.OpenAsync();
                    Console.WriteLine("Connection opened.");


                    // Eingaben
                    foreach (var row in eingaben ?? new List<EingabeDaten>())
                    {
                        int? reusableId = null;
                        if (row.IsNewRowAuftrag)
                        {
                            using (SqlCommand checkCmd = new SqlCommand("SELECT TOP 1 ID FROM dbo.Thermo_Auftrag WHERE FA_Nr = @FANr AND Maschinen_Nr IS NULL AND Begin_Datum IS NULL AND End_Datum IS NULL", con))
                            {
                                checkCmd.Parameters.AddWithValue("@FANr", faNr);
                                var res = await checkCmd.ExecuteScalarAsync();
                                if (res != null && res != DBNull.Value) reusableId = Convert.ToInt32(res);
                            }
                        }

                        string q = reusableId.HasValue || row.ID.HasValue
                            ? "UPDATE dbo.Thermo_Auftrag SET Maschinen_Nr=@MaschinenNr, Begin_Datum=@BeginDate, End_Datum=@EndeDate, Pausenzeit=@Pausenzeit, Ausfallzeit=@Ausfallzeit, Bemerkung=@Bemerkung, Personal_Nr=@PersonalNr, Erstellungsdatum=@Erstellungsdatum, Abgesendet=@Abgesendet, Material=@Material WHERE ID=@ID"
                            : row.IsNewRowAuftrag
                                ? "INSERT INTO dbo.Thermo_Auftrag (FA_Nr, Maschinen_Nr, Begin_Datum, End_Datum, Pausenzeit, Ausfallzeit, Bemerkung, Personal_Nr, Erstellungsdatum, Abgesendet, Material) VALUES (@FANr, @MaschinenNr, @BeginDate, @EndeDate, @Pausenzeit, @Ausfallzeit, @Bemerkung, @PersonalNr, @Erstellungsdatum, @Abgesendet, @Material)"
                                : "UPDATE dbo.Thermo_Auftrag SET Maschinen_Nr=@MaschinenNr, Begin_Datum=@BeginDate, End_Datum=@EndeDate, Pausenzeit=@Pausenzeit, Ausfallzeit=@Ausfallzeit, Bemerkung=@Bemerkung, Personal_Nr=@PersonalNr, Erstellungsdatum=@Erstellungsdatum, Abgesendet=@Abgesendet, Material=@Material WHERE FA_Nr=@FANr AND ID=(SELECT MIN(ID) FROM Thermo_Auftrag WHERE FA_Nr=@FANr)"; 

                        using (SqlCommand cmd = new SqlCommand(q, con))
                        {
                            if (reusableId.HasValue) cmd.Parameters.AddWithValue("@ID", reusableId.Value);
                            else if (row.ID.HasValue) cmd.Parameters.AddWithValue("@ID", row.ID.Value);

                            cmd.Parameters.AddWithValue("@FANr", faNr);
                            cmd.Parameters.AddWithValue("@MaschinenNr", row.MaschinenNr ?? "");
                            
                            // Safe Date Parsing
                            DateTime beginDate = DateTime.TryParse(row.Begin, out var b) ? b : DateTime.Now;
                            DateTime endeDate = DateTime.TryParse(row.Ende, out var e) ? e : DateTime.Now;

                            cmd.Parameters.AddWithValue("@BeginDate", beginDate);
                            cmd.Parameters.AddWithValue("@EndeDate", endeDate);
                            
                            cmd.Parameters.AddWithValue("@Pausenzeit", row.Pausen ?? "");
                            cmd.Parameters.AddWithValue("@Ausfallzeit", row.Ausfall ?? "");
                            cmd.Parameters.AddWithValue("@Bemerkung", row.Bemerkung ?? "");
                            cmd.Parameters.AddWithValue("@PersonalNr", personalNr ?? "0"); 
                            cmd.Parameters.AddWithValue("@Erstellungsdatum", DateTime.Now);
                            cmd.Parameters.AddWithValue("@Abgesendet", abgesendet ? 1 : 0);
                            cmd.Parameters.AddWithValue("@Material", material ?? "");
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }

                    if (abgesendet)
                    {
                        using (SqlCommand cmd = new SqlCommand("UPDATE dbo.Thermo_Auftrag SET Abgesendet = 1 WHERE FA_Nr = @FANr", con))
                        {
                            cmd.Parameters.AddWithValue("@FANr", faNr);
                            await cmd.ExecuteNonQueryAsync();
                        }
                        await LogActivityAsync(userName, $"Thermoformungsauftrag {faNr} erfolgreich abgeschlossen");
                    }
                    else
                    {
                        await LogActivityAsync(userName, $"Thermoformungsauftrag {faNr} wurde für weitere Bearbeitungen gespeichert.");
                    }

                    // Chargen
                    foreach (var charge in chargen ?? new List<FChargeDaten>())
                    {
                        int lfmlst = int.TryParse(charge.Flfmlst, out int lfm) ? lfm : 0;
                        int verbraucht = int.TryParse(charge.Verbraucht, out int verb) ? verb : 0;
                        int gute = int.TryParse(charge.Gutteile, out int g) ? g : 0;
                        int schlechte = int.TryParse(charge.Schlechtteile, out int s) ? s : 0;
                        int schlechtext = int.TryParse(charge.SchlechtteileExt, out int se) ? se : 0;
                        int aktuelleMenge = lfmlst - verbraucht;

                        bool chargeExists = false;
                        using (SqlCommand checkCmd = new SqlCommand("SELECT COUNT(1) FROM dbo.Chargen WHERE Charge = @Charge", con))
                        {
                            checkCmd.Parameters.AddWithValue("@Charge", charge.Charge ?? "");
                            chargeExists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;
                        }

                        string qC = !chargeExists
                            ? "INSERT INTO dbo.Chargen (Charge, Aktuelle_Menge, Lfm_Ist, Gutteile, Schlechtteile, Schlechtteile_Ext, FA_Nr, WKZ_Temp, Folien_Temp, Status_ID, FIFO) VALUES (@Charge, @Aktuelle_Menge, @LfmIst, @Gute, @Schlechte, @SchlechteExt, @FANr, @WKZTemp, @FolienTemp, @Status_ID, @FIFO)"
                            : "WITH CTE AS (SELECT TOP 1 * FROM dbo.Chargen WHERE Charge = @Charge) UPDATE CTE SET Aktuelle_Menge = @Aktuelle_Menge, Lfm_Ist = @LfmIst, Gutteile = @Gute, Schlechtteile = @Schlechte, Schlechtteile_Ext = @SchlechteExt, FA_Nr = @FANr, WKZ_Temp = @WKZTemp, Folien_Temp = @FolienTemp, Status_ID = @Status_ID, FIFO = @FIFO";

                        using (SqlCommand cmd = new SqlCommand(qC, con))
                        {
                            cmd.Parameters.AddWithValue("@Charge", charge.Charge ?? "");
                            cmd.Parameters.AddWithValue("@Aktuelle_Menge", aktuelleMenge);
                            cmd.Parameters.AddWithValue("@LfmIst", lfmlst);
                            cmd.Parameters.AddWithValue("@Gute", gute);
                            cmd.Parameters.AddWithValue("@Schlechte", schlechte);
                            cmd.Parameters.AddWithValue("@SchlechteExt", schlechtext);
                            cmd.Parameters.AddWithValue("@FANr", faNr);
                            cmd.Parameters.AddWithValue("@WKZTemp", charge.WKZTemp ?? "");
                            cmd.Parameters.AddWithValue("@FolienTemp", charge.FolienTemp ?? "");
                            if (string.IsNullOrEmpty(charge.FIFO)) cmd.Parameters.AddWithValue("@FIFO", DBNull.Value);
                            else cmd.Parameters.AddWithValue("@FIFO", charge.FIFO);
                            cmd.Parameters.AddWithValue("@Status_ID", aktuelleMenge <= 0 ? 3 : 1);
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                }
                Console.WriteLine("SaveDataAsync completed successfully.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SaveDataAsync ERROR: {ex.Message}");
                if (ex.InnerException != null) Console.WriteLine($"Inner: {ex.InnerException.Message}");
                return false;
            }
        }

        public static async Task<bool> CheckFreigabeAsync(string faNr)
        {
            if (string.IsNullOrEmpty(faNr)) return false;
            string query = "SELECT COUNT(1) FROM dbo.Freigaben WHERE FA_Nr = @FANr AND (Typ = 'thermo' OR Typ = 'thermoformung') AND CAST(Datum AS DATE) = CAST(GETDATE() AS DATE)";
            try
            {
                using (SqlConnection con = new SqlConnection(SqlManager.connectionString))
                {
                    await con.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@FANr", faNr);
                        return Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
                    }
                }
            } catch { return false; }
        }

        public static async Task<List<(DateTime Datum, string User)>> GetFreigabenListAsync(string faNr)
        {
            var list = new List<(DateTime, string)>();
            string query = "SELECT Datum, [User] FROM dbo.Freigaben WHERE FA_Nr = @FANr AND (Typ = 'thermo' OR Typ = 'thermoformung') ORDER BY Datum DESC";
            try
            {
                using (SqlConnection con = new SqlConnection(SqlManager.connectionString))
                {
                    await con.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@FANr", faNr);
                        using (var r = await cmd.ExecuteReaderAsync())
                        {
                            while (await r.ReadAsync())
                                list.Add((Convert.ToDateTime(r["Datum"]), r["User"]?.ToString() ?? "Unbekannt"));
                        }
                    }
                }
            } catch { }
            return list;
        }

        public static async Task<bool> AddFreigabeAsync(string faNr, string user)
        {
            string query = "INSERT INTO dbo.Freigaben (FA_Nr, [User], Datum, Typ) VALUES (@FANr, @User, GETDATE(), 'thermo')";
            try
            {
                using (SqlConnection con = new SqlConnection(SqlManager.connectionString))
                {
                    await con.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@FANr", faNr);
                        cmd.Parameters.AddWithValue("@User", user);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                await LogActivityAsync(user, $"[Kiefel/Parco] Freigabe erteilt für FA-Nr: {faNr}");
                return true;
            } catch { return false; }
        }

        private static async Task LogActivityAsync(string user, string detail)
        {
            string query = "INSERT INTO dbo.Logs (Zeitstempel, [User], Aktivitaet) VALUES (GETDATE(), @User, @Detail)";
            try
            {
                using (SqlConnection con = new SqlConnection(SqlManager.connectionString))
                {
                    await con.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@User", user);
                        cmd.Parameters.AddWithValue("@Detail", detail);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            } catch { }
        }
    }
}
