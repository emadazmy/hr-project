using System.Data;
using HR_ERP.Models;
using Microsoft.Data.SqlClient;

namespace HR_ERP.Data
{
    public static class EmployeeRepository
    {
        private const string BaseSelect = @"
            SELECT id, code, depart, name, [position], gender, dateofbirth, nationalid, hire_date,
                   employment_type, employment_status, termination_date, termination_reason, phone,
                   email, address, emergency_contact, e_relation, e_phone1, e_phone2, e_email, e_address,
                   creat_date, insurance_no, marital_st, late_ex, workhours, weekend1, weekend2,
                   basic_salary, allowance, overtime_rate, late_deduction_rate,
                   salary_completion, other_entitlements, is_insured, social_insurance_amount, monthly_tax,
                   annual_leave_days, leave_carried_over, manager_code
            FROM employee";

        public static List<Employee> GetAll(string? searchText = null)
        {
            var list = new List<Employee>();
            string sql = BaseSelect;
            var parameters = new List<SqlParameter>();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                sql += " WHERE name LIKE @s OR code LIKE @s OR email LIKE @s";
                parameters.Add(new SqlParameter("@s", $"%{searchText}%"));
            }
            sql += " ORDER BY id DESC";

            var table = DatabaseHelper.ExecuteQuery(sql, parameters.ToArray());
            foreach (DataRow row in table.Rows)
                list.Add(MapRow(row));

            return list;
        }

        public static Employee? GetByCode(string code)
        {
            var table = DatabaseHelper.ExecuteQuery(BaseSelect + " WHERE code = @code", new SqlParameter("@code", code));
            return table.Rows.Count > 0 ? MapRow(table.Rows[0]) : null;
        }

        private static Employee MapRow(DataRow row) => new()
        {
            Id = (int)row["id"],
            Code = row["code"].ToString() ?? "",
            Depart = row["depart"] as string,
            Name = row["name"].ToString() ?? "",
            Position = row["position"] as string,
            Gender = row["gender"] as string,
            DateOfBirth = row["dateofbirth"] as DateTime?,
            NationalId = row["nationalid"] as string,
            HireDate = row["hire_date"] as DateTime?,
            EmploymentType = row["employment_type"] as string,
            EmploymentStatus = row["employment_status"].ToString() ?? "Active",
            TerminationDate = row["termination_date"] as DateTime?,
            TerminationReason = row["termination_reason"] as string,
            Phone = row["phone"] as string,
            Email = row["email"] as string,
            Address = row["address"] as string,
            EmergencyContact = row["emergency_contact"] as string,
            ERelation = row["e_relation"] as string,
            EPhone1 = row["e_phone1"] as string,
            EPhone2 = row["e_phone2"] as string,
            EEmail = row["e_email"] as string,
            EAddress = row["e_address"] as string,
            CreateDate = (DateTime)row["creat_date"],
            InsuranceNo = row["insurance_no"] as string,
            MaritalStatus = row["marital_st"] as string,
            LateEx = row["late_ex"] as bool? ?? false,
            WorkHours = row["workhours"] as int?,
            Weekend1 = row["weekend1"] as string,
            Weekend2 = row["weekend2"] as string,
            BasicSalary = row["basic_salary"] as decimal? ?? 0,
            Allowance = row["allowance"] as decimal? ?? 0,
            OvertimeRate = row["overtime_rate"] as decimal? ?? 0,
            LateDeductionRate = row["late_deduction_rate"] as decimal? ?? 0,
            SalaryCompletion = row["salary_completion"] as decimal? ?? 0,
            OtherEntitlements = row["other_entitlements"] as decimal? ?? 0,
            IsInsured = row["is_insured"] as bool? ?? false,
            SocialInsuranceAmount = row["social_insurance_amount"] as decimal? ?? 0,
            MonthlyTax = row["monthly_tax"] as decimal? ?? 0,
            AnnualLeaveDays = row["annual_leave_days"] as int? ?? 21,
            LeaveCarriedOver = row["leave_carried_over"] as int? ?? 0,
            ManagerCode = row["manager_code"] as string
        };

        public static void Add(Employee e)
        {
            DatabaseHelper.ExecuteNonQuery(@"
                INSERT INTO employee (code, depart, name, [position], gender, dateofbirth, nationalid, hire_date,
                    employment_type, employment_status, termination_date, termination_reason, phone, email, address,
                    emergency_contact, e_relation, e_phone1, e_phone2, e_email, e_address, insurance_no, marital_st,
                    late_ex, workhours, weekend1, weekend2, basic_salary, allowance, overtime_rate, late_deduction_rate,
                    salary_completion, other_entitlements, is_insured, social_insurance_amount, monthly_tax,
                    annual_leave_days, leave_carried_over, manager_code)
                VALUES (@code, @depart, @name, @position, @gender, @dob, @nid, @hire, @etype, @estatus, @tdate, @treason,
                    @phone, @email, @address, @econtact, @erelation, @ephone1, @ephone2, @eemail, @eaddress, @insurance,
                    @marital, @lateex, @workhours, @weekend1, @weekend2, @basicSalary, @allowance, @otRate, @lateDeductRate,
                    @salaryCompletion, @otherEntitlements, @isInsured, @socialInsurance, @monthlyTax,
                    @annualLeave, @carriedOver, @managerCode)",
                new SqlParameter("@code", e.Code),
                new SqlParameter("@depart", (object?)e.Depart ?? DBNull.Value),
                new SqlParameter("@name", e.Name),
                new SqlParameter("@position", (object?)e.Position ?? DBNull.Value),
                new SqlParameter("@gender", (object?)e.Gender ?? DBNull.Value),
                new SqlParameter("@dob", (object?)e.DateOfBirth ?? DBNull.Value),
                new SqlParameter("@nid", (object?)e.NationalId ?? DBNull.Value),
                new SqlParameter("@hire", (object?)e.HireDate ?? DBNull.Value),
                new SqlParameter("@etype", (object?)e.EmploymentType ?? DBNull.Value),
                new SqlParameter("@estatus", e.EmploymentStatus),
                new SqlParameter("@tdate", (object?)e.TerminationDate ?? DBNull.Value),
                new SqlParameter("@treason", (object?)e.TerminationReason ?? DBNull.Value),
                new SqlParameter("@phone", (object?)e.Phone ?? DBNull.Value),
                new SqlParameter("@email", (object?)e.Email ?? DBNull.Value),
                new SqlParameter("@address", (object?)e.Address ?? DBNull.Value),
                new SqlParameter("@econtact", (object?)e.EmergencyContact ?? DBNull.Value),
                new SqlParameter("@erelation", (object?)e.ERelation ?? DBNull.Value),
                new SqlParameter("@ephone1", (object?)e.EPhone1 ?? DBNull.Value),
                new SqlParameter("@ephone2", (object?)e.EPhone2 ?? DBNull.Value),
                new SqlParameter("@eemail", (object?)e.EEmail ?? DBNull.Value),
                new SqlParameter("@eaddress", (object?)e.EAddress ?? DBNull.Value),
                new SqlParameter("@insurance", (object?)e.InsuranceNo ?? DBNull.Value),
                new SqlParameter("@marital", (object?)e.MaritalStatus ?? DBNull.Value),
                new SqlParameter("@lateex", e.LateEx),
                new SqlParameter("@workhours", (object?)e.WorkHours ?? DBNull.Value),
                new SqlParameter("@weekend1", (object?)e.Weekend1 ?? DBNull.Value),
                new SqlParameter("@weekend2", (object?)e.Weekend2 ?? DBNull.Value),
                new SqlParameter("@basicSalary", e.BasicSalary),
                new SqlParameter("@allowance", e.Allowance),
                new SqlParameter("@otRate", e.OvertimeRate),
                new SqlParameter("@lateDeductRate", e.LateDeductionRate),
                new SqlParameter("@salaryCompletion", e.SalaryCompletion),
                new SqlParameter("@otherEntitlements", e.OtherEntitlements),
                new SqlParameter("@isInsured", e.IsInsured),
                new SqlParameter("@socialInsurance", e.SocialInsuranceAmount),
                new SqlParameter("@monthlyTax", e.MonthlyTax),
                new SqlParameter("@annualLeave", e.AnnualLeaveDays),
                new SqlParameter("@carriedOver", e.LeaveCarriedOver),
                new SqlParameter("@managerCode", (object?)e.ManagerCode ?? DBNull.Value));
        }

        public static void Update(Employee e)
        {
            DatabaseHelper.ExecuteNonQuery(@"
                UPDATE employee SET
                    code = @code, depart = @depart, name = @name, [position] = @position, gender = @gender,
                    dateofbirth = @dob, nationalid = @nid, hire_date = @hire, employment_type = @etype,
                    employment_status = @estatus, termination_date = @tdate, termination_reason = @treason,
                    phone = @phone, email = @email, address = @address, emergency_contact = @econtact,
                    e_relation = @erelation, e_phone1 = @ephone1, e_phone2 = @ephone2, e_email = @eemail,
                    e_address = @eaddress, insurance_no = @insurance, marital_st = @marital, late_ex = @lateex,
                    workhours = @workhours, weekend1 = @weekend1, weekend2 = @weekend2,
                    basic_salary = @basicSalary, allowance = @allowance, overtime_rate = @otRate,
                    late_deduction_rate = @lateDeductRate, salary_completion = @salaryCompletion,
                    other_entitlements = @otherEntitlements, is_insured = @isInsured,
                    social_insurance_amount = @socialInsurance, monthly_tax = @monthlyTax,
                    annual_leave_days = @annualLeave,
                    leave_carried_over = @carriedOver, manager_code = @managerCode
                WHERE id = @id",
                new SqlParameter("@code", e.Code),
                new SqlParameter("@depart", (object?)e.Depart ?? DBNull.Value),
                new SqlParameter("@name", e.Name),
                new SqlParameter("@position", (object?)e.Position ?? DBNull.Value),
                new SqlParameter("@gender", (object?)e.Gender ?? DBNull.Value),
                new SqlParameter("@dob", (object?)e.DateOfBirth ?? DBNull.Value),
                new SqlParameter("@nid", (object?)e.NationalId ?? DBNull.Value),
                new SqlParameter("@hire", (object?)e.HireDate ?? DBNull.Value),
                new SqlParameter("@etype", (object?)e.EmploymentType ?? DBNull.Value),
                new SqlParameter("@estatus", e.EmploymentStatus),
                new SqlParameter("@tdate", (object?)e.TerminationDate ?? DBNull.Value),
                new SqlParameter("@treason", (object?)e.TerminationReason ?? DBNull.Value),
                new SqlParameter("@phone", (object?)e.Phone ?? DBNull.Value),
                new SqlParameter("@email", (object?)e.Email ?? DBNull.Value),
                new SqlParameter("@address", (object?)e.Address ?? DBNull.Value),
                new SqlParameter("@econtact", (object?)e.EmergencyContact ?? DBNull.Value),
                new SqlParameter("@erelation", (object?)e.ERelation ?? DBNull.Value),
                new SqlParameter("@ephone1", (object?)e.EPhone1 ?? DBNull.Value),
                new SqlParameter("@ephone2", (object?)e.EPhone2 ?? DBNull.Value),
                new SqlParameter("@eemail", (object?)e.EEmail ?? DBNull.Value),
                new SqlParameter("@eaddress", (object?)e.EAddress ?? DBNull.Value),
                new SqlParameter("@insurance", (object?)e.InsuranceNo ?? DBNull.Value),
                new SqlParameter("@marital", (object?)e.MaritalStatus ?? DBNull.Value),
                new SqlParameter("@lateex", e.LateEx),
                new SqlParameter("@workhours", (object?)e.WorkHours ?? DBNull.Value),
                new SqlParameter("@weekend1", (object?)e.Weekend1 ?? DBNull.Value),
                new SqlParameter("@weekend2", (object?)e.Weekend2 ?? DBNull.Value),
                new SqlParameter("@basicSalary", e.BasicSalary),
                new SqlParameter("@allowance", e.Allowance),
                new SqlParameter("@otRate", e.OvertimeRate),
                new SqlParameter("@lateDeductRate", e.LateDeductionRate),
                new SqlParameter("@salaryCompletion", e.SalaryCompletion),
                new SqlParameter("@otherEntitlements", e.OtherEntitlements),
                new SqlParameter("@isInsured", e.IsInsured),
                new SqlParameter("@socialInsurance", e.SocialInsuranceAmount),
                new SqlParameter("@monthlyTax", e.MonthlyTax),
                new SqlParameter("@annualLeave", e.AnnualLeaveDays),
                new SqlParameter("@carriedOver", e.LeaveCarriedOver),
                new SqlParameter("@managerCode", (object?)e.ManagerCode ?? DBNull.Value),
                new SqlParameter("@id", e.Id));
        }

        /// <summary>Targeted update used by the Salary Details expander (EmployeesView) — only
        /// touches the salary/entitlement/deduction columns, matched by employee code rather
        /// than id. Deliberately excludes is_insured, which belongs to the main Add/Edit
        /// Employee form's Insurance Status combo instead, so the two independent forms never
        /// overwrite each other's fields.</summary>
        public static void UpdateSalaryFields(Employee e)
        {
            DatabaseHelper.ExecuteNonQuery(@"
                UPDATE employee SET
                    basic_salary = @basicSalary, allowance = @allowance, overtime_rate = @otRate,
                    late_deduction_rate = @lateDeductRate, salary_completion = @salaryCompletion,
                    other_entitlements = @otherEntitlements, social_insurance_amount = @socialInsurance,
                    monthly_tax = @monthlyTax
                WHERE code = @code",
                new SqlParameter("@basicSalary", e.BasicSalary),
                new SqlParameter("@allowance", e.Allowance),
                new SqlParameter("@otRate", e.OvertimeRate),
                new SqlParameter("@lateDeductRate", e.LateDeductionRate),
                new SqlParameter("@salaryCompletion", e.SalaryCompletion),
                new SqlParameter("@otherEntitlements", e.OtherEntitlements),
                new SqlParameter("@socialInsurance", e.SocialInsuranceAmount),
                new SqlParameter("@monthlyTax", e.MonthlyTax),
                new SqlParameter("@code", e.Code));
        }

        public static void Delete(int id)
        {
            DatabaseHelper.ExecuteNonQuery("DELETE FROM employee WHERE id = @id", new SqlParameter("@id", id));
        }
    }
}
