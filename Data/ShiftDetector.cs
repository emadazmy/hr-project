using HR_ERP.Models;

namespace HR_ERP.Data
{
    /// <summary>
    /// Figures out which Shift an employee is on purely from their check-in time. This lets the
    /// Attendance screen and Excel import assign a Shift automatically instead of requiring it
    /// to be picked/typed for every punch.
    ///
    /// Two layers, in priority order:
    ///   1. An explicit detection window (Shift.DetectionStartTime/DetectionEndTime) — set this
    ///      on a shift when its check-in window needs to be defined precisely, e.g. two shifts
    ///      with start times close enough together that the nearest-start-time guess below could
    ///      pick the wrong one for a borderline punch.
    ///   2. Nearest scheduled start time — the default for every shift with no window configured
    ///      (which is every shift, unless someone deliberately sets one). Whichever shift's
    ///      StartTime is closest to the check-in wins, wrapping correctly across midnight. This
    ///      is what actually makes detection work out of the box: an employee checking in at
    ///      8:52 for a 9:00 shift, or at 7:40 (running early) for an 8:00 shift, both correctly
    ///      match their shift even though neither check-in falls literally inside
    ///      [StartTime, EndTime] — which is why the previous window-only version of this class
    ///      returned no match at all for any early or late arrival, leaving Shift unassigned and
    ///      silently skipping every late/early/overtime/working-hours rule that depends on it.
    /// </summary>
    public static class ShiftDetector
    {
        public static Shift? Detect(TimeSpan? checkin, IEnumerable<Shift> shifts)
        {
            if (checkin == null) return null;

            var list = shifts as IList<Shift> ?? shifts.ToList();
            if (list.Count == 0) return null;

            foreach (var shift in list)
            {
                if (shift.DetectionStartTime.HasValue && shift.DetectionEndTime.HasValue
                    && IsWithin(checkin.Value, shift.DetectionStartTime.Value, shift.DetectionEndTime.Value))
                    return shift;
            }

            return list.OrderBy(s => CircularMinutesDiff(checkin.Value, s.StartTime)).First();
        }

        private static bool IsWithin(TimeSpan t, TimeSpan start, TimeSpan end)
        {
            // Handles windows that cross midnight (e.g. detection 19:00 -> 07:00 for a night shift).
            return start <= end
                ? t >= start && t <= end
                : t >= start || t <= end;
        }

        /// <summary>Minutes between two times-of-day, taking the shorter way around the clock —
        /// so 23:50 and 00:10 are 20 minutes apart, not 23 hours 40 minutes.</summary>
        private static double CircularMinutesDiff(TimeSpan a, TimeSpan b)
        {
            double diff = Math.Abs((a - b).TotalMinutes);
            return Math.Min(diff, 24 * 60 - diff);
        }
    }
}
