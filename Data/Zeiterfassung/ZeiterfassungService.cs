using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace QIN_Production_Web.Data.Zeiterfassung
{
    public class ZeiterfassungSollSettings
    {
        public int WochenMinuten { get; set; } = 2250;
        public int ArbeitstageProWoche { get; set; } = 5;
        public int TaglicheArbeitszeit { get; set; } = 450;
        public string NichtArbeitstage { get; set; } = "";
    }

    public class ZeiterfassungService
    {
        public async Task<bool> LoadCanViewOtherUsersAsync(string anmeldename)
        {
            if (string.IsNullOrWhiteSpace(anmeldename)) return false;

            const string sql = @"SELECT Zeiterfassung_Verwalten FROM dbo.LoginDaten WHERE Benutzer = @u;";
            await using var con = new SqlConnection(SqlManager.connectionString);
            await using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.Add("@u", SqlDbType.NVarChar).Value = anmeldename;

            await con.OpenAsync();
            var val = await cmd.ExecuteScalarAsync();
            if (val == null || val == DBNull.Value) return false;
            return Convert.ToInt32(val) == 1;
        }

        public async Task<List<ZeiterfassungUserItem>> LoadUsersFromLoginDatenAsync()
        {
            var list = new List<ZeiterfassungUserItem>();
            const string sql = @"SELECT Benutzer FROM dbo.LoginDaten ORDER BY Benutzer;";
            await using var con = new SqlConnection(SqlManager.connectionString);
            await using var cmd = new SqlCommand(sql, con);

            await con.OpenAsync();
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                list.Add(new ZeiterfassungUserItem { Benutzer = r.GetString(0) });
            }
            return list;
        }

        public async Task<ZeiterfassungSollSettings> LoadWeeklySollForSelectedUserAsync(string benutzer)
        {
            var settings = new ZeiterfassungSollSettings();
            
            try
            {
                const string sql = @"
                SELECT WochenMinuten, ArbeitstageProWoche, Tagliche_Arbeitszeit, Nicht_Arbeitstage
                FROM dbo.LoginDaten
                WHERE Benutzer = @benutzer;";

                await using var con = new SqlConnection(SqlManager.connectionString);
                await using var cmd = new SqlCommand(sql, con);
                cmd.Parameters.Add("@benutzer", SqlDbType.NVarChar).Value = benutzer;

                await con.OpenAsync();
                await using var r = await cmd.ExecuteReaderAsync();

                if (await r.ReadAsync())
                {
                    if (!r.IsDBNull(0)) settings.WochenMinuten = Convert.ToInt32(r.GetValue(0));
                    if (!r.IsDBNull(1)) settings.ArbeitstageProWoche = Convert.ToInt32(r.GetValue(1));
                    if (r.FieldCount > 2 && !r.IsDBNull(2)) settings.TaglicheArbeitszeit = Convert.ToInt32(r.GetValue(2));
                    if (r.FieldCount > 3 && !r.IsDBNull(3)) settings.NichtArbeitstage = r.GetString(3);
                }
            }
            catch (SqlException)
            {
                const string sqlFallback = @"
                SELECT WochenMinuten, ArbeitstageProWoche
                FROM dbo.LoginDaten
                WHERE Benutzer = @benutzer;";

                await using var con2 = new SqlConnection(SqlManager.connectionString);
                await using var cmd2 = new SqlCommand(sqlFallback, con2);
                cmd2.Parameters.Add("@benutzer", SqlDbType.NVarChar).Value = benutzer;

                await con2.OpenAsync();
                await using var r2 = await cmd2.ExecuteReaderAsync();

                if (await r2.ReadAsync())
                {
                    if (!r2.IsDBNull(0)) settings.WochenMinuten = Convert.ToInt32(r2.GetValue(0));
                    if (!r2.IsDBNull(1)) settings.ArbeitstageProWoche = Convert.ToInt32(r2.GetValue(1));
                }
            }

            return settings;
        }

        public async Task<List<Booking>> LoadBookingsFromDatabaseAsync(int year, int month, string benutzer)
        {
            var list = new List<Booking>();
            var start = new DateTime(year, month, 1, 0, 0, 0);
            var end = start.AddMonths(1);

            const string sql = @"
                SELECT Id, Zeitstempel, Aktion, Manuel, Homeoffice, Bemerkung
                FROM dbo.Zeiterfassung
                WHERE Benutzername = @user
                  AND Zeitstempel >= @start
                  AND Zeitstempel <  @end
                ORDER BY Zeitstempel ASC;";

            await using var con = new SqlConnection(SqlManager.connectionString);
            await using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.Add("@user", SqlDbType.NVarChar).Value = benutzer;
            cmd.Parameters.Add("@start", SqlDbType.DateTime2).Value = start;
            cmd.Parameters.Add("@end", SqlDbType.DateTime2).Value = end;

            await con.OpenAsync();
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                var id = r.GetInt32(0);
                var ts = r.GetDateTime(1);
                string actionRaw = r.GetString(2);

                bool isManual = false;
                if (!r.IsDBNull(3))
                {
                    object o = r.GetValue(3);
                    if (o is bool b) isManual = b;
                    else isManual = Convert.ToInt32(o) != 0;
                }

                bool isHomeOffice = false;
                if (!r.IsDBNull(4))
                {
                    object o = r.GetValue(4);
                    if (o is bool b) isHomeOffice = b;
                    else isHomeOffice = Convert.ToInt32(o) != 0;
                }

                string? note = r.IsDBNull(5) ? null : r.GetString(5);

                list.Add(new Booking(id, ts, ParseAction(actionRaw), isManual, isHomeOffice, note));
            }

            return list;
        }

        private static BookingAction ParseAction(string actionRaw)
        {
            if (string.Equals(actionRaw, "KOMMEN", StringComparison.OrdinalIgnoreCase))
                return BookingAction.Kommen;
            if (string.Equals(actionRaw, "GEHEN", StringComparison.OrdinalIgnoreCase))
                return BookingAction.Gehen;
            if (int.TryParse(actionRaw, out int n))
                return n == 0 ? BookingAction.Kommen : BookingAction.Gehen;

            return BookingAction.Kommen; // Fallback
        }

        public async Task InsertManualBookingAsync(string benutzer, DateTime timestamp, BookingAction action, bool homeoffice, string note)
        {
            const string sql = @"
            INSERT INTO dbo.Zeiterfassung (ChipId, Benutzername, Zeitstempel, Aktion, Geraetename, Homeoffice, Bemerkung)
            VALUES (@chipid, @user, @ts, @aktion, @geraetename, @homeoffice, @note);";

            await using var con = new SqlConnection(SqlManager.connectionString);
            await using var cmd = new SqlCommand(sql, con);

            cmd.Parameters.Add("@chipid", SqlDbType.NVarChar).Value = "Manuell-Web";
            cmd.Parameters.Add("@user", SqlDbType.NVarChar).Value = benutzer;
            cmd.Parameters.Add("@ts", SqlDbType.DateTime2).Value = timestamp;
            cmd.Parameters.Add("@aktion", SqlDbType.NVarChar).Value = action == BookingAction.Kommen ? "KOMMEN" : "GEHEN";
            cmd.Parameters.Add("@geraetename", SqlDbType.NVarChar).Value = "QIN-Production-Web";
            cmd.Parameters.Add("@homeoffice", SqlDbType.Bit).Value = homeoffice;
            cmd.Parameters.Add("@note", SqlDbType.NVarChar).Value = (object?)note ?? DBNull.Value;

            await con.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteBookingAsync(int bookingId)
        {
            const string sql = @"DELETE FROM dbo.Zeiterfassung WHERE Id = @id;";
            await using var con = new SqlConnection(SqlManager.connectionString);
            await using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.Add("@id", SqlDbType.Int).Value = bookingId;

            await con.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpsertAbsenceAsync(string benutzer, DateOnly day, string typ)
        {
            const string sql = @"
            IF EXISTS (
                SELECT 1 FROM dbo.ZE_Abwesenheit
                WHERE Benutzer = @b AND Von = @d AND Bis = @d
            )
            BEGIN
                UPDATE dbo.ZE_Abwesenheit SET Typ = @typ
                WHERE Benutzer = @b AND Von = @d AND Bis = @d;
            END
            ELSE
            BEGIN
                INSERT INTO dbo.ZE_Abwesenheit (Benutzer, Von, Bis, Typ)
                VALUES (@b, @d, @d, @typ);
            END";

            await using var con = new SqlConnection(SqlManager.connectionString);
            await using var cmd = new SqlCommand(sql, con);

            cmd.Parameters.Add("@b", SqlDbType.NVarChar).Value = benutzer;
            cmd.Parameters.Add("@d", SqlDbType.Date).Value = day.ToDateTime(TimeOnly.MinValue).Date;
            cmd.Parameters.Add("@typ", SqlDbType.NVarChar).Value = typ;

            await con.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
