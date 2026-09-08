using System.Data;
using HR_ERP.Models;
using Microsoft.Data.SqlClient;

namespace HR_ERP.Data
{
    /// <summary>Append-only log of salary snapshots — one row per Save from the Salary Details
    /// expander (EmployeesView), giving HR a dated history of every change to an employee's
    /// pay breakdown. Never updated or deleted from the UI; only ever added to.</summary>
    public static class SalaryHistoryRepository
    {
        private const string BaseSelect = @"
            SELECT id, employee_code, recorded_date, basic_salary, fixed_allowance, salary_completion,
                   other_entitlements, is_insured, social_insurance_amount, monthly_tax, changed_by
            FROM SalaryHistory";

        public static List<SalaryHistoryEntry> GetByEmployee(string employeeCode)
        {
            var list = new List<SalaryHistoryEntry>();
            var table = DatabaseHelper.ExecuteQuery(
                BaseSelect + " WHERE employee_code = @code ORDER BY recorded_date DESC",
                new SqlParameter("@code", employeeCode));
            foreach (DataRow row in table.Rows)
                list.Add(MapRow(row));
            return list;
        }

        public static void Add(SalaryHistoryEntry entry)
        {
            DatabaseHelper.ExecuteNonQuery(
                "INSERT INTO SalaryHistory (employee_code, basic_salary, fixed_allowance, salary_completion, " +
                "other_entitlements, is_insured, social_insurance_amount, monthly_tax, changed_by) " +
                "VALUES (@code, @basic, @allowance, @completion, @other, @insured, @insurance, @tax, @by)",
                new SqlParameter("@code", entry.EmployeeCode),
                new SqlParameter("@basic", entry.BasicSalary),
                new SqlParameter("@allowance", entry.FixedAllowance),
                new SqlParameter("@completion", entry.SalaryCompletion),
                new SqlParameter("@other", entry.OtherEntitlements),
                new SqlParameter("@insured", entry.IsInsured),
                new SqlParameter("@insurance", entry.SocialInsuranceAmount),
                new SqlParameter("@tax", entry.MonthlyTax),
                new SqlParameter("@by", (object?)entry.ChangedBy ?? DBNull.Value));
        }

        private static SalaryHistoryEntry MapRow(DataRow row) => new()
        {
            Id = (int)row["id"],
            EmployeeCode = row["employee_code"].ToString() ?? "",
            RecordedDate = (DateTime)row["recorded_date"],
            BasicSalary = row["basic_salary"] as decimal? ?? 0,
            FixedAllowance = row["fixed_allowance"] as decimal? ?? 0,
            SalaryCompletion = row["salary_completion"] as decimal? ?? 0,
            OtherEntitlements = row["other_entitlements"] as decimal? ?? 0,
            IsInsured = row["is_insured"] as bool? ?? false,
            SocialInsuranceAmount = row["social_insurance_amount"] as decimal? ?? 0,
            MonthlyTax = row["monthly_tax"] as decimal? ?? 0,
            ChangedBy = row["changed_by"] as string
        };
    }
}
