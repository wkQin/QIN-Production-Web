using System;
using System.Collections.Generic;
using System.Globalization;

namespace QIN_Production_Web.Data.Zeiterfassung
{
    public class ZeiterfassungUserItem
    {
        public string Benutzer { get; init; } = "";
        public override string ToString() => Benutzer;
    }

    public class DayTimeRowVM
    {
        public DateTime Date { get; set; }
        public string DateText => Date.ToString("dd.MM.yyyy");
        public string WeekdayText => Date.ToString("dddd", CultureInfo.CurrentCulture);
        public string InTimeText { get; set; }
        public string OutTimeText { get; set; }

        public bool ShowMissingBadge => IsMissing && !IsHoliday && !IsAbsent;
        public string WorkedText { get; set; } = "";
        public string DailyOvertimeText { get; set; } = "";

        public bool IsMissing { get; set; }
        public bool IsWeekStart { get; set; }
        public string WeekHeaderText { get; set; } = "";
        public bool IsExpanded { get; set; }
        public bool HasMultipleBookings { get; set; }
        public string BookingHint { get; set; } = "";
        public bool HasBookings { get; set; }

        public bool IsHoliday { get; set; }
        public string HolidayText { get; set; } = "";
        public bool IsAbsent { get; set; }
        public string AbsenceText { get; set; } = "";
        public bool IsHomeOffice { get; set; }
        public string Note { get; set; } = "";

        public bool HasManual => ManualType != ManualBookingType.None;
        public ManualBookingType ManualType { get; set; } = ManualBookingType.None;

        public string ManualBadgeText => ManualType switch
        {
            ManualBookingType.Kommen => "Manuell · Kommen",
            ManualBookingType.Gehen => "Manuell · Gehen",
            ManualBookingType.KommenUndGehen => "Manuell · Kommen + Gehen",
            _ => ""
        };

        public List<BookingVM> Bookings { get; set; } = new();

        public DayTimeRowVM(DateTime date, string inTime, string outTime, bool isMissing, ManualBookingType manualType, string worked, string dailyOvertimeText = "")
        {
            Date = date;
            InTimeText = inTime;
            OutTimeText = outTime;
            IsMissing = isMissing;
            ManualType = manualType;
            WorkedText = worked;
            DailyOvertimeText = dailyOvertimeText;
        }
    }

    public class BookingVM
    {
        public int Id { get; set; }
        public string TimeText { get; set; }
        public string ActionText { get; set; }
        public string SourceText { get; set; }
        public string Note { get; set; }

        public bool IsManual { get; set; }
        public bool IsHomeOffice { get; set; }
        public bool CanDelete { get; set; }
        
        // Removed original command binding, this will be handled directly in Razor view.

        public BookingVM(int id, string timeText, string actionText, string sourceText, string note, bool isManual, bool isHomeOffice, bool canDelete)
        {
            Id = id;
            TimeText = timeText;
            ActionText = actionText;
            SourceText = sourceText;
            Note = note;
            IsManual = isManual;
            IsHomeOffice = isHomeOffice;
            CanDelete = canDelete;
        }
    }
}
