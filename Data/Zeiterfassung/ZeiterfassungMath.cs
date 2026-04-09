using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace QIN_Production_Web.Data.Zeiterfassung
{
    public enum BookingAction
    {
        Kommen = 0,
        Gehen = 1
    }

    public sealed record Booking(int Id, DateTime Timestamp, BookingAction Action, bool IsManual, bool IsHomeOffice, string? Note = null);

    public sealed record WorkInterval(DateTime Start, DateTime End, bool StartManual, bool EndManual)
    {
        public int DurationMinutes => (int)Math.Max(0, (End - Start).TotalMinutes);
    }

    public enum ManualBookingType
    {
        None = 0,
        Kommen = 1,
        Gehen = 2,
        KommenUndGehen = 3
    }

    public enum RoundingMode
    {
        None = 0,
        RoundDown = 1,
        RoundUp = 2,
        Nearest = 3
    }

    public sealed class ZeiterfassungPolicy
    {
        public int StepMinutes { get; init; } = 15;
        public RoundingMode KommenRounding { get; init; } = RoundingMode.None;
        public RoundingMode GehenRounding { get; init; } = RoundingMode.None;
        public int GraceMinutes { get; init; } = 0;
        public bool EnableRounding { get; set; } = false;
        public DayOfWeek WeekStart { get; init; } = DayOfWeek.Monday;
        public bool EnableAutoBreak { get; init; } = true;
        public int Break15ThresholdMinutes { get; init; } = 4 * 60;
        public int Break15Minutes { get; init; } = 15;
        public int Break30ThresholdMinutes { get; init; } = 6 * 60;
        public int Break30Minutes { get; init; } = 30;
        public int Break45ThresholdMinutes { get; init; } = 9 * 60;
        public int Break45Minutes { get; init; } = 45;

        public void Validate()
        {
            if (StepMinutes <= 0) throw new ArgumentOutOfRangeException(nameof(StepMinutes));
            if (Break15ThresholdMinutes < 0) throw new ArgumentOutOfRangeException();
            if (Break15Minutes < 0) throw new ArgumentOutOfRangeException();
            if (StepMinutes > 60) throw new ArgumentOutOfRangeException(nameof(StepMinutes));
            if (Break30ThresholdMinutes < 0 || Break45ThresholdMinutes < 0) throw new ArgumentOutOfRangeException();
            if (Break30Minutes < 0 || Break45Minutes < 0) throw new ArgumentOutOfRangeException();
        }
    }

    public sealed record DayCalculationResult(
        DateOnly Date,
        IReadOnlyList<Booking> Bookings,
        IReadOnlyList<WorkInterval> Intervals,
        int WorkedMinutes,
        int DeductedBreakMinutes,
        bool IsMissing,
        ManualBookingType ManualType,
        bool IsWeekStart,
        int CalendarWeek
    );

    public sealed class ZeiterfassungMath
    {
        private readonly ZeiterfassungPolicy _policy;

        public ZeiterfassungMath(ZeiterfassungPolicy policy)
        {
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
            _policy.Validate();
        }

        public DayCalculationResult CalculateDay(DateOnly date, IEnumerable<Booking> allBookings)
        {
            if (allBookings == null) throw new ArgumentNullException(nameof(allBookings));

            var bookings = allBookings
                .Where(b => DateOnly.FromDateTime(b.Timestamp) == date)
                .OrderBy(b => b.Timestamp)
                .ToList();

            bool manualKommen = bookings.Any(b => b.IsManual && b.Action == BookingAction.Kommen);
            bool manualGehen = bookings.Any(b => b.IsManual && b.Action == BookingAction.Gehen);

            ManualBookingType manualType =
                manualKommen && manualGehen ? ManualBookingType.KommenUndGehen :
                manualKommen ? ManualBookingType.Kommen :
                manualGehen ? ManualBookingType.Gehen :
                ManualBookingType.None;

            var intervals = new List<WorkInterval>();
            Booking? openKommen = null;
            bool hasOrphanGehen = false;

            foreach (var b in bookings)
            {
                if (b.Action == BookingAction.Kommen)
                {
                    openKommen = b;
                    continue;
                }

                if (openKommen == null)
                {
                    hasOrphanGehen = true;
                    continue;
                }

                var start = ApplyRounding(openKommen.Timestamp, _policy.KommenRounding);
                var end = ApplyRounding(b.Timestamp, _policy.GehenRounding);

                if (end > start)
                    intervals.Add(new WorkInterval(start, end, openKommen.IsManual, b.IsManual));

                openKommen = null;
            }

            bool isMissing = openKommen != null || hasOrphanGehen;

            int workedMinutesRaw = intervals.Sum(i => i.DurationMinutes);
            int takenBreakMinutes = 0;
            if (intervals.Count >= 1)
            {
                int presenceMinutes = (int)(intervals.Last().End - intervals.First().Start).TotalMinutes;
                takenBreakMinutes = Math.Max(0, presenceMinutes - workedMinutesRaw);
            }

            int requiredBreak = 0;
            if (_policy.EnableAutoBreak)
            {
                int m = workedMinutesRaw;
                if (m >= _policy.Break45ThresholdMinutes)
                    requiredBreak = _policy.Break45Minutes;
                else if (m > _policy.Break30ThresholdMinutes)
                    requiredBreak = _policy.Break30Minutes;
                else if (m > _policy.Break15ThresholdMinutes)
                    requiredBreak = _policy.Break15Minutes;
            }

            int additionalBreakToDeduct = Math.Max(0, requiredBreak - takenBreakMinutes);
            int workedMinutesNet = Math.Max(0, workedMinutesRaw - additionalBreakToDeduct);

            bool isWeekStart = date.ToDateTime(TimeOnly.MinValue).DayOfWeek == _policy.WeekStart;
            int week = GetCalendarWeek(date, _policy.WeekStart);

            return new DayCalculationResult(
                date,
                bookings,
                intervals,
                workedMinutesNet,
                additionalBreakToDeduct,
                isMissing,
                manualType,
                isWeekStart,
                week
            );
        }

        public IReadOnlyList<DayCalculationResult> CalculateMonth(int year, int month, IEnumerable<Booking> allBookings)
        {
            if (month < 1 || month > 12) throw new ArgumentOutOfRangeException(nameof(month));
            if (allBookings == null) throw new ArgumentNullException(nameof(allBookings));

            int daysInMonth = DateTime.DaysInMonth(year, month);
            var results = new List<DayCalculationResult>(daysInMonth);

            for (int d = 1; d <= daysInMonth; d++)
            {
                var date = new DateOnly(year, month, d);
                results.Add(CalculateDay(date, allBookings));
            }

            if (results.Count > 0)
                results[0] = results[0] with { IsWeekStart = true };

            return results;
        }

        private DateTime ApplyRounding(DateTime t, RoundingMode mode)
        {
            if (!_policy.EnableRounding) return t;
            if (mode == RoundingMode.None) return t;

            if (_policy.GraceMinutes != 0)
                t = t.AddMinutes(_policy.GraceMinutes);

            int step = _policy.StepMinutes;
            int minutes = t.Hour * 60 + t.Minute;
            int rem = minutes % step;

            int rounded = mode switch
            {
                RoundingMode.RoundDown => minutes - rem,
                RoundingMode.RoundUp => rem == 0 ? minutes : minutes + (step - rem),
                RoundingMode.Nearest => rem < step / 2 ? minutes - rem : minutes + (step - rem),
                _ => minutes
            };

            return new DateTime(t.Year, t.Month, t.Day, 0, 0, 0, t.Kind).AddMinutes(rounded);
        }

        private static int GetCalendarWeek(DateOnly date, DayOfWeek weekStart)
        {
            var dt = date.ToDateTime(TimeOnly.MinValue);
            return CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(dt, CalendarWeekRule.FirstFourDayWeek, weekStart);
        }
    }
}
