using System.Data;
using HR_ERP.Models;
using Microsoft.Data.SqlClient;

namespace HR_ERP.Data
{
    public static class PayrollRepository
    {
        private const string BaseSelect = @"
            SELECT p.PayrollID, p.employee_code, p.PayPeriodStart, p.PayPeriodEnd, p.BasicSalary,
                   p.Allowances, p.Deductions, p.OvertimeHours, p.OvertimeAmount, p.NetSalary,
                   p.PaymentDate, p.Status, e.name AS EmployeeName
            FROM Payroll p
            JOIN employee e ON p.employee_code = e.code";

        public static List<PayrollRecord> GetAll()
        {
            var list = new List<PayrollRecord>();
            var table = DatabaseHelper.ExecuteQuery(BaseSelect + " ORDER BY p.PayPeriodStart DESC");
            foreach (DataRow row in table.Rows)
                list.Add(MapRow(row));
            return list;
        }

        private static PayrollRecord MapRow(DataRow row) => new()
        {
            PayrollID = (int)row["PayrollID"],
            EmployeeCode = row["employee_code"].ToString() ?? "",
            PayPeriodStart = (DateTime)row["PayPeriodStart"],
            PayPeriodEnd = (DateTime)row["PayPeriodEnd"],
            BasicSalary = (decimal)row["BasicSalary"],
            Allowances = (decimal)row["Allowances"],
            Deductions = (decimal)row["Deductions"],
            OvertimeHours = (decimal)row["OvertimeHours"],
            OvertimeAmount = (decimal)row["OvertimeAmount"],
            NetSalary = (decimal)row["NetSalary"],
            PaymentDate = row["PaymentDate"] as DateTime?,
            Status = row["Status"].ToString() ?? "Pending",
            EmployeeName = row["EmployeeName"] as string
        };

        public static PayrollRecord? GetByEmployeeAndPeriod(string employeeCode, DateTime periodStart, DateTime periodEnd)
        {
            var table = DatabaseHelper.ExecuteQuery(
                BaseSelect + " WHERE p.employee_code = @code AND p.PayPeriodStart = @start AND p.PayPeriodEnd = @end",
                new SqlParameter("@code", employeeCode),
                new SqlParameter("@start", periodStart.Date),
                new SqlParameter("@end", periodEnd.Date));
            return table.Rows.Count > 0 ? MapRow(table.Rows[0]) : null;
        }

        /// <summary>Insert a new payroll row, or update the existing one for this employee/period
        /// (used by automatic monthly generation so re-running it doesn't create duplicates).</summary>
        public static void Upsert(PayrollRecord p)
        {
            var existing = GetByEmployeeAndPeriod(p.EmployeeCode, p.PayPeriodStart, p.PayPeriodEnd);
            if (existing == null)
            {
                Add(p);
            }
            else if (existing.Status != "Paid") // don't silently overwrite an already-paid record
            {
                DatabaseHelper.ExecuteNonQuery(@"
                    UPDATE Payroll SET BasicSalary=@basic, Allowances=@allow, Deductions=@deduct,
                        OvertimeHours=@otHours, OvertimeAmount=@otAmount
                    WHERE PayrollID=@id",
                    new SqlParameter("@basic", p.BasicSalary),
                    new SqlParameter("@allow", p.Allowances),
                    new SqlParameter("@deduct", p.Deductions),
                    new SqlParameter("@otHours", p.OvertimeHours),
                    new SqlParameter("@otAmount", p.OvertimeAmount),
                    new SqlParameter("@id", existing.PayrollID));
            }
        }

        public static void Add(PayrollRecord p)
        {
            DatabaseHelper.ExecuteNonQuery(@"
                INSERT INTO Payroll (employee_code, PayPeriodStart, PayPeriodEnd, BasicSalary, Allowances, Deductions, OvertimeHours, OvertimeAmount, PaymentDate, Status)
                VALUES (@code, @start, @end, @basic, @allow, @deduct, @otHours, @otAmount, @payDate, @status)",
                new SqlParameter("@code", p.EmployeeCode),
                new SqlParameter("@start", p.PayPeriodStart),
                new SqlParameter("@end", p.PayPeriodEnd),
                new SqlParameter("@basic", p.BasicSalary),
                new SqlParameter("@allow", p.Allowances),
                new SqlParameter("@deduct", p.Deductions),
                new SqlParameter("@otHours", p.OvertimeHours),
                new SqlParameter("@otAmount", p.OvertimeAmount),
                new SqlParameter("@payDate", (object?)p.PaymentDate ?? DBNull.Value),
                new SqlParameter("@status", p.Status));
        }

        public static void MarkPaid(int payrollId, DateTime paymentDate)
        {
            DatabaseHelper.ExecuteNonQuery(
                "UPDATE Payroll SET Status = 'Paid', PaymentDate = @date WHERE PayrollID = @id",
                new SqlParameter("@date", paymentDate),
                new SqlParameter("@id", payrollId));
        }

        public static void Delete(int id)
        {
            DatabaseHelper.ExecuteNonQuery("DELETE FROM Payroll WHERE PayrollID = @id", new SqlParameter("@id", id));
        }
    }
}
