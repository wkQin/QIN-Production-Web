using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace QIN_Production_Web.Data.Zeiterfassung
{
    public sealed class ZeiterfassungCalendarRepository
    {
        public Dictionary<DateOnly, string> LoadFeiertage(int year, int month)
        {
            var dict = new Dictionary<DateOnly, string>();
            var start = new DateTime(year, month, 1);
            var end = start.AddMonths(1);

            const string sql = @"
                SELECT Datum, Name
                FROM dbo.ZE_Feiertage
                WHERE IstBetriebsfrei = 1
                  AND Datum >= @start
                  AND Datum <  @end;";

            using var con = new SqlConnection(SqlManager.connectionString);
            using var cmd = new SqlCommand(sql, con);

            cmd.Parameters.Add("@start", SqlDbType.Date).Value = start.Date;
            cmd.Parameters.Add("@end", SqlDbType.Date).Value = end.Date;

            con.Open();
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var date = DateOnly.FromDateTime(r.GetDateTime(0));
                var name = r.IsDBNull(1) ? "Feiertag" : r.GetString(1);
                dict[date] = name;
            }

            return dict;
        }

        public Dictionary<DateOnly, string> LoadAbwesenheiten(int year, int month, string benutzer)
        {
            var dict = new Dictionary<DateOnly, string>();
            var start = new DateTime(year, month, 1);
            var end = start.AddMonths(1);

            const string sql = @"
                SELECT Von, Bis, Typ
                FROM dbo.ZE_Abwesenheit
                WHERE Benutzer = @b
                  AND Bis >= @start
                  AND Von <  @end;";

            using var con = new SqlConnection(SqlManager.connectionString);
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.Add("@b", SqlDbType.NVarChar).Value = benutzer;
            cmd.Parameters.Add("@start", SqlDbType.Date).Value = start.Date;
            cmd.Parameters.Add("@end", SqlDbType.Date).Value = end.Date;

            con.Open();
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var von = DateOnly.FromDateTime(r.GetDateTime(0));
                var bis = DateOnly.FromDateTime(r.GetDateTime(1));
                var typ = r.IsDBNull(2) ? "Abwesend" : r.GetString(2);

                var monthStart = DateOnly.FromDateTime(start);
                var monthEnd = DateOnly.FromDateTime(end).AddDays(-1);

                if (von < monthStart) von = monthStart;
                if (bis > monthEnd) bis = monthEnd;

                for (var d = von; d <= bis; d = d.AddDays(1))
                    dict[d] = typ;
            }

            return dict;
        }
    }
}
