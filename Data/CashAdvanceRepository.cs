using System.Data;
using HR_ERP.Models;
using Microsoft.Data.SqlClient;

namespace HR_ERP.Data
{
    public static class CashAdvanceRepository
    {
        private const string BaseSelect = @"
            SELECT c.id, c.employee_code, c.amount, c.installments, c.monthly_deduction, c.start_month,
                   c.status, c.request_date, c.notes, e.name AS EmployeeName
            FROM CashAdvances c
            JOIN employee e ON c.employee_code = e.code";

        public static List<CashAdvance> GetAll()
        {
            var list = new List<CashAdvance>();
            var table = DatabaseHelper.ExecuteQuery(BaseSelect + " ORDER BY c.request_date DESC");
            foreach (DataRow row in table.Rows)
                list.Add(MapRow(row));
            return list;
        }

        public static List<CashAdvance> GetByEmployee(string employeeCode)
        {
            var list = new List<CashAdvance>();
            var table = DatabaseHelper.ExecuteQuery(
                BaseSelect + " WHERE c.employee_code = @code ORDER BY c.request_date DESC",
                new SqlParameter("@code", employeeCode));
            foreach (DataRow row in table.Rows)
                list.Add(MapRow(row));
            return list;
        }

        /// <summary>Active advances for this employee whose deduction window covers the given
        /// month (start_month &lt;= month, and not already fully paid off before this month).
        /// Used by PayrollGenerationService to apply the automatic monthly deduction.</summary>
        public static List<CashAdvance> GetActiveForEmployeeAndMonth(string employeeCode, string month)
        {
            return GetByEmployee(employeeCode)
                .Where(c => c.Status == "Active" && string.CompareOrdinal(c.StartMonth, month) <= 0 && c.RemainingAsOf(month) > 0)
                .ToList();
        }

        private static CashAdvance MapRow(DataRow row) => new()
        {
            Id = (int)row["id"],
            EmployeeCode = row["employee_code"].ToString() ?? "",
            Amount = (decimal)row["amount"],
            Installments = (int)row["installments"],
            MonthlyDeduction = (decimal)row["monthly_deduction"],
            StartMonth = row["start_month"].ToString() ?? "",
            Status = row["status"].ToString() ?? "Active",
            RequestDate = (DateTime)row["request_date"],
            Notes = row["notes"] as string,
            EmployeeName = row["EmployeeName"] as string
        };

        public static void Add(CashAdvance c)
        {
            DatabaseHelper.ExecuteNonQuery(@"
                INSERT INTO CashAdvances (employee_code, amount, installments, monthly_deduction, start_month, status, notes)
                VALUES (@code, @amount, @installments, @monthly, @start, @status, @notes)",
                new SqlParameter("@code", c.EmployeeCode),
                new SqlParameter("@amount", c.Amount),
                new SqlParameter("@installments", c.Installments),
                new SqlParameter("@monthly", c.MonthlyDeduction),
                new SqlParameter("@start", c.StartMonth),
                new SqlParameter("@status", c.Status),
                new SqlParameter("@notes", (object?)c.Notes ?? DBNull.Value));
        }

        public static void UpdateStatus(int id, string status)
        {
            DatabaseHelper.ExecuteNonQuery(
                "UPDATE CashAdvances SET status = @status WHERE id = @id",
                new SqlParameter("@status", status), new SqlParameter("@id", id));
        }

        public static void Delete(int id)
        {
            DatabaseHelper.ExecuteNonQuery("DELETE FROM CashAdvances WHERE id = @id", new SqlParameter("@id", id));
        }
    }
}
