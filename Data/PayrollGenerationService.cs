using HR_ERP.Models;

namespace HR_ERP.Data
{
    public class PayrollGenerationLine
    {
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? Message { get; set; }
    }

    public class PayrollGenerationResult
    {
        public int Generated { get; set; }
        public List<PayrollGenerationLine> Skipped { get; set; } = new();
    }

    /// <summary>
    /// Automatically calculates payroll for every active employee for a given month,
    /// using ONLY data already in the database — no manual entry of hours, overtime,
    /// or deductions:
    ///
    ///   1. Refreshes attend_summary for the month by re-running
    ///      dbo.sp_GenerateMonthlyAttendanceSummary, so present/absent/late/overtime
    ///      days are recomputed straight from the Attendance table.
    ///   2. For each employee, combines that summary with the pay rates stored on
    ///      the employee record (basic_salary, allowance, overtime_rate,
    ///      late_deduction_rate, salary_completion, other_entitlements,
    ///      social_insurance_amount, monthly_tax — the last two Salary Details
    ///      expander fields; social insurance only applies while is_insured = true),
    ///      plus this month's PayrollAdjustments (Bonus/Penalty/Incentive) and
    ///      CashAdvances installments due this month, to compute:
    ///         BasicSalary = employee.basic_salary
    ///         Allowances  = employee.allowance + employee.salary_completion + employee.other_entitlements
    ///                       + this month's Bonus/Incentive total
    ///         OvertimeAmount = summary.overtime_hours * employee.overtime_rate
    ///         Deductions  = (absent_days * daily_rate) + ((late_hours + earlyL_hours) * late_deduction_rate)
    ///                       + (employee.is_insured ? employee.social_insurance_amount : 0) + employee.monthly_tax
    ///                       + this month's Penalty total + this month's Cash Advance installment(s) due
    ///                       where daily_rate = basic_salary / days_count
    ///   3. Upserts one Payroll row per employee for the month (re-running for the
    ///      same month updates the existing row instead of duplicating it, unless
    ///      that row has already been marked "Paid" — paid records are left alone).
    /// </summary>
    public static class PayrollGenerationService
    {
        public static PayrollGenerationResult GenerateForMonth(string month)
        {
            // Step 1: make sure attend_summary reflects the latest Attendance data.
            AttendSummaryRepository.GenerateForMonth(month);

            var periodStart = DateTime.ParseExact(month + "-01", "yyyy-MM-dd", null);
            var periodEnd = new DateTime(periodStart.Year, periodStart.Month,
                DateTime.DaysInMonth(periodStart.Year, periodStart.Month));

            var result = new PayrollGenerationResult();
            var employees = EmployeeRepository.GetAll().Where(e => e.EmploymentStatus == "Active");

            foreach (var emp in employees)
            {
                var summary = AttendSummaryRepository.GetByEmployeeAndMonth(emp.Code, month);
                if (summary == null)
                {
                    result.Skipped.Add(new PayrollGenerationLine
                    {
                        EmployeeCode = emp.Code,
                        EmployeeName = emp.Name,
                        Success = false,
                        Message = "No attendance summary for this month (no Attendance rows found)."
                    });
                    continue;
                }

                decimal dailyRate = summary.DaysCount > 0 ? emp.BasicSalary / summary.DaysCount : 0;
                decimal absenceDeduction = summary.Absent * dailyRate;
                decimal latenessDeduction = (summary.LateHours + summary.EarlyLeaveHours) * emp.LateDeductionRate;
                decimal overtimeAmount = summary.OvertimeHours * emp.OvertimeRate;

                // Social insurance is a fixed monthly deduction, but only while the employee is
                // actually marked Insured (Insurance Status, Add/Edit Employee) — a value can sit
                // in social_insurance_amount from before someone was marked Not Insured without
                // affecting payroll.
                decimal insuranceDeduction = emp.IsInsured ? emp.SocialInsuranceAmount : 0;

                decimal bonusIncentiveTotal = PayrollAdjustmentRepository.GetTotalPositive(emp.Code, month);
                decimal penaltyTotal = PayrollAdjustmentRepository.GetTotalPenalties(emp.Code, month);

                // Each active advance's due amount this month is its monthly installment,
                // capped at whatever's left (covers a smaller final installment).
                decimal cashAdvanceDeduction = 0m;
                foreach (var advance in CashAdvanceRepository.GetActiveForEmployeeAndMonth(emp.Code, month))
                {
                    var remainingBeforeThisMonth = advance.Amount - Math.Min(advance.Amount, Math.Max(0, advance.InstallmentsElapsed(month) - 1) * advance.MonthlyDeduction);
                    var due = Math.Min(advance.MonthlyDeduction, remainingBeforeThisMonth);
                    cashAdvanceDeduction += Math.Max(0, due);

                    // Mark fully-paid advances as Completed once this month's deduction clears the balance.
                    if (advance.RemainingAsOf(month) <= 0)
                        CashAdvanceRepository.UpdateStatus(advance.Id, "Completed");
                }

                var payroll = new PayrollRecord
                {
                    EmployeeCode = emp.Code,
                    PayPeriodStart = periodStart,
                    PayPeriodEnd = periodEnd,
                    BasicSalary = emp.BasicSalary,
                    Allowances = emp.Allowance + emp.SalaryCompletion + emp.OtherEntitlements + bonusIncentiveTotal,
                    Deductions = Math.Round(absenceDeduction + latenessDeduction + insuranceDeduction + emp.MonthlyTax
                                             + penaltyTotal + cashAdvanceDeduction, 2),
                    OvertimeHours = summary.OvertimeHours,
                    OvertimeAmount = Math.Round(overtimeAmount, 2),
                    Status = "Pending"
                };

                PayrollRepository.Upsert(payroll);
                result.Generated++;
            }

            return result;
        }
    }
}
