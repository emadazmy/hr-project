using System.Data;
using HR_ERP.Models;
using Microsoft.Data.SqlClient;

namespace HR_ERP.Data
{
    public static class LeaveRequestRepository
    {
        private const string BaseSelect = "SELECT LeaveID, Emp_code, Empl_name, StartDate, EndDate, LeaveType, Reason, Status FROM Leaves";

        public static List<LeaveRequest> GetAll()
        {
            var list = new List<LeaveRequest>();
            var table = DatabaseHelper.ExecuteQuery(BaseSelect + " ORDER BY LeaveID DESC");
            foreach (DataRow row in table.Rows)
                list.Add(MapRow(row));
            return list;
        }

        private static LeaveRequest MapRow(DataRow row) => new()
        {
            LeaveID = (int)row["LeaveID"],
            EmpCode = row["Emp_code"].ToString() ?? "",
            EmplName = row["Empl_name"] as string,
            StartDate = (DateTime)row["StartDate"],
            EndDate = (DateTime)row["EndDate"],
            LeaveType = row["LeaveType"] as string,
            Reason = row["Reason"] as string,
            Status = row["Status"].ToString() ?? "Pending"
        };

        public static void Add(LeaveRequest lr)
        {
            DatabaseHelper.ExecuteNonQuery(@"
                INSERT INTO Leaves (Emp_code, Empl_name, StartDate, EndDate, LeaveType, Reason, Status)
                VALUES (@code, @name, @start, @end, @type, @reason, @status)",
                new SqlParameter("@code", lr.EmpCode),
                new SqlParameter("@name", (object?)lr.EmplName ?? DBNull.Value),
                new SqlParameter("@start", lr.StartDate),
                new SqlParameter("@end", lr.EndDate),
                new SqlParameter("@type", (object?)lr.LeaveType ?? DBNull.Value),
                new SqlParameter("@reason", (object?)lr.Reason ?? DBNull.Value),
                new SqlParameter("@status", lr.Status));
        }

        public static void UpdateStatus(int leaveId, string status)
        {
            DatabaseHelper.ExecuteNonQuery(
                "UPDATE Leaves SET Status = @status WHERE LeaveID = @id",
                new SqlParameter("@status", status),
                new SqlParameter("@id", leaveId));
        }

        public static void Delete(int id)
        {
            DatabaseHelper.ExecuteNonQuery("DELETE FROM Leaves WHERE LeaveID = @id", new SqlParameter("@id", id));
        }

        /// <summary>True if this employee has an approved leave request covering the given date.
        /// Used by AttendanceCalculator to set Status automatically.</summary>
        public static bool IsOnApprovedLeave(string employeeCode, DateTime date)
        {
            var result = DatabaseHelper.ExecuteScalar(
                "SELECT TOP 1 1 FROM Leaves WHERE Emp_code = @code AND Status = 'Approved' AND @date BETWEEN StartDate AND EndDate",
                new SqlParameter("@code", employeeCode),
                new SqlParameter("@date", date.Date));
            return result != null;
        }

        /// <summary>Total Annual Leave days approved for this employee within the given year
        /// (assumes requests don't span a year boundary — good enough for a balance summary).
        /// Used to build the leave balance report.</summary>
        public static int GetApprovedAnnualLeaveDaysUsed(string employeeCode, int year)
        {
            var table = DatabaseHelper.ExecuteQuery(
                "SELECT StartDate, EndDate FROM Leaves WHERE Emp_code = @code AND Status = 'Approved' AND LeaveType = 'Annual Leave' AND YEAR(StartDate) = @year",
                new SqlParameter("@code", employeeCode),
                new SqlParameter("@year", year));

            int total = 0;
            foreach (DataRow row in table.Rows)
            {
                var start = (DateTime)row["StartDate"];
                var end = (DateTime)row["EndDate"];
                total += (end - start).Days + 1;
            }
            return total;
        }

        /// <summary>Total Annual Leave days already tied up in this employee's own Pending
        /// requests for the given year — subtracted from the approved-based remaining balance
        /// so a person can't submit several pending requests that would collectively exceed
        /// their entitlement once approved (each is only checked against what's actually left
        /// after everything else they already have pending).</summary>
        public static int GetPendingAnnualLeaveDaysReserved(string employeeCode, int year)
        {
            var table = DatabaseHelper.ExecuteQuery(
                "SELECT StartDate, EndDate FROM Leaves WHERE Emp_code = @code AND Status = 'Pending' AND LeaveType = 'Annual Leave' AND YEAR(StartDate) = @year",
                new SqlParameter("@code", employeeCode),
                new SqlParameter("@year", year));

            int total = 0;
            foreach (DataRow row in table.Rows)
            {
                var start = (DateTime)row["StartDate"];
                var end = (DateTime)row["EndDate"];
                total += (end - start).Days + 1;
            }
            return total;
        }

        /// <summary>Annual Leave balance for a single employee, for the given year (defaults to
        /// the current year) — used by LeaveView to validate a new request against the
        /// remaining balance before it's ever inserted, and to show a live indicator as the
        /// person fills in the form.</summary>
        public static LeaveBalance GetBalanceForEmployee(Employee emp, int? year = null)
        {
            int y = year ?? DateTime.Today.Year;
            return new LeaveBalance
            {
                EmployeeCode = emp.Code,
                EmployeeName = emp.Name,
                Entitlement = emp.AnnualLeaveDays,
                CarriedOver = emp.LeaveCarriedOver,
                UsedThisYear = GetApprovedAnnualLeaveDaysUsed(emp.Code, y)
            };
        }

        /// <summary>Builds the Annual Leave balance summary (entitlement, carried-over,
        /// used this year, remaining) for every active employee, for the given year
        /// (defaults to the current year).</summary>
        public static List<LeaveBalance> GetLeaveBalances(int? year = null)
        {
            int y = year ?? DateTime.Today.Year;
            var employees = EmployeeRepository.GetAll().Where(e => e.EmploymentStatus == "Active");

            return employees.Select(e => new LeaveBalance
            {
                EmployeeCode = e.Code,
                EmployeeName = e.Name,
                Entitlement = e.AnnualLeaveDays,
                CarriedOver = e.LeaveCarriedOver,
                UsedThisYear = GetApprovedAnnualLeaveDaysUsed(e.Code, y)
            }).OrderBy(b => b.EmployeeName).ToList();
        }
    }

    public static class HolidayRepository
    {
        public static List<Holiday> GetAll()
        {
            var list = new List<Holiday>();
            var table = DatabaseHelper.ExecuteQuery("SELECT HolidayID, HolidayDate, Description FROM Holidays ORDER BY HolidayDate");
            foreach (DataRow row in table.Rows)
            {
                list.Add(new Holiday
                {
                    HolidayID = (int)row["HolidayID"],
                    HolidayDate = (DateTime)row["HolidayDate"],
                    Description = row["Description"] as string
                });
            }
            return list;
        }

        public static void Add(Holiday h)
        {
            DatabaseHelper.ExecuteNonQuery(
                "INSERT INTO Holidays (HolidayDate, Description) VALUES (@date, @desc)",
                new SqlParameter("@date", h.HolidayDate),
                new SqlParameter("@desc", (object?)h.Description ?? DBNull.Value));
        }

        public static void Update(Holiday h)
        {
            DatabaseHelper.ExecuteNonQuery(
                "UPDATE Holidays SET HolidayDate = @date, Description = @desc WHERE HolidayID = @id",
                new SqlParameter("@date", h.HolidayDate),
                new SqlParameter("@desc", (object?)h.Description ?? DBNull.Value),
                new SqlParameter("@id", h.HolidayID));
        }

        public static void Delete(int id)
        {
            DatabaseHelper.ExecuteNonQuery("DELETE FROM Holidays WHERE HolidayID = @id", new SqlParameter("@id", id));
        }

        /// <summary>True if the given date is a company holiday. Used by AttendanceCalculator
        /// to set Status automatically.</summary>
        public static bool IsHoliday(DateTime date)
        {
            var result = DatabaseHelper.ExecuteScalar(
                "SELECT TOP 1 1 FROM Holidays WHERE HolidayDate = @date",
                new SqlParameter("@date", date.Date));
            return result != null;
        }
    }
}
