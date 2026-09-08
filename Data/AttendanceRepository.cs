using System.Data;
using HR_ERP.Models;
using Microsoft.Data.SqlClient;

namespace HR_ERP.Data
{
    public static class AttendanceRepository
    {
        private const string BaseSelect = @"
            SELECT a.id, a.employee_code, a.depart, a.name, a.checkin_date, a.checkin, a.checkout_date, a.checkout,
                   a.shift_, s.ShiftName, a.WorkingHours, a.MinutesLate, a.MinutesEarlyLeave, a.Overtime, a.status1,
                   a.approved, a.daynam, a.TWHours, a.absent_reason
            FROM Attendance a
            LEFT JOIN Shifts s ON a.shift_ = s.ShiftID";

        public static List<AttendanceRecord> GetByDate(DateTime date)
        {
            var list = new List<AttendanceRecord>();
            var table = DatabaseHelper.ExecuteQuery(
                BaseSelect + " WHERE a.checkin_date = @date ORDER BY a.name",
                new SqlParameter("@date", date.Date));

            foreach (DataRow row in table.Rows)
                list.Add(MapRow(row));

            return list;
        }

        /// <summary>Every employee's attendance across a date range (inclusive) — used to build
        /// printed/exported attendance reports covering more than one person at once.</summary>
        public static List<AttendanceRecord> GetByDateRange(DateTime start, DateTime end)
        {
            var list = new List<AttendanceRecord>();
            var table = DatabaseHelper.ExecuteQuery(
                BaseSelect + " WHERE a.checkin_date >= @start AND a.checkin_date <= @end ORDER BY a.depart, a.name, a.checkin_date",
                new SqlParameter("@start", start.Date), new SqlParameter("@end", end.Date));

            foreach (DataRow row in table.Rows)
                list.Add(MapRow(row));

            return list;
        }

        public static List<AttendanceRecord> GetByEmployee(string employeeCode)
        {
            var list = new List<AttendanceRecord>();
            var table = DatabaseHelper.ExecuteQuery(
                BaseSelect + " WHERE a.employee_code = @code ORDER BY a.checkin_date DESC",
                new SqlParameter("@code", employeeCode));

            foreach (DataRow row in table.Rows)
                list.Add(MapRow(row));

            return list;
        }

        /// <summary>Attendance for one employee across a custom date range (inclusive) — used
        /// by the "one employee, whole month / custom range" preview mode.</summary>
        public static List<AttendanceRecord> GetByEmployeeAndDateRange(string employeeCode, DateTime start, DateTime end)
        {
            var list = new List<AttendanceRecord>();
            var table = DatabaseHelper.ExecuteQuery(
                BaseSelect + " WHERE a.employee_code = @code AND a.checkin_date BETWEEN @start AND @end ORDER BY a.checkin_date",
                new SqlParameter("@code", employeeCode),
                new SqlParameter("@start", start.Date),
                new SqlParameter("@end", end.Date));

            foreach (DataRow row in table.Rows)
                list.Add(MapRow(row));

            return list;
        }

        public static AttendanceRecord? GetByEmployeeAndDate(string employeeCode, DateTime date)
        {
            var table = DatabaseHelper.ExecuteQuery(
                BaseSelect + " WHERE a.employee_code = @code AND a.checkin_date = @date",
                new SqlParameter("@code", employeeCode),
                new SqlParameter("@date", date.Date));

            return table.Rows.Count > 0 ? MapRow(table.Rows[0]) : null;
        }

        private static AttendanceRecord MapRow(DataRow row) => new()
        {
            Id = (int)row["id"],
            EmployeeCode = row["employee_code"].ToString() ?? "",
            Depart = row["depart"] as string,
            Name = row["name"] as string,
            CheckinDate = row["checkin_date"] as DateTime?,
            Checkin = row["checkin"] as TimeSpan?,
            CheckoutDate = row["checkout_date"] as DateTime?,
            Checkout = row["checkout"] as TimeSpan?,
            ShiftId = row["shift_"] as int?,
            ShiftName = row["ShiftName"] as string,
            WorkingHours = row["WorkingHours"] as int?,
            MinutesLate = row["MinutesLate"] as int? ?? 0,
            MinutesEarlyLeave = row["MinutesEarlyLeave"] as int? ?? 0,
            Overtime = row["Overtime"] as decimal? ?? 0,
            Status1 = row["status1"] as string,
            Approved = row["approved"] as bool? ?? false,
            DayName = row["daynam"] as string,
            TWHours = row["TWHours"] as decimal? ?? 0,
            AbsentReason = row["absent_reason"] as string
        };

        public static void Add(AttendanceRecord a)
        {
            DatabaseHelper.ExecuteNonQuery(@"
                INSERT INTO Attendance (employee_code, depart, name, checkin_date, checkin, checkout_date, checkout,
                    shift_, WorkingHours, MinutesLate, MinutesEarlyLeave, Overtime, status1, approved, daynam, absent_reason)
                VALUES (@code, @depart, @name, @cinDate, @cin, @coutDate, @cout, @shift, @hours, @late, @early, @ot, @status, @approved, @dayname, @reason)",
                new SqlParameter("@code", a.EmployeeCode),
                new SqlParameter("@depart", (object?)a.Depart ?? DBNull.Value),
                new SqlParameter("@name", (object?)a.Name ?? DBNull.Value),
                new SqlParameter("@cinDate", (object?)a.CheckinDate ?? DBNull.Value),
                new SqlParameter("@cin", (object?)a.Checkin ?? DBNull.Value),
                new SqlParameter("@coutDate", (object?)a.CheckoutDate ?? DBNull.Value),
                new SqlParameter("@cout", (object?)a.Checkout ?? DBNull.Value),
                new SqlParameter("@shift", (object?)a.ShiftId ?? DBNull.Value),
                new SqlParameter("@hours", (object?)a.WorkingHours ?? DBNull.Value),
                new SqlParameter("@late", a.MinutesLate),
                new SqlParameter("@early", a.MinutesEarlyLeave),
                new SqlParameter("@ot", a.Overtime),
                new SqlParameter("@status", (object?)a.Status1 ?? DBNull.Value),
                new SqlParameter("@approved", a.Approved),
                new SqlParameter("@dayname", (object?)a.DayName ?? DBNull.Value),
                new SqlParameter("@reason", (object?)a.AbsentReason ?? DBNull.Value));
        }

        public static void Update(AttendanceRecord a)
        {
            DatabaseHelper.ExecuteNonQuery(@"
                UPDATE Attendance SET
                    depart = @depart, name = @name, checkin_date = @cinDate, checkin = @cin,
                    checkout_date = @coutDate, checkout = @cout, shift_ = @shift, WorkingHours = @hours,
                    MinutesLate = @late, MinutesEarlyLeave = @early, Overtime = @ot, status1 = @status,
                    approved = @approved, daynam = @dayname, absent_reason = @reason
                WHERE id = @id",
                new SqlParameter("@depart", (object?)a.Depart ?? DBNull.Value),
                new SqlParameter("@name", (object?)a.Name ?? DBNull.Value),
                new SqlParameter("@cinDate", (object?)a.CheckinDate ?? DBNull.Value),
                new SqlParameter("@cin", (object?)a.Checkin ?? DBNull.Value),
                new SqlParameter("@coutDate", (object?)a.CheckoutDate ?? DBNull.Value),
                new SqlParameter("@cout", (object?)a.Checkout ?? DBNull.Value),
                new SqlParameter("@shift", (object?)a.ShiftId ?? DBNull.Value),
                new SqlParameter("@hours", (object?)a.WorkingHours ?? DBNull.Value),
                new SqlParameter("@late", a.MinutesLate),
                new SqlParameter("@early", a.MinutesEarlyLeave),
                new SqlParameter("@ot", a.Overtime),
                new SqlParameter("@status", (object?)a.Status1 ?? DBNull.Value),
                new SqlParameter("@approved", a.Approved),
                new SqlParameter("@dayname", (object?)a.DayName ?? DBNull.Value),
                new SqlParameter("@reason", (object?)a.AbsentReason ?? DBNull.Value),
                new SqlParameter("@id", a.Id));
        }

        /// <summary>Insert or update the single record for this employee/date (used by Excel import).</summary>
        public static void Upsert(AttendanceRecord a)
        {
            var existing = a.CheckinDate.HasValue ? GetByEmployeeAndDate(a.EmployeeCode, a.CheckinDate.Value) : null;
            if (existing == null)
            {
                Add(a);
            }
            else
            {
                a.Id = existing.Id;
                Update(a);
            }
        }

        public static void Delete(int id)
        {
            DatabaseHelper.ExecuteNonQuery("DELETE FROM Attendance WHERE id = @id", new SqlParameter("@id", id));
        }
    }

    public static class AttendSummaryRepository
    {
        private const string BaseSelect = @"
            SELECT id, employee_code, depart, name, [month], days_count, present, late, late_hours,
                   earlyL, earlyL_hours, leaves, holidays, absent, overtime, overtime_hours
            FROM attend_summary";

        public static List<AttendSummary> GetByMonth(string month)
        {
            var list = new List<AttendSummary>();
            var table = DatabaseHelper.ExecuteQuery(BaseSelect + " WHERE [month] = @month ORDER BY name", new SqlParameter("@month", month));
            foreach (DataRow row in table.Rows)
                list.Add(MapRow(row));
            return list;
        }

        public static AttendSummary? GetByEmployeeAndMonth(string employeeCode, string month)
        {
            var table = DatabaseHelper.ExecuteQuery(
                BaseSelect + " WHERE employee_code = @code AND [month] = @month",
                new SqlParameter("@code", employeeCode), new SqlParameter("@month", month));
            return table.Rows.Count > 0 ? MapRow(table.Rows[0]) : null;
        }

        /// <summary>
        /// Runs dbo.sp_GenerateMonthlyAttendanceSummary for the given month, which recomputes
        /// attend_summary for every active employee straight from Attendance + Leaves + Holidays
        /// (see Database/sp_GenerateMonthlyAttendanceSummary.sql). Run this once per month before
        /// generating payroll so the numbers reflect the latest Attendance data automatically.
        /// </summary>
        public static void GenerateForMonth(string month)
        {
            DatabaseHelper.ExecuteNonQuery(
                "EXEC dbo.sp_GenerateMonthlyAttendanceSummary @Month = @month",
                new SqlParameter("@month", month));
        }

        public static List<AttendSummary> GetByEmployee(string employeeCode)
        {
            var list = new List<AttendSummary>();
            var table = DatabaseHelper.ExecuteQuery(BaseSelect + " WHERE employee_code = @code ORDER BY [month] DESC", new SqlParameter("@code", employeeCode));
            foreach (DataRow row in table.Rows)
                list.Add(MapRow(row));
            return list;
        }

        private static AttendSummary MapRow(DataRow row) => new()
        {
            Id = (int)row["id"],
            EmployeeCode = row["employee_code"].ToString() ?? "",
            Depart = row["depart"] as string,
            Name = row["name"] as string,
            Month = row["month"].ToString() ?? "",
            DaysCount = row["days_count"] as int? ?? 0,
            Present = row["present"] as int? ?? 0,
            Late = row["late"] as int? ?? 0,
            LateHours = row["late_hours"] as decimal? ?? 0,
            EarlyLeave = row["earlyL"] as int? ?? 0,
            EarlyLeaveHours = row["earlyL_hours"] as decimal? ?? 0,
            Leaves = row["leaves"] as int? ?? 0,
            Holidays = row["holidays"] as int? ?? 0,
            Absent = row["absent"] as int? ?? 0,
            Overtime = row["overtime"] as int? ?? 0,
            OvertimeHours = row["overtime_hours"] as decimal? ?? 0
        };

        /// <summary>Insert or update the one row per employee/month (unique constraint in the DB).</summary>
        public static void Upsert(AttendSummary s)
        {
            var existing = DatabaseHelper.ExecuteScalar(
                "SELECT id FROM attend_summary WHERE employee_code = @code AND [month] = @month",
                new SqlParameter("@code", s.EmployeeCode), new SqlParameter("@month", s.Month));

            if (existing == null)
            {
                DatabaseHelper.ExecuteNonQuery(@"
                    INSERT INTO attend_summary (employee_code, depart, name, [month], days_count, present, late, late_hours,
                        earlyL, earlyL_hours, leaves, holidays, absent, overtime, overtime_hours)
                    VALUES (@code, @depart, @name, @month, @days, @present, @late, @lateH, @early, @earlyH, @leaves, @holidays, @absent, @ot, @otH)",
                    Params(s));
            }
            else
            {
                DatabaseHelper.ExecuteNonQuery(@"
                    UPDATE attend_summary SET depart=@depart, name=@name, days_count=@days, present=@present, late=@late,
                        late_hours=@lateH, earlyL=@early, earlyL_hours=@earlyH, leaves=@leaves, holidays=@holidays,
                        absent=@absent, overtime=@ot, overtime_hours=@otH
                    WHERE employee_code=@code AND [month]=@month",
                    Params(s));
            }
        }

        private static SqlParameter[] Params(AttendSummary s) => new[]
        {
            new SqlParameter("@code", s.EmployeeCode),
            new SqlParameter("@depart", (object?)s.Depart ?? DBNull.Value),
            new SqlParameter("@name", (object?)s.Name ?? DBNull.Value),
            new SqlParameter("@month", s.Month),
            new SqlParameter("@days", s.DaysCount),
            new SqlParameter("@present", s.Present),
            new SqlParameter("@late", s.Late),
            new SqlParameter("@lateH", s.LateHours),
            new SqlParameter("@early", s.EarlyLeave),
            new SqlParameter("@earlyH", s.EarlyLeaveHours),
            new SqlParameter("@leaves", s.Leaves),
            new SqlParameter("@holidays", s.Holidays),
            new SqlParameter("@absent", s.Absent),
            new SqlParameter("@ot", s.Overtime),
            new SqlParameter("@otH", s.OvertimeHours)
        };

        public static void Delete(int id)
        {
            DatabaseHelper.ExecuteNonQuery("DELETE FROM attend_summary WHERE id = @id", new SqlParameter("@id", id));
        }
    }
}
