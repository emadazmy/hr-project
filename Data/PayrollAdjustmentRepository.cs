using System.Data;
using HR_ERP.Models;
using Microsoft.Data.SqlClient;

namespace HR_ERP.Data
{
    public static class PayrollAdjustmentRepository
    {
        private const string BaseSelect = @"
            SELECT a.id, a.employee_code, a.[type], a.[month], a.amount, a.description, a.created_date, e.name AS EmployeeName
            FROM PayrollAdjustments a
            JOIN employee e ON a.employee_code = e.code";

        public static List<PayrollAdjustment> GetByMonth(string month)
        {
            var list = new List<PayrollAdjustment>();
            var table = DatabaseHelper.ExecuteQuery(BaseSelect + " WHERE a.[month] = @month ORDER BY e.name", new SqlParameter("@month", month));
            foreach (DataRow row in table.Rows)
                list.Add(MapRow(row));
            return list;
        }

        public static List<PayrollAdjustment> GetByEmployeeAndMonth(string employeeCode, string month)
        {
            var list = new List<PayrollAdjustment>();
            var table = DatabaseHelper.ExecuteQuery(
                BaseSelect + " WHERE a.employee_code = @code AND a.[month] = @month",
                new SqlParameter("@code", employeeCode), new SqlParameter("@month", month));
            foreach (DataRow row in table.Rows)
                list.Add(MapRow(row));
            return list;
        }

        private static PayrollAdjustment MapRow(DataRow row) => new()
        {
            Id = (int)row["id"],
            EmployeeCode = row["employee_code"].ToString() ?? "",
            Type = row["type"].ToString() ?? "Bonus",
            Month = row["month"].ToString() ?? "",
            Amount = (decimal)row["amount"],
            Description = row["description"] as string,
            CreatedDate = (DateTime)row["created_date"],
            EmployeeName = row["EmployeeName"] as string
        };

        public static void Add(PayrollAdjustment a)
        {
            DatabaseHelper.ExecuteNonQuery(
                "INSERT INTO PayrollAdjustments (employee_code, [type], [month], amount, description) VALUES (@code, @type, @month, @amount, @desc)",
                new SqlParameter("@code", a.EmployeeCode),
                new SqlParameter("@type", a.Type),
                new SqlParameter("@month", a.Month),
                new SqlParameter("@amount", a.Amount),
                new SqlParameter("@desc", (object?)a.Description ?? DBNull.Value));
        }

        public static void Delete(int id)
        {
            DatabaseHelper.ExecuteNonQuery("DELETE FROM PayrollAdjustments WHERE id = @id", new SqlParameter("@id", id));
        }

        /// <summary>Sum of Bonus+Incentive amounts for this employee/month (adds to Allowances).</summary>
        public static decimal GetTotalPositive(string employeeCode, string month) =>
            GetByEmployeeAndMonth(employeeCode, month)
                .Where(a => a.Type == "Bonus" || a.Type == "Incentive")
                .Sum(a => a.Amount);

        /// <summary>Sum of Penalty amounts for this employee/month (adds to Deductions).</summary>
        public static decimal GetTotalPenalties(string employeeCode, string month) =>
            GetByEmployeeAndMonth(employeeCode, month)
                .Where(a => a.Type == "Penalty")
                .Sum(a => a.Amount);
    }
}
