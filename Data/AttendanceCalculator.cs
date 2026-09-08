using HR_ERP.Models;

namespace HR_ERP.Data
{
    public class AttendanceCalcResult
    {
        public int MinutesLate { get; set; }
        public int MinutesEarlyLeave { get; set; }
        public int? WorkingHours { get; set; }
        public decimal Overtime { get; set; }
        public string Status { get; set; } = "Present";
    }

    /// <summary>
    /// Derives status1 / MinutesLate / MinutesEarlyLeave / WorkingHours / Overtime from a
    /// raw check-in/check-out pair, the employee's assigned Shift, and whether the date is the
    /// employee's Weekend, a company Holiday, or covered by an approved Leave. This is "Layer 1"
    /// of the attendance pipeline — it runs whenever check-in/check-out/shift/employee/date
    /// change in the Attendance screen (or can be re-run with the "Recalculate" button), and
    /// every field it fills in is a normal editable text box, so an HR manager can override any
    /// value before saving without losing the automatic calculation for everyone else.
    ///
    /// Priority: approved Leave &gt; Holiday &gt; Weekend &gt; normal check-in/check-out comparison
    /// against the Shift. If the person still punched in/out on a day off, hours worked are
    /// recorded, but late/early/overtime rules are skipped since there's no shift expectation
    /// to compare against on a day off.
    /// </summary>
    public static class AttendanceCalculator
    {
        public static AttendanceCalcResult Calculate(TimeSpan? checkin, TimeSpan? checkout, Shift? shift,
            bool isHoliday = false, bool isOnApprovedLeave = false, bool isWeekend = false)
        {
            if (isOnApprovedLeave)
                return DayOffResult("Leave", checkin, checkout);

            if (isHoliday)
                return DayOffResult("Holiday", checkin, checkout);

            if (isWeekend)
                return DayOffResult("Weekend", checkin, checkout);

            var result = new AttendanceCalcResult();

            // No check-in at all -> Absent, nothing else to compute.
            if (checkin == null)
            {
                result.Status = "Absent";
                return result;
            }

            // No shift assigned -> we have no rule to compare against; just record hours worked.
            if (shift == null)
            {
                result.Status = "Present";
                if (checkout.HasValue)
                {
                    var elapsed = checkout.Value - checkin.Value;
                    if (elapsed < TimeSpan.Zero) elapsed += TimeSpan.FromDays(1); // crossed midnight
                    result.WorkingHours = (int)Math.Round(elapsed.TotalHours);
                }
                return result;
            }

            bool overnightShift = shift.EndTime <= shift.StartTime; // e.g. 21:00 -> 05:00

            // --- Late minutes ---
            double lateRaw = (checkin.Value - shift.StartTime).TotalMinutes;
            if (lateRaw < 0) lateRaw = 0; // arrived before shift start: not late
            result.MinutesLate = Math.Max(0, (int)lateRaw - shift.LateToleranceMinutes);

            // --- Early leave / overtime minutes, and working hours ---
            if (checkout.HasValue)
            {
                double checkoutFromStart = (checkout.Value - shift.StartTime).TotalMinutes;
                double endFromStart = (shift.EndTime - shift.StartTime).TotalMinutes;
                if (overnightShift)
                {
                    if (checkoutFromStart < 0) checkoutFromStart += 24 * 60;
                    if (endFromStart <= 0) endFromStart += 24 * 60;
                }

                double diffFromShiftEnd = endFromStart - checkoutFromStart; // positive = left early, negative = stayed late

                if (diffFromShiftEnd > 0)
                    result.MinutesEarlyLeave = Math.Max(0, (int)diffFromShiftEnd - shift.EarlyLeaveToleranceMinutes);
                else
                    result.Overtime = Math.Round(Math.Max(0, (int)(-diffFromShiftEnd) - shift.OvertimeGraceMinutes) / 60m, 2);

                var elapsed = checkout.Value - checkin.Value;
                if (elapsed < TimeSpan.Zero) elapsed += TimeSpan.FromDays(1);
                int workedHours = (int)Math.Round(elapsed.TotalHours);
                if (shift.WorkingHours.HasValue && workedHours > shift.WorkingHours.Value)
                    workedHours = shift.WorkingHours.Value; // cap regular hours; excess already captured as Overtime above
                result.WorkingHours = workedHours;
            }

            result.Status = result.MinutesLate > 0 ? "Late" : "Present";
            return result;
        }

        private static AttendanceCalcResult DayOffResult(string status, TimeSpan? checkin, TimeSpan? checkout)
        {
            var result = new AttendanceCalcResult { Status = status };
            if (checkin.HasValue && checkout.HasValue)
            {
                var elapsed = checkout.Value - checkin.Value;
                if (elapsed < TimeSpan.Zero) elapsed += TimeSpan.FromDays(1);
                result.WorkingHours = (int)Math.Round(elapsed.TotalHours);
            }
            return result;
        }

        /// <summary>True if the given date falls on this employee's Weekend1 or Weekend2
        /// (compared by day-of-week name, e.g. "Friday").</summary>
        public static bool IsWeekend(Employee employee, DateTime date)
        {
            var dayName = date.DayOfWeek.ToString();
            return string.Equals(employee.Weekend1, dayName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(employee.Weekend2, dayName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
