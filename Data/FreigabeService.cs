using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace QIN_Production_Web.Data
{
    public class FreigabeRow
    {
        public string FANr { get; set; } = "";
        public DateTime? Datum { get; set; }
        public string Informationen { get; set; } = "";
        public string Bemerkung { get; set; } = "";
        public string Ruecklagemuster { get; set; } = "";
        public string Personalnummer { get; set; } = "";
        public string Quelle { get; set; } = "";
    }

    public enum FreigabeTyp
    {
        Thermoformung,
        UV,
        Stanzen,
        Sauberraum
    }

    public class FreigabeService
    {
        public static async Task<List<FreigabeRow>> GetFreigabenAsync(string faNr, string typ = "alle")
        {
            var result = new List<FreigabeRow>();
            if (string.IsNullOrWhiteSpace(faNr)) return result;

            var tablesByType = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["alle"] = new[] { "Stanzen_Freigabe", "Thermo_Freigabe", "UV_Freigabe" },
                ["stanzen"] = new[] { "Stanzen_Freigabe" },
                ["thermo"] = new[] { "Thermo_Freigabe" },
                ["thermoformung"] = new[] { "Thermo_Freigabe" },
                ["uv"] = new[] { "UV_Freigabe" },
                ["sauberraum"] = new[] { "Sauberraum_Freigabe" }
            };

            if (!tablesByType.TryGetValue(typ ?? "alle", out var tables))
                tables = tablesByType["alle"];

            string BuildSelect(string table) => $@"
            SELECT 
                CAST([FA_Nr] AS nvarchar(50))            AS [FANr],
                CAST([Erstellungsdatum] AS datetime)     AS [Datum],
                CAST([Informationen] AS nvarchar(4000))  AS [Informationen],
                CAST([Bemerkung] AS nvarchar(4000))      AS [Bemerkung],
                CAST([Rücklagemuster] AS nvarchar(10))   AS [Ruecklagemuster],
                CAST([Personal_Nr] AS nvarchar(50))      AS [Personalnummer],
                '{table}'                                AS [Quelle]
            FROM [{table}]
            WHERE [FA_Nr] = @faNr";

            string sql = string.Join("\nUNION ALL\n", tables.Select(BuildSelect)) + "\nORDER BY [Datum] DESC;";

            try
            {
                using (var conn = new SqlConnection(SqlManager.FertigungConnectionString))
                {
                    await conn.OpenAsync();
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.Add("@faNr", SqlDbType.NVarChar, 50).Value = faNr;
                        using (var rdr = await cmd.ExecuteReaderAsync())
                        {
                            while (await rdr.ReadAsync())
                            {
                                result.Add(new FreigabeRow
                                {
                                    FANr = rdr["FANr"]?.ToString() ?? "",
                                    Datum = rdr["Datum"] != DBNull.Value ? (DateTime?)rdr["Datum"] : null,
                                    Informationen = rdr["Informationen"]?.ToString() ?? "",
                                    Bemerkung = rdr["Bemerkung"]?.ToString() ?? "",
                                    Ruecklagemuster = rdr["Ruecklagemuster"]?.ToString() ?? "",
                                    Personalnummer = rdr["Personalnummer"]?.ToString() ?? "",
                                    Quelle = rdr["Quelle"]?.ToString() ?? ""
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
            return result;
        }

        public static async Task<bool> CheckFreigabeTodayAsync(string faNr, FreigabeTyp typ)
        {
            string tableName = GetTableName(typ);
            string query = $"SELECT COUNT(1) FROM dbo.{tableName} WHERE FA_Nr = @FANr AND CAST(Erstellungsdatum AS DATE) = CAST(GETDATE() AS DATE)";
            try
            {
                using (SqlConnection con = new SqlConnection(SqlManager.FertigungConnectionString))
                {
                    await con.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@FANr", faNr);
                        return Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CheckFreigabeTodayAsync Error for {faNr}: {ex.Message}");
                return false;
            }
        }

        public static async Task<int> CheckLoginAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)) return 0;
            try
            {
                using (SqlConnection conn = new SqlConnection(SqlManager.connectionString))
                {
                    await conn.OpenAsync();
                    string query = "SELECT Personalnummer FROM LoginDaten WHERE (Anmeldename = @u OR Personalnummer = @u) AND Password = @p";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@u", username);
                        cmd.Parameters.AddWithValue("@p", password);
                        var res = await cmd.ExecuteScalarAsync();
                        if (res != null && int.TryParse(res.ToString(), out int pn)) return pn;
                    }
                }
            } catch { }
            return 0;
        }

        public static async Task<bool> HasFreigabePermissionAsync(string username)
        {
            try {
                using (SqlConnection conn = new SqlConnection(SqlManager.connectionString))
                {
                    await conn.OpenAsync();
                    string query = "SELECT FreigabeRecht FROM LoginDaten WHERE Anmeldename = @u OR Personalnummer = @u";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@u", username);
                        var res = await cmd.ExecuteScalarAsync();
                        if (res == null || res == DBNull.Value) return false;
                        string val = res.ToString() ?? "";
                        return val == "1" || val.ToLower() == "true";
                    }
                }
            } catch { return false; }
        }

        public static async Task<bool> InsertFreigabeAsync(string faNr, int personalnummer, bool rucklagemuster, string information, string bemerkung, FreigabeTyp typ)
        {
            string tableName = GetTableName(typ);
            string sql = $@"INSERT INTO {tableName} (Fa_Nr, Personal_Nr, Rücklagemuster, Informationen, Bemerkung, Erstellungsdatum) VALUES (@faNr, @pn, @rm, @info, @bem, @datum)";
            try
            {
                using (SqlConnection conn = new SqlConnection(SqlManager.FertigungConnectionString))
                {
                    await conn.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@faNr", faNr);
                        cmd.Parameters.AddWithValue("@pn", personalnummer);
                        cmd.Parameters.AddWithValue("@rm", rucklagemuster); // Send bool (bit) instead of string
                        cmd.Parameters.AddWithValue("@info", information ?? "");
                        cmd.Parameters.AddWithValue("@bem", bemerkung ?? "");
                        cmd.Parameters.AddWithValue("@datum", DateTime.Now);
                        await cmd.ExecuteNonQueryAsync();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"InsertFreigabeAsync Error in {tableName}: {ex.Message}");
                return false;
            }
        }

        private static string GetTableName(FreigabeTyp typ)
        {
            return typ switch
            {
                FreigabeTyp.Thermoformung => "Thermo_Freigabe",
                FreigabeTyp.UV => "UV_Freigabe",
                FreigabeTyp.Stanzen => "Stanzen_Freigabe",
                FreigabeTyp.Sauberraum => "Sauberraum_Freigabe",
                _ => "Thermo_Freigabe"
            };
        }
    }
}
