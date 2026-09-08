using HR_ERP.Models;

namespace HR_ERP.Data
{
    /// <summary>
    /// Keeps existing Attendance rows in sync automatically when something that affects
    /// attendance status changes elsewhere — an approved/rejected Leave, or a Holiday being
    /// added/edited/deleted. Without this, approving a leave request after the Attendance row
    /// already exists would leave that row showing stale Present/Absent/Late data.
    ///
    /// Approved rows are always skipped — once an HR manager has ticked "Approved" on an
    /// Attendance record, this service treats it as locked and will not silently overwrite it.
    /// The person can still edit an approved row manually in the Attendance screen; this only
    /// stops the *automatic* recalculation from touching it.
    /// </summary>
    public static class AttendanceRecalculationService
    {
        /// <summary>Recalculates one employee's existing Attendance rows across a date range
        /// (inclusive) — used after a Leave request is approved, rejected, or deleted.</summary>
        public static int RecalculateEmployeeRange(string employeeCode, DateTime start, DateTime end)
        {
            var employee = EmployeeRepository.GetByCode(employeeCode);
            if (employee == null) return 0;

            var shifts = ShiftRepository.GetAll();
            int updated = 0;

            for (var date = start.Date; date <= end.Date; date = date.AddDays(1))
            {
                var existing = AttendanceRepository.GetByEmployeeAndDate(employeeCode, date);
                if (existing == null || existing.Approved) continue; // nothing to update, or locked

                RecalculateOne(employee, existing, shifts);
                updated++;
            }

            return updated;
        }

        /// <summary>Recalculates every employee's existing Attendance row on one specific date —
        /// used after a company Holiday is added, edited, or deleted (affects everyone, not just
        /// one employee).</summary>
        public static int RecalculateAllEmployeesForDate(DateTime date)
        {
            var records = AttendanceRepository.GetByDate(date);
            if (records.Count == 0) return 0;

            var shifts = ShiftRepository.GetAll();
            var employeesByCode = EmployeeRepository.GetAll()
                .GroupBy(e => e.Code, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            int updated = 0;
            foreach (var record in records)
            {
                if (record.Approved) continue; // locked
                if (!employeesByCode.TryGetValue(record.EmployeeCode, out var employee)) continue;

                RecalculateOne(employee, record, shifts);
                updated++;
            }

            return updated;
        }

        private static void RecalculateOne(Employee employee, AttendanceRecord existing, List<Shift> shifts)
        {
            if (!existing.CheckinDate.HasValue) return;
            var date = existing.CheckinDate.Value;

            var shift = existing.ShiftId.HasValue ? shifts.FirstOrDefault(s => s.ShiftID == existing.ShiftId) : null;
            bool isHoliday = HolidayRepository.IsHoliday(date);
            bool isOnLeave = LeaveRequestRepository.IsOnApprovedLeave(employee.Code, date);
            bool isWeekend = AttendanceCalculator.IsWeekend(employee, date);

            var calc = AttendanceCalculator.Calculate(existing.Checkin, existing.Checkout, shift, isHoliday, isOnLeave, isWeekend);

            existing.Status1 = calc.Status;
            existing.MinutesLate = calc.MinutesLate;
            existing.MinutesEarlyLeave = calc.MinutesEarlyLeave;
            existing.WorkingHours = calc.WorkingHours;
            existing.Overtime = calc.Overtime;

            AttendanceRepository.Update(existing);
        }
    }
}
