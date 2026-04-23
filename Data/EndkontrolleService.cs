using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QIN_Production_Web.Data
{
    public class CustomerData
    {
        public string Name { get; set; } = "";
        public bool IsActive { get; set; }
    }

    public class EndkontrolleEintrag
    {
        public int ID { get; set; }
        public string Charge { get; set; } = "";
        public string FANr { get; set; } = "";
        public string Kunde { get; set; } = "";
        public string Projekt { get; set; } = "";
        public string Artikel { get; set; } = "";
        public string Dekor { get; set; } = "";
        public DateTime Datum { get; set; } = DateTime.Today;
        
        public int Gutteile { get; set; }
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
        
        public string Bemerkung { get; set; } = "";
        public string Personalnummer { get; set; } = "100";
    }

    public class EndkontrolleService
    {
        public static async Task<List<CustomerData>> GetCustomersAsync()
        {
            var list = new List<CustomerData>();
            string query = "SELECT Kunde, MAX(CAST(IstAktiv AS INT)) FROM dbo.Kunden WHERE Kunde IS NOT NULL AND Kunde <> '' GROUP BY Kunde ORDER BY MAX(CAST(IstAktiv AS INT)) DESC, Kunde";
            try
            {
                using (var con = new SqlConnection(SqlManager.connectionString))
                {
                    await con.OpenAsync();
                    using (var cmd = new SqlCommand(query, con))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            list.Add(new CustomerData { Name = reader.GetString(0), IsActive = !reader.IsDBNull(1) && reader.GetInt32(1) == 1 });
                        }
                    }
                }
            } catch { }
            return list;
        }

        public static async Task<List<string>> GetProjectsAsync(string kunde)
        {
            var list = new List<string>();
            try
            {
                using (var con = new SqlConnection(SqlManager.connectionString))
                {
                    await con.OpenAsync();
                    using (var cmd = new SqlCommand("SELECT DISTINCT Projekt FROM dbo.Kunden WHERE Kunde = @Kunde AND Projekt IS NOT NULL AND Projekt <> '' ORDER BY Projekt", con))
                    {
                        cmd.Parameters.AddWithValue("@Kunde", kunde);
                        using (var r = await cmd.ExecuteReaderAsync())
                            while (await r.ReadAsync()) list.Add(r.GetString(0));
                    }
                }
            } catch { }
            return list;
        }

        public static async Task<(List<string> Artikels, List<string> Dekors)> GetArtikelsAndDekorsAsync(string project)
        {
            var artikels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var dekors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using (var con = new SqlConnection(SqlManager.connectionString))
                {
                    await con.OpenAsync();
                    using (var cmd = new SqlCommand("SELECT DISTINCT Artikel, Dekor FROM dbo.Kunden WHERE Projekt = @Projekt", con))
                    {
                        cmd.Parameters.AddWithValue("@Projekt", project);
                        using (var r = await cmd.ExecuteReaderAsync())
                        {
                            while (await r.ReadAsync())
                            {
                                if (!r.IsDBNull(0)) { foreach (var x in r.GetString(0).Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)) artikels.Add(x.Trim()); }
                                if (!r.IsDBNull(1)) { foreach (var x in r.GetString(1).Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)) dekors.Add(x.Trim()); }
                            }
                        }
                    }
                }
            } catch { }
            return (new List<string>(artikels), new List<string>(dekors));
        }

        public static async Task<(bool Success, string Message)> InsertEintragAsync(EndkontrolleEintrag e, string userName)
        {
            try
            {
                using (var con = new SqlConnection(SqlManager.connectionString))
                {
                    await con.OpenAsync();
                    string q = @"INSERT INTO dbo.Table1 
                        (Kunde, Projekt, Artikel, Dekor, Charge, FSKdate, Gutteile, Fusseln, Nadelstiche, Pickel, Dekorfehler, Color, Flecken, Nebel, Vertiefung, Oelflecken, Tiefziehfehler, Fraesfehler, Knicke, Kratzer, Personalnummer, [FA-Nr], Bemerkungen) 
                        VALUES (@kunde, @projekt, @artikel, @dekor, @charge, @FSKdate, @gutteile, @fusseln, @nadelstiche, @pickel, @dekorfehler, @color, @flecken, @nebel, @vertiefung, @oelflecken, @tiefziehfehler, @fraesfehler, @knicke, @kratzer, @personalnummer, @FANr, @bemerkungen)";
                    
                    using (var cmd = new SqlCommand(q, con))
                    {
                        cmd.Parameters.AddWithValue("@kunde", e.Kunde);
                        cmd.Parameters.AddWithValue("@projekt", e.Projekt);
                        cmd.Parameters.AddWithValue("@artikel", e.Artikel);
                        cmd.Parameters.AddWithValue("@dekor", e.Dekor);
                        cmd.Parameters.AddWithValue("@charge", e.Charge);
                        cmd.Parameters.AddWithValue("@FSKdate", e.Datum.ToString("yyyy-MM-dd"));
                        cmd.Parameters.AddWithValue("@gutteile", e.Gutteile);
                        cmd.Parameters.AddWithValue("@fusseln", e.Fusseln);
                        cmd.Parameters.AddWithValue("@nadelstiche", e.Nadelstiche);
                        cmd.Parameters.AddWithValue("@pickel", e.Pickel);
                        cmd.Parameters.AddWithValue("@dekorfehler", e.Dekorfehler);
                        cmd.Parameters.AddWithValue("@color", e.Farbfehler);
                        cmd.Parameters.AddWithValue("@flecken", e.Flecken);
                        cmd.Parameters.AddWithValue("@nebel", e.Nebel);
                        cmd.Parameters.AddWithValue("@vertiefung", e.Vertiefung);
                        cmd.Parameters.AddWithValue("@oelflecken", e.Oelflecken);
                        cmd.Parameters.AddWithValue("@tiefziehfehler", e.Tiefziehfehler);
                        cmd.Parameters.AddWithValue("@fraesfehler", e.Fraesfehler);
                        cmd.Parameters.AddWithValue("@knicke", e.Knicke);
                        cmd.Parameters.AddWithValue("@kratzer", e.Kratzer);
                        cmd.Parameters.AddWithValue("@personalnummer", e.Personalnummer);
                        cmd.Parameters.AddWithValue("@FANr", e.FANr);
                        cmd.Parameters.AddWithValue("@bemerkungen", e.Bemerkung);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                await ActivityLogService.InsertLogAsync(userName, $"[Sauberraum] Fehlersammelkarte für Charge {e.Charge} wurde erfolgreich erstellt. Bemerkung: {e.Bemerkung}");
                return (true, "Erfolgreich gespeichert.");
            } catch (Exception ex) { return (false, ex.Message); }
        }

        public static async Task<List<EndkontrolleEintrag>> GetRecentEintraegeAsync(string personalnummer = "100")
        {
            var list = new List<EndkontrolleEintrag>();
            try
            {
                using (var con = new SqlConnection(SqlManager.connectionString))
                {
                    await con.OpenAsync();
                    string q = @"SELECT TOP 10 ID, Charge, [FA-Nr], Kunde, Projekt, Artikel, Dekor, Gutteile, Fusseln, Nadelstiche, Pickel, Dekorfehler, Color, Flecken, Nebel, Vertiefung, Oelflecken, Tiefziehfehler, Fraesfehler, Knicke, Kratzer, FSKdate, Bemerkungen 
                                 FROM dbo.Table1 WHERE Personalnummer = @Personalnummer ORDER BY ID DESC";
                    using (var cmd = new SqlCommand(q, con))
                    {
                        cmd.Parameters.AddWithValue("@Personalnummer", personalnummer);
                        using (var r = await cmd.ExecuteReaderAsync())
                        {
                            while (await r.ReadAsync())
                            {
                                var e = new EndkontrolleEintrag
                                {
                                    ID = Convert.ToInt32(r["ID"]),
                                    Charge = r["Charge"]?.ToString() ?? "",
                                    FANr = r["FA-Nr"]?.ToString() ?? "",
                                    Datum = r["FSKdate"] == DBNull.Value ? DateTime.Today : Convert.ToDateTime(r["FSKdate"]),
                                    Kunde = r["Kunde"]?.ToString() ?? "",
                                    Projekt = r["Projekt"]?.ToString() ?? "",
                                    Artikel = r["Artikel"]?.ToString() ?? "",
                                    Dekor = r["Dekor"]?.ToString() ?? "",
                                    Gutteile = r["Gutteile"] == DBNull.Value ? 0 : Convert.ToInt32(r["Gutteile"]),
                                    
                                    Fusseln = r["Fusseln"] == DBNull.Value ? 0 : Convert.ToInt32(r["Fusseln"]),
                                    Nadelstiche = r["Nadelstiche"] == DBNull.Value ? 0 : Convert.ToInt32(r["Nadelstiche"]),
                                    Pickel = r["Pickel"] == DBNull.Value ? 0 : Convert.ToInt32(r["Pickel"]),
                                    Dekorfehler = r["Dekorfehler"] == DBNull.Value ? 0 : Convert.ToInt32(r["Dekorfehler"]),
                                    Farbfehler = r["Color"] == DBNull.Value ? 0 : Convert.ToInt32(r["Color"]),
                                    Flecken = r["Flecken"] == DBNull.Value ? 0 : Convert.ToInt32(r["Flecken"]),
                                    Nebel = r["Nebel"] == DBNull.Value ? 0 : Convert.ToInt32(r["Nebel"]),
                                    Vertiefung = r["Vertiefung"] == DBNull.Value ? 0 : Convert.ToInt32(r["Vertiefung"]),
                                    
                                    Oelflecken = r["Oelflecken"] == DBNull.Value ? 0 : Convert.ToInt32(r["Oelflecken"]),
                                    Tiefziehfehler = r["Tiefziehfehler"] == DBNull.Value ? 0 : Convert.ToInt32(r["Tiefziehfehler"]),
                                    Fraesfehler = r["Fraesfehler"] == DBNull.Value ? 0 : Convert.ToInt32(r["Fraesfehler"]),
                                    Knicke = r["Knicke"] == DBNull.Value ? 0 : Convert.ToInt32(r["Knicke"]),
                                    Kratzer = r["Kratzer"] == DBNull.Value ? 0 : Convert.ToInt32(r["Kratzer"]),

                                    Bemerkung = r["Bemerkungen"]?.ToString() ?? ""
                                };
                                list.Add(e);
                            }
                        }
                    }
                }
            } catch { }
            return list;
        }

        public static async Task<(bool Success, string Message)> UpdateEintragFieldAsync(int id, string field, object value, string userName)
        {
            try
            {
                using (var con = new SqlConnection(SqlManager.connectionString))
                {
                    await con.OpenAsync();
                    // Map display-friendly field names to DB names if necessary, though we'll pass safe names from the UI
                    string q = $"UPDATE dbo.Table1 SET [{field}] = @value WHERE ID = @ID";
                    using (var cmd = new SqlCommand(q, con))
                    {
                        cmd.Parameters.AddWithValue("@value", value ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ID", id);
                        int affected = await cmd.ExecuteNonQueryAsync();
                        if (affected > 0)
                        {
                            await ActivityLogService.InsertLogAsync(userName, $"[Sauberraum] Eintrag ID {id}: Feld '{field}' wurde auf '{value}' geändert.");
                            return (true, "Aktualisiert");
                        }
                        return (false, "Eintrag nicht gefunden.");
                    }
                }
            } catch (Exception ex) { return (false, ex.Message); }
        }

        public static async Task<(bool Success, string Message)> UpdateEintragAsync(EndkontrolleEintrag e, string userName)
        {
            try
            {
                using (var con = new SqlConnection(SqlManager.connectionString))
                {
                    await con.OpenAsync();
                    string q = @"UPDATE dbo.Table1 SET
                                    Kunde = @kunde,
                                    Projekt = @projekt,
                                    Artikel = @artikel,
                                    Dekor = @dekor,
                                    Charge = @charge,
                                    FSKdate = @FSKdate,
                                    Gutteile = @gutteile,
                                    Fusseln = @fusseln,
                                    Nadelstiche = @nadelstiche,
                                    Pickel = @pickel,
                                    Dekorfehler = @dekorfehler,
                                    Color = @color,
                                    Flecken = @flecken,
                                    Nebel = @nebel,
                                    Vertiefung = @vertiefung,
                                    Oelflecken = @oelflecken,
                                    Tiefziehfehler = @tiefziehfehler,
                                    Fraesfehler = @fraesfehler,
                                    Knicke = @knicke,
                                    Kratzer = @kratzer,
                                    Personalnummer = @personalnummer,
                                    [FA-Nr] = @FANr,
                                    Bemerkungen = @bemerkungen
                                 WHERE ID = @ID";

                    using (var cmd = new SqlCommand(q, con))
                    {
                        cmd.Parameters.AddWithValue("@ID", e.ID);
                        cmd.Parameters.AddWithValue("@kunde", e.Kunde);
                        cmd.Parameters.AddWithValue("@projekt", e.Projekt);
                        cmd.Parameters.AddWithValue("@artikel", e.Artikel);
                        cmd.Parameters.AddWithValue("@dekor", e.Dekor);
                        cmd.Parameters.AddWithValue("@charge", e.Charge);
                        cmd.Parameters.AddWithValue("@FSKdate", e.Datum.ToString("yyyy-MM-dd"));
                        cmd.Parameters.AddWithValue("@gutteile", e.Gutteile);
                        cmd.Parameters.AddWithValue("@fusseln", e.Fusseln);
                        cmd.Parameters.AddWithValue("@nadelstiche", e.Nadelstiche);
                        cmd.Parameters.AddWithValue("@pickel", e.Pickel);
                        cmd.Parameters.AddWithValue("@dekorfehler", e.Dekorfehler);
                        cmd.Parameters.AddWithValue("@color", e.Farbfehler);
                        cmd.Parameters.AddWithValue("@flecken", e.Flecken);
                        cmd.Parameters.AddWithValue("@nebel", e.Nebel);
                        cmd.Parameters.AddWithValue("@vertiefung", e.Vertiefung);
                        cmd.Parameters.AddWithValue("@oelflecken", e.Oelflecken);
                        cmd.Parameters.AddWithValue("@tiefziehfehler", e.Tiefziehfehler);
                        cmd.Parameters.AddWithValue("@fraesfehler", e.Fraesfehler);
                        cmd.Parameters.AddWithValue("@knicke", e.Knicke);
                        cmd.Parameters.AddWithValue("@kratzer", e.Kratzer);
                        cmd.Parameters.AddWithValue("@personalnummer", e.Personalnummer);
                        cmd.Parameters.AddWithValue("@FANr", e.FANr);
                        cmd.Parameters.AddWithValue("@bemerkungen", e.Bemerkung);

                        int affected = await cmd.ExecuteNonQueryAsync();
                        if (affected > 0)
                        {
                            await ActivityLogService.InsertLogAsync(userName, $"[Sauberraum] Fehlersammelkarte ID {e.ID} wurde aktualisiert. Charge: {e.Charge}");
                            return (true, "Aktualisiert");
                        }

                        return (false, "Eintrag nicht gefunden.");
                    }
                }
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        public static async Task<(bool Success, string Message)> DeleteEintragAsync(int id, string userName)
        {
            try
            {
                 using (var con = new SqlConnection(SqlManager.connectionString))
                {
                    await con.OpenAsync();
                    using (var cmd = new SqlCommand("DELETE FROM dbo.Table1 WHERE ID = @ID", con))
                    {
                        cmd.Parameters.AddWithValue("@ID", id);
                        int affected = await cmd.ExecuteNonQueryAsync();
                        if (affected > 0)
                        {
                            await ActivityLogService.InsertLogAsync(userName, $"[Sauberraum] Eintrag mit ID {id} wurde erfolgreich gelöscht.");
                            return (true, "Gelöscht");
                        }
                        return (false, "Eintrag nicht gefunden.");
                    }
                }
            } catch (Exception ex) { return (false, ex.Message); }
        }
    }
}
