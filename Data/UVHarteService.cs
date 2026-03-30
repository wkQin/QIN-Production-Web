using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QIN_Production_Web.Data
{
    public class UVCharge
    {
        public string Charge { get; set; } = "";
        public int Menge { get; set; }
        public int Ausschuss { get; set; }
    }

    public class UVAuftrag
    {
        public int ID { get; set; }
        public DateTime Datum { get; set; } = DateTime.Today;
        public TimeOnly Beginnzeit { get; set; } = new TimeOnly(6, 0);
        public TimeOnly Endzeit { get; set; } = new TimeOnly(14, 0);
        public int Pausenzeit { get; set; }
        public int Ausfallzeit { get; set; }
        public string Bemerkung { get; set; } = "";
        public bool IsNew { get; set; }
    }

    public class UVHarteService
    {
        public static async Task<List<string>> GetOpenFANrsAsync()
        {
            var list = new List<string>();
            string query = @"
                SELECT UA.FA_Nr FROM UV_Auftrag UA WHERE ISNULL(UA.Abgesendet, 0) = 0
                UNION
                SELECT TA.FA_Nr FROM Thermo_Auftrag TA WHERE TA.Abgesendet = 1 AND TA.Material LIKE '%Rollo%' 
                AND NOT EXISTS (SELECT 1 FROM UV_Auftrag UA2 WHERE UA2.FA_Nr = TA.FA_Nr AND UA2.Abgesendet = 1)
                ORDER BY 1;";

            try
            {
                using (SqlConnection con = new SqlConnection(SqlManager.FertigungConnectionString))
                {
                    await con.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync()) list.Add(reader["FA_Nr"]?.ToString() ?? "");
                    }
                }
            } catch (Exception ex) { Console.WriteLine(ex.Message); }
            return list;
        }

        public static async Task<(List<UVCharge> Chargen, string Material, string Nummer, string Anweisung, List<UVAuftrag> Auftraege)> GetDataForFaAsync(string faNr)
        {
            var chargen = new List<UVCharge>();
            var auftraege = new List<UVAuftrag>();
            string material = "Unbekannt";
            string nummer = "Unbekannt";
            string anweisung = "Arbeitsanweisung und UV Datenblatt beachten.";

            try
            {
                using (SqlConnection con = new SqlConnection(SqlManager.FertigungConnectionString))
                {
                    await con.OpenAsync();

                    // Chargen
                    using (SqlCommand cmd = new SqlCommand("SELECT Charge, UVMenge, UVAuschuss FROM Chargen WHERE FA_Nr = @faNr", con))
                    {
                        cmd.Parameters.AddWithValue("@faNr", faNr);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                chargen.Add(new UVCharge
                                {
                                    Charge = reader["Charge"]?.ToString() ?? "",
                                    Menge = reader["UVMenge"] == DBNull.Value ? 0 : Convert.ToInt32(reader["UVMenge"]),
                                    Ausschuss = reader["UVAuschuss"] == DBNull.Value ? 0 : Convert.ToInt32(reader["UVAuschuss"])
                                });
                            }
                        }
                    }

                    // Artikelinfo
                    using (SqlCommand cmd = new SqlCommand("SELECT Material, Anweisung FROM Thermo_Auftrag WHERE FA_Nr = @faNr", con))
                    {
                        cmd.Parameters.AddWithValue("@faNr", faNr);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var matVal = reader["Material"]?.ToString() ?? "";
                                if (reader["Anweisung"] != DBNull.Value && !string.IsNullOrWhiteSpace(reader["Anweisung"].ToString()))
                                {
                                    anweisung = reader["Anweisung"].ToString()!;
                                }

                                if (matVal.Contains(";"))
                                {
                                    var parts = matVal.Split(';');
                                    nummer = parts[0].Trim();
                                    material = parts.Length > 1 ? parts[1].Trim() : "";
                                }
                                else
                                {
                                    nummer = matVal;
                                    material = "Unbekannt";
                                }
                            }
                        }
                    }

                    // Aufträge
                    using (SqlCommand cmd = new SqlCommand("SELECT ID, Datum, Beginnzeit, Endzeit, Pausenzeit, Ausfallzeit, Bemerkung, Abgesendet FROM UV_Auftrag WHERE FA_Nr = @faNr", con))
                    {
                        cmd.Parameters.AddWithValue("@faNr", faNr);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                auftraege.Add(new UVAuftrag
                                {
                                    ID = Convert.ToInt32(reader["ID"]),
                                    Datum = Convert.ToDateTime(reader["Datum"]),
                                    Beginnzeit = TimeOnly.FromTimeSpan((TimeSpan)reader["Beginnzeit"]),
                                    Endzeit = TimeOnly.FromTimeSpan((TimeSpan)reader["Endzeit"]),
                                    Pausenzeit = Convert.ToInt32(reader["Pausenzeit"]),
                                    Ausfallzeit = Convert.ToInt32(reader["Ausfallzeit"]),
                                    Bemerkung = reader["Bemerkung"]?.ToString() ?? "",
                                    IsNew = false
                                });
                            }
                        }
                    }
                }
            } catch (Exception ex) { Console.WriteLine(ex.Message); }

            return (chargen, material, nummer, anweisung, auftraege);
        }

        public static async Task<bool> SaveDataAsync(string faNr, bool abgesendet, List<UVAuftrag> auftraege, List<UVCharge> chargen, string userName = "Unbekannt")
        {
            try
            {
                using (SqlConnection con = new SqlConnection(SqlManager.FertigungConnectionString))
                {
                    await con.OpenAsync();

                    foreach (var auftrag in auftraege)
                    {
                        if (auftrag.IsNew)
                        {
                            string q = "INSERT INTO UV_Auftrag (Maschinen_Nr, FA_Nr, Datum, Beginnzeit, Endzeit, Pausenzeit, Ausfallzeit, Bemerkung, Benutzer, Abgesendet) VALUES (@Maschinen_Nr, @FA_Nr, @Datum, @Beginnzeit, @Endzeit, @Pausenzeit, @Ausfallzeit, @Bemerkung, @Benutzer, @Abgesendet)";
                            using (SqlCommand cmd = new SqlCommand(q, con))
                            {
                                cmd.Parameters.AddWithValue("@Maschinen_Nr", 5230);
                                cmd.Parameters.AddWithValue("@FA_Nr", faNr);
                                cmd.Parameters.AddWithValue("@Datum", auftrag.Datum);
                                cmd.Parameters.AddWithValue("@Beginnzeit", auftrag.Beginnzeit.ToTimeSpan());
                                cmd.Parameters.AddWithValue("@Endzeit", auftrag.Endzeit.ToTimeSpan());
                                cmd.Parameters.AddWithValue("@Pausenzeit", auftrag.Pausenzeit);
                                cmd.Parameters.AddWithValue("@Ausfallzeit", auftrag.Ausfallzeit);
                                cmd.Parameters.AddWithValue("@Bemerkung", string.IsNullOrWhiteSpace(auftrag.Bemerkung) ? DBNull.Value : auftrag.Bemerkung);
                                cmd.Parameters.AddWithValue("@Benutzer", "100"); // Standard Benutzer
                                cmd.Parameters.AddWithValue("@Abgesendet", abgesendet ? 1 : 0);
                                await cmd.ExecuteNonQueryAsync();
                            }
                            auftrag.IsNew = false;
                        }
                        else
                        {
                            string q = "UPDATE UV_Auftrag SET Datum=@Datum, Beginnzeit=@Beginnzeit, Endzeit=@Endzeit, Pausenzeit=@Pausenzeit, Ausfallzeit=@Ausfallzeit, Bemerkung=@Bemerkung, Abgesendet=@Abgesendet WHERE ID=@ID";
                            using (SqlCommand cmd = new SqlCommand(q, con))
                            {
                                cmd.Parameters.AddWithValue("@Datum", auftrag.Datum);
                                cmd.Parameters.AddWithValue("@Beginnzeit", auftrag.Beginnzeit.ToTimeSpan());
                                cmd.Parameters.AddWithValue("@Endzeit", auftrag.Endzeit.ToTimeSpan());
                                cmd.Parameters.AddWithValue("@Pausenzeit", auftrag.Pausenzeit);
                                cmd.Parameters.AddWithValue("@Ausfallzeit", auftrag.Ausfallzeit);
                                cmd.Parameters.AddWithValue("@Bemerkung", string.IsNullOrWhiteSpace(auftrag.Bemerkung) ? DBNull.Value : auftrag.Bemerkung);
                                cmd.Parameters.AddWithValue("@Abgesendet", abgesendet ? 1 : 0);
                                cmd.Parameters.AddWithValue("@ID", auftrag.ID);
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }
                    }

                    foreach (var charge in chargen)
                    {
                        using (SqlCommand cmd = new SqlCommand("UPDATE Chargen SET UVMenge = @UVMenge, UVAuschuss = @UVAuschuss WHERE Charge = @Charge", con))
                        {
                            cmd.Parameters.AddWithValue("@UVMenge", charge.Menge);
                            cmd.Parameters.AddWithValue("@UVAuschuss", charge.Ausschuss);
                            cmd.Parameters.AddWithValue("@Charge", charge.Charge);
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                }
                
                await ActivityLogService.InsertLogAsync(userName, abgesendet 
                    ? $"[UV Härte] Auftrag {faNr} erfolgreich abgeschlossen." 
                    : $"[UV Härte] Auftrag {faNr} wurde für weitere Bearbeitungen gespeichert.");
                    
                return true;
            } catch (Exception ex) { Console.WriteLine(ex.Message); return false; }
        }

        public static async Task<bool> CheckFreigabeAsync(string faNr)
        {
            return await Task.FromResult(true); // Mock like before
        }
    }
}
