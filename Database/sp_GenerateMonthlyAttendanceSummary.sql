/* ============================================================
   sp_GenerateMonthlyAttendanceSummary
   Computes attend_summary for every active employee for a given
   calendar month, from Attendance + Leaves + Holidays + employee
   weekend settings, and upserts the results.

   Usage:
     EXEC dbo.sp_GenerateMonthlyAttendanceSummary @Month = '2026-08';
   ============================================================ */
USE HR_ERP;
GO

IF OBJECT_ID('dbo.sp_GenerateMonthlyAttendanceSummary', 'P') IS NULL
    EXEC('CREATE PROCEDURE dbo.sp_GenerateMonthlyAttendanceSummary AS BEGIN SET NOCOUNT ON; END');
GO

ALTER PROCEDURE dbo.sp_GenerateMonthlyAttendanceSummary
    @Month VARCHAR(7)   -- 'YYYY-MM'
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @PeriodStart DATE = CAST(@Month + '-01' AS DATE);
    DECLARE @PeriodEnd   DATE = EOMONTH(@PeriodStart);

    ;WITH Days AS (
        -- one row per calendar day in the month
        SELECT TOP (DATEDIFF(DAY, @PeriodStart, @PeriodEnd) + 1)
               DATEADD(DAY, ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1, @PeriodStart) AS d
        FROM sys.all_objects
    ),
    WorkingDays AS (
        -- days that are NOT the employee's weekend and NOT a company holiday
        SELECT e.code AS employee_code, dy.d
        FROM dbo.employee e
        CROSS JOIN Days dy
        WHERE e.employment_status = 'Active'
          AND DATENAME(WEEKDAY, dy.d) NOT IN (ISNULL(e.weekend1,''), ISNULL(e.weekend2,''))
          AND NOT EXISTS (SELECT 1 FROM dbo.Holidays h WHERE h.HolidayDate = dy.d)
    ),
    HolidayDays AS (
        -- company holidays that fall on what would otherwise be a working day
        SELECT e.code AS employee_code, h.HolidayDate AS d
        FROM dbo.employee e
        CROSS JOIN dbo.Holidays h
        WHERE e.employment_status = 'Active'
          AND h.HolidayDate BETWEEN @PeriodStart AND @PeriodEnd
          AND DATENAME(WEEKDAY, h.HolidayDate) NOT IN (ISNULL(e.weekend1,''), ISNULL(e.weekend2,''))
    ),
    LeaveDays AS (
        -- approved, PAID leave days only (Unpaid Leave is deliberately excluded here so it
        -- falls through into 'absent' below and gets deducted like an absence in payroll)
        SELECT l.Emp_code AS employee_code, dy.d
        FROM dbo.Leaves l
        JOIN dbo.employee e ON e.code = l.Emp_code
        CROSS JOIN Days dy
        WHERE l.Status = 'Approved'
          AND ISNULL(l.LeaveType, '') NOT IN ('Unpaid Leave', 'Unpaid')
          AND dy.d BETWEEN l.StartDate AND l.EndDate
          AND DATENAME(WEEKDAY, dy.d) NOT IN (ISNULL(e.weekend1,''), ISNULL(e.weekend2,''))
          AND NOT EXISTS (SELECT 1 FROM dbo.Holidays h WHERE h.HolidayDate = dy.d)
    ),
    AttendanceAgg AS (
        SELECT
            a.employee_code,
            SUM(CASE WHEN a.checkin IS NOT NULL THEN 1 ELSE 0 END) AS present,
            SUM(CASE WHEN a.MinutesLate > 0 THEN 1 ELSE 0 END)    AS late,
            SUM(ISNULL(a.MinutesLate,0)) / 60.0                   AS late_hours,
            SUM(CASE WHEN a.MinutesEarlyLeave > 0 THEN 1 ELSE 0 END) AS earlyL,
            SUM(ISNULL(a.MinutesEarlyLeave,0)) / 60.0             AS earlyL_hours,
            SUM(CASE WHEN a.Overtime > 0 THEN 1 ELSE 0 END)       AS overtime,
            SUM(ISNULL(a.Overtime,0))                             AS overtime_hours
        FROM dbo.Attendance a
        WHERE a.checkin_date BETWEEN @PeriodStart AND @PeriodEnd
        GROUP BY a.employee_code
    ),
    WorkingDaysAgg AS (
        SELECT employee_code, COUNT(*) AS days_count FROM WorkingDays GROUP BY employee_code
    ),
    HolidayAgg AS (
        SELECT employee_code, COUNT(*) AS holidays FROM HolidayDays GROUP BY employee_code
    ),
    LeaveAgg AS (
        SELECT employee_code, COUNT(*) AS leaves FROM LeaveDays GROUP BY employee_code
    ),
    Final AS (
        SELECT
            e.code AS employee_code,
            e.depart,
            e.name,
            @Month AS [month],
            ISNULL(wd.days_count, 0)                                                          AS days_count,
            ISNULL(aa.present, 0)                                                              AS present,
            ISNULL(aa.late, 0)                                                                 AS late,
            ISNULL(aa.late_hours, 0)                                                           AS late_hours,
            ISNULL(aa.earlyL, 0)                                                                AS earlyL,
            ISNULL(aa.earlyL_hours, 0)                                                          AS earlyL_hours,
            ISNULL(la.leaves, 0)                                                                AS leaves,
            ISNULL(ha.holidays, 0)                                                              AS holidays,
            -- absent = working days that aren't present, on leave, or a holiday
            CASE WHEN ISNULL(wd.days_count,0) - ISNULL(aa.present,0) - ISNULL(la.leaves,0) < 0
                 THEN 0
                 ELSE ISNULL(wd.days_count,0) - ISNULL(aa.present,0) - ISNULL(la.leaves,0)
            END                                                                                 AS absent,
            ISNULL(aa.overtime, 0)                                                              AS overtime,
            ISNULL(aa.overtime_hours, 0)                                                        AS overtime_hours
        FROM dbo.employee e
        LEFT JOIN AttendanceAgg  aa ON aa.employee_code = e.code
        LEFT JOIN WorkingDaysAgg wd ON wd.employee_code = e.code
        LEFT JOIN HolidayAgg     ha ON ha.employee_code = e.code
        LEFT JOIN LeaveAgg       la ON la.employee_code = e.code
        WHERE e.employment_status = 'Active'
    )
    MERGE dbo.attend_summary AS target
    USING Final AS src
        ON target.employee_code = src.employee_code AND target.[month] = src.[month]
    WHEN MATCHED THEN
        UPDATE SET depart = src.depart, name = src.name, days_count = src.days_count,
                   present = src.present, late = src.late, late_hours = src.late_hours,
                   earlyL = src.earlyL, earlyL_hours = src.earlyL_hours, leaves = src.leaves,
                   holidays = src.holidays, absent = src.absent, overtime = src.overtime,
                   overtime_hours = src.overtime_hours
    WHEN NOT MATCHED THEN
        INSERT (employee_code, depart, name, [month], days_count, present, late, late_hours,
                earlyL, earlyL_hours, leaves, holidays, absent, overtime, overtime_hours)
        VALUES (src.employee_code, src.depart, src.name, src.[month], src.days_count, src.present,
                src.late, src.late_hours, src.earlyL, src.earlyL_hours, src.leaves, src.holidays,
                src.absent, src.overtime, src.overtime_hours);
END
GO

/* Example:
EXEC dbo.sp_GenerateMonthlyAttendanceSummary @Month = '2026-08';
SELECT * FROM dbo.attend_summary WHERE [month] = '2026-08';
*/
