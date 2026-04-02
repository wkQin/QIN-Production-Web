using ClosedXML.Excel;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace QIN_Production_Web.Data.Zeiterfassung
{
    public sealed class ZeiterfassungExcelExportService
    {
        private static readonly HashSet<string> ExcludedUsers = new(StringComparer.OrdinalIgnoreCase)
        {
            "Dagmar Kranz",
            "Oliver Beiner"
        };

        private static bool IsExcluded(string user)
        {
            if (ExcludedUsers.Contains(user)) return true;
            var normalized = string.Join(" ", user.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
            if (ExcludedUsers.Contains(normalized)) return true;
            return false;
        }

        private readonly ZeiterfassungMath _math;
        private readonly ZeiterfassungCalendarRepository _calendarRepo = new();

        public ZeiterfassungExcelExportService(ZeiterfassungMath math)
        {
            _math = math;
        }

        public byte[] ExportMonthForAllUsersAsBytes(int year, int month)
        {
            var users = LoadUsersFromLoginDaten()
                .Where(u => !IsExcluded(u.Name.Trim()))
                .OrderBy(u => FormatUserLastFirst(u.Name))
                .ToList();

            using var wb = new XLWorkbook();

            foreach (var user in users)
                CreateUserWorksheet(wb, user.Name, year, month);

            wb.Worksheets.First().SetTabActive();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

        private void CreateUserWorksheet(XLWorkbook wb, string benutzer, int year, int month)
        {
            var formattedUser = FormatUserLastFirst(benutzer);
            var ws = wb.Worksheets.Add(SanitizeWorksheetName(formattedUser));

            var bookings = LoadBookingsFromDatabase(year, month, benutzer);
            var results = _math.CalculateMonth(year, month, bookings);

            string monthName = GetGermanMonthName(month);

            ws.Cell("A1").Value = "Zeiterfassung Export";
            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontSize = 16;
            ws.Cell("A2").Value = $"Benutzer: {formattedUser}";
            ws.Cell("A3").Value = $"Monat: {monthName} {year}";

            ws.Range("A1:F1").Merge();
            ws.Range("A2:F2").Merge();
            ws.Range("A3:F3").Merge();

            ws.Cell("A5").Value = "Datum";
            ws.Cell("B5").Value = "Kommen";
            ws.Cell("C5").Value = "Gehen";
            ws.Cell("D5").Value = "Arbeitszeit";
            ws.Cell("E5").Value = "Abwesenheit";
            ws.Cell("F5").Value = "Homeoffice";
            ws.Cell("G5").Value = "Bemerkung";

            var header = ws.Range("A5:G5");
            header.Style.Font.Bold = true;
            header.Style.Fill.BackgroundColor = XLColor.LightGray;

            int row = 6;
            var absences = _calendarRepo.LoadAbwesenheiten(year, month, benutzer);

            for (int i = 0; i < results.Count; i++)
            {
                var day = results[i];
                var date = day.Date.ToDateTime(TimeOnly.MinValue);

                bool isNightShiftKommen = false;
                bool isNightShiftGehen = false;

                if (day.IsMissing)
                {
                    if (day.Bookings.Any(b => b.Action == BookingAction.Kommen))
                    {
                        if (i + 1 < results.Count)
                        {
                            var nextDay = results[i + 1];
                            var firstNext = nextDay.Bookings.OrderBy(b => b.Timestamp).FirstOrDefault();
                            if (firstNext?.Action == BookingAction.Gehen)
                                isNightShiftKommen = true;
                        }
                    }

                    if (day.Bookings.Any(b => b.Action == BookingAction.Gehen))
                    {
                        if (i > 0)
                        {
                            var prevDay = results[i - 1];
                            var lastPrev = prevDay.Bookings.OrderBy(b => b.Timestamp).LastOrDefault();
                            if (lastPrev?.Action == BookingAction.Kommen)
                                isNightShiftGehen = true;
                        }
                    }
                }

                ws.Cell(row, 1).Value = $"{GetGermanWeekday(date.DayOfWeek)}, {date:dd.MM.yyyy}";

                var (startStr, endStr, hasQuestionMark) = GetStartEndStrings(day.Bookings, date, isNightShiftKommen, isNightShiftGehen);

                ws.Cell(row, 2).Value = startStr;
                ws.Cell(row, 2).Style.Alignment.WrapText = true;

                ws.Cell(row, 3).Value = endStr;
                ws.Cell(row, 3).Style.Alignment.WrapText = true;

                if (day.WorkedMinutes > 0)
                {
                    ws.Cell(row, 4).Value = day.WorkedMinutes / 60.0;
                    ws.Cell(row, 4).Style.NumberFormat.Format = "0.00";
                }
                else
                {
                    ws.Cell(row, 4).Value = "";
                }

                var dateOnly = DateOnly.FromDateTime(date);

                ws.Cell(row, 5).Value = absences.TryGetValue(dateOnly, out var abs) ? abs : "";

                ws.Cell(row, 6).Value = day.Bookings.Any(b => b.IsHomeOffice) ? "Ja" : "";

                var notes = day.Bookings.Where(b => !string.IsNullOrWhiteSpace(b.Note)).Select(b => b.Note).ToList();
                if (notes.Count > 0)
                {
                    ws.Cell(row, 7).Value = string.Join(Environment.NewLine, notes);
                    ws.Cell(row, 7).Style.Alignment.WrapText = true;
                }

                if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                {
                    ws.Range(row, 1, row, 7).Style.Fill.BackgroundColor = XLColor.LightGray;
                }

                if (hasQuestionMark)
                {
                    ws.Range(row, 1, row, 7).Style.Fill.BackgroundColor = XLColor.FromHtml("#33F44336");
                }

                row++;
            }

            ws.Range(5, 1, row - 1, 7).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.Range(5, 1, row - 1, 7).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            ws.Column(1).Width = 22;
            ws.Column(2).Width = 15;
            ws.Column(3).Width = 15;
            ws.Column(4).Width = 14;
            ws.Column(5).Width = 18;
            ws.Column(6).Width = 14;
            ws.Column(7).Width = 30;

            ws.SheetView.FreezeRows(5);
            ws.Range(5, 1, row - 1, 7).SetAutoFilter();
        }

        private static (string Start, string End, bool HasMissingMark) GetStartEndStrings(
            IReadOnlyList<Booking> bookings, DateTime dayDate,
            bool isNightShiftKommen, bool isNightShiftGehen)
        {
            if (bookings == null || bookings.Count == 0) return ("", "", false);

            var ordered = bookings.OrderBy(b => b.Timestamp).ToList();
            var starts = new List<string>();
            var ends = new List<string>();

            DateTime? openKommen = null;
            string missingMark = dayDate.Date < DateTime.Today ? "?" : "";
            bool addedQuestionMark = false;

            foreach (var b in ordered)
            {
                if (b.Action == BookingAction.Kommen)
                {
                    if (openKommen != null)
                    {
                        starts.Add(ToGermanTime(openKommen.Value));
                        ends.Add(missingMark);
                        if (missingMark == "?") addedQuestionMark = true;
                    }
                    openKommen = b.Timestamp;
                }
                else
                {
                    if (openKommen == null)
                    {
                        starts.Add(isNightShiftGehen ? "" : missingMark);
                        if (!isNightShiftGehen && missingMark == "?") addedQuestionMark = true;
                        ends.Add(ToGermanTime(b.Timestamp));
                    }
                    else
                    {
                        starts.Add(ToGermanTime(openKommen.Value));
                        ends.Add(ToGermanTime(b.Timestamp));
                        openKommen = null;
                    }
                }
            }

            if (openKommen != null)
            {
                starts.Add(ToGermanTime(openKommen.Value));
                ends.Add(isNightShiftKommen ? "" : missingMark);
                if (!isNightShiftKommen && missingMark == "?") addedQuestionMark = true;
            }

            var finalStarts = starts.Where(s => !string.IsNullOrEmpty(s));
            var finalEnds = ends.Where(s => !string.IsNullOrEmpty(s));

            return (string.Join(Environment.NewLine, finalStarts), string.Join(Environment.NewLine, finalEnds), addedQuestionMark);
        }

        private static string GetGermanMonthName(int month)
        {
            return new[]
            {
                "Januar","Februar","März","April","Mai","Juni",
                "Juli","August","September","Oktober","November","Dezember"
            }[month - 1];
        }

        private static string GetGermanWeekday(DayOfWeek d) => d switch
        {
            DayOfWeek.Monday => "Montag",
            DayOfWeek.Tuesday => "Dienstag",
            DayOfWeek.Wednesday => "Mittwoch",
            DayOfWeek.Thursday => "Donnerstag",
            DayOfWeek.Friday => "Freitag",
            DayOfWeek.Saturday => "Samstag",
            DayOfWeek.Sunday => "Sonntag",
            _ => ""
        };

        private static string FormatUserLastFirst(string user)
        {
            if (string.IsNullOrWhiteSpace(user)) return user;
            var lastSpaceIndex = user.LastIndexOf(' ');
            if (lastSpaceIndex > 0 && lastSpaceIndex < user.Length - 1)
            {
                var firstName = user[..lastSpaceIndex];
                var lastName = user[(lastSpaceIndex + 1)..];
                return $"{lastName} {firstName}";
            }
            return user;
        }

        private static string SanitizeWorksheetName(string name)
        {
            foreach (var c in new[] { ':', '\\', '/', '?', '*', '[', ']' })
                name = name.Replace(c, '_');
            return name.Length > 31 ? name[..31] : name;
        }

        private static List<(string Name, string Rechte)> LoadUsersFromLoginDaten()
        {
            var list = new List<(string Name, string Rechte)>();
            using var con = new SqlConnection(SqlManager.connectionString);
            using var cmd = new SqlCommand("SELECT Benutzer, Rechte FROM dbo.LoginDaten", con);
            con.Open();
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                string name = r.GetString(0);
                string rechte = r.IsDBNull(1) ? "" : r.GetString(1);
                list.Add((name, rechte));
            }
            return list;
        }

        private static List<Booking> LoadBookingsFromDatabase(int year, int month, string benutzer)
        {
            var list = new List<Booking>();
            var start = new DateTime(year, month, 1);
            var end = start.AddMonths(1);

            const string sql = @"
                SELECT Id, Zeitstempel, Aktion, Manuel, Homeoffice, Bemerkung
                FROM dbo.Zeiterfassung
                WHERE Benutzername = @user
                AND Zeitstempel >= @start AND Zeitstempel < @end
                ORDER BY Zeitstempel";

            using var con = new SqlConnection(SqlManager.connectionString);
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.Add("@user", SqlDbType.NVarChar).Value = benutzer;
            cmd.Parameters.Add("@start", SqlDbType.DateTime2).Value = start;
            cmd.Parameters.Add("@end", SqlDbType.DateTime2).Value = end;

            con.Open();
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var id = r.GetInt32(0);
                string? note = r.IsDBNull(5) ? null : r.GetString(5);

                list.Add(new Booking(id,
                    r.GetDateTime(1),
                    ParseAction(r.GetString(2)),
                    !r.IsDBNull(3) && Convert.ToInt32(r[3]) != 0,
                    !r.IsDBNull(4) && Convert.ToInt32(r[4]) != 0,
                    note
                ));
            }
            return list;
        }

        private static BookingAction ParseAction(string raw) =>
            raw.ToUpper() == "GEHEN" ? BookingAction.Gehen : BookingAction.Kommen;

        private static string ToGermanTime(DateTime dt) => $"{dt:HH},{dt:mm}";
    }
}
