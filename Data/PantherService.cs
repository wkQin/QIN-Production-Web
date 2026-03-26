using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QIN_Production_Web.Data
{
    public class PantherAuftragsData
    {
        public string? FANR { get; set; }
        public string? Material { get; set; }
        public DateTime? EintragsDate { get; set; }
        public int? Liefermenge { get; set; }
        public int? MaschinenNr { get; set; }
    }

    public class PantherService
    {
        public static async Task<List<PantherAuftragsData>> GetAuftraegeAsync()
        {
            var list = new List<PantherAuftragsData>();
            string query = "SELECT TOP 100 FA_NR, Material, Liefermenge, ErstellungsDatum, Maschinen_Nr FROM dbo.Thermo_Auftrag ORDER BY ErstellungsDatum DESC"; 

            try
            {
                using (SqlConnection con = new SqlConnection(SqlManager.FertigungConnectionString))
                {
                    await con.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            list.Add(new PantherAuftragsData
                            {
                                FANR = reader["FA_Nr"]?.ToString(),
                                Material = reader["Material"]?.ToString(),
                                EintragsDate = reader["ErstellungsDatum"] != DBNull.Value ? (DateTime?)reader["ErstellungsDatum"] : null,
                                Liefermenge = reader["Liefermenge"] != DBNull.Value ? Convert.ToInt32(reader["Liefermenge"]) : null,
                                MaschinenNr = reader["Maschinen_Nr"] != DBNull.Value ? Convert.ToInt32(reader["Maschinen_Nr"]) : null
                            });
                        }
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
            return list;
        }

        public static async Task<(bool Success, string Message, string NewZustand)> UpdateChargeZustandAsync(string charge, bool ausschuss)
        {
            if (string.IsNullOrEmpty(charge)) return (false, "Bitte scanne eine Charge.", "");

            try
            {
                using (SqlConnection con = new SqlConnection(SqlManager.FertigungConnectionString))
                {
                    await con.OpenAsync();
                    
                    // Check if charge exists
                    string selectQ = "SELECT Charge FROM dbo.Chargen WHERE Charge = @charge";
                    using (SqlCommand checkCmd = new SqlCommand(selectQ, con))
                    {
                        checkCmd.Parameters.AddWithValue("@charge", charge);
                        var result = await checkCmd.ExecuteScalarAsync();
                        
                        if (result != null)
                        {
                            string newZustand = ausschuss ? "Ausschuss" : "Sammelausschuss";
                            string updateQ = "UPDATE dbo.Chargen SET Zustand = @zustand WHERE Charge = @charge";
                            using (SqlCommand updateCmd = new SqlCommand(updateQ, con))
                            {
                                updateCmd.Parameters.AddWithValue("@zustand", newZustand);
                                updateCmd.Parameters.AddWithValue("@charge", charge);
                                await updateCmd.ExecuteNonQueryAsync();
                            }
                            return (true, $"Charge {charge} aktualisiert", newZustand);
                        }
                        else
                        {
                            return (false, "Charge existiert nicht.", "");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return (false, $"Fehler: {ex.Message}", "");
            }
        }
    }
}
