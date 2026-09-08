using HR_ERP.Helpers;

namespace HR_ERP.Models
{
    public class Department
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? EmplCode { get; set; }   // department head's employee code
        public string? Type { get; set; }
        public DateTime? Date { get; set; }
        public string? ParentCode { get; set; } // code of the main department this is a sub-department of (null = main department)
        public override string ToString() => Name;
    }

    public class JobPosition
    {
        public int Id { get; set; }
        public string? Code { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Depart { get; set; }
        public string? JobDescription { get; set; }
        public override string ToString() => Title;
    }

    public class Shift
    {
        public int ShiftID { get; set; }
        public string ShiftName { get; set; } = string.Empty;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int LateToleranceMinutes { get; set; }
        public int EarlyLeaveToleranceMinutes { get; set; }
        public int OvertimeGraceMinutes { get; set; }
        public int? WorkingHours { get; set; }
        public TimeSpan? DetectionStartTime { get; set; }
        public TimeSpan? DetectionEndTime { get; set; }
        public override string ToString() => ShiftName;
    }

    public class WorkShift
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int BreakMinutes { get; set; }
        public decimal? TotalHours { get; set; }
        public override string ToString() => Name;
    }

    public class Employee
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string? Depart { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Position { get; set; }
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? NationalId { get; set; }
        public DateTime? HireDate { get; set; }
        public string? EmploymentType { get; set; }
        public string EmploymentStatus { get; set; } = "Active";
        public DateTime? TerminationDate { get; set; }
        public string? TerminationReason { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? EmergencyContact { get; set; }
        public string? ERelation { get; set; }
        public string? EPhone1 { get; set; }
        public string? EPhone2 { get; set; }
        public string? EEmail { get; set; }
        public string? EAddress { get; set; }
        public DateTime CreateDate { get; set; }
        public string? InsuranceNo { get; set; }
        public string? MaritalStatus { get; set; }
        public bool LateEx { get; set; }
        public int? WorkHours { get; set; }
        public string? Weekend1 { get; set; }
        public string? Weekend2 { get; set; }
        public decimal BasicSalary { get; set; }
        public decimal Allowance { get; set; }
        public decimal OvertimeRate { get; set; }
        public decimal LateDeductionRate { get; set; }
        public decimal SalaryCompletion { get; set; }
        public decimal OtherEntitlements { get; set; }
        public bool IsInsured { get; set; }
        public decimal SocialInsuranceAmount { get; set; }
        public decimal MonthlyTax { get; set; }
        public int AnnualLeaveDays { get; set; } = 21;
        public int LeaveCarriedOver { get; set; }
        public string? ManagerCode { get; set; } // code of the employee this person reports to directly

        public string FullName => Name;
        public override string ToString() => $"{Code} - {Name}";
    }

    public class AttendanceRecord
    {
        public int Id { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string? Depart { get; set; }
        public string? Name { get; set; }
        public DateTime? CheckinDate { get; set; }
        public TimeSpan? Checkin { get; set; }
        public DateTime? CheckoutDate { get; set; }
        public TimeSpan? Checkout { get; set; }
        public int? ShiftId { get; set; }
        public string? ShiftName { get; set; }
        public int? WorkingHours { get; set; }
        public int MinutesLate { get; set; }
        public int MinutesEarlyLeave { get; set; }
        public decimal Overtime { get; set; }
        public string? Status1 { get; set; }
        public bool Approved { get; set; }
        public string? DayName { get; set; }
        public decimal TWHours { get; set; }
        public string? AbsentReason { get; set; }
    }

    public class AttendSummary
    {
        public int Id { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string? Depart { get; set; }
        public string? Name { get; set; }
        public string Month { get; set; } = string.Empty;
        public int DaysCount { get; set; }
        public int Present { get; set; }
        public int Late { get; set; }
        public decimal LateHours { get; set; }
        public int EarlyLeave { get; set; }
        public decimal EarlyLeaveHours { get; set; }
        public int Leaves { get; set; }
        public int Holidays { get; set; }
        public int Absent { get; set; }
        public int Overtime { get; set; }
        public decimal OvertimeHours { get; set; }
    }

    public class Holiday
    {
        public int HolidayID { get; set; }
        public DateTime HolidayDate { get; set; }
        public string? Description { get; set; }
    }

    public class LeaveRequest
    {
        public int LeaveID { get; set; }
        public string EmpCode { get; set; } = string.Empty;
        public string? EmplName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? LeaveType { get; set; }
        public string? Reason { get; set; }
        public string Status { get; set; } = "Pending";
    }

    /// <summary>Pairs a canonical (English, stored/matched) value with its localized display
    /// text, for ComboBoxes whose selection gets saved to the database — the fix for the bug
    /// where selecting a translated dropdown item saved the translated text itself instead of
    /// a stable value. Bind ItemsSource to a list of these with DisplayMemberPath="Display",
    /// and read the selection back via `(box.SelectedItem as ComboOption)?.Value`.</summary>
    public class ComboOption
    {
        public string Value { get; set; } = string.Empty;
        public string Display { get; set; } = string.Empty;
        public override string ToString() => Display;
    }

    public class EmployeeDocument
    {
        public int Id { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string DocumentType { get; set; } = "Other";
        public string FileName { get; set; } = string.Empty;
        public string StoredPath { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime UploadedDate { get; set; }

        /// <summary>The fixed set of document categories for the personal-profile document hub
        /// (matches the CK_EmployeeDocuments_type constraint in the database). Value is the
        /// canonical English text actually stored/checked by that constraint; Display is
        /// localized. Never bind a raw translated string array here — see ComboOption.</summary>
        public static readonly (string Value, string Key)[] CategoryDefinitions =
        {
            ("National ID Card", "EmployeeDocCategory.NationalId"),
            ("Birth Certificate", "EmployeeDocCategory.BirthCertificate"),
            ("Personal Photo", "EmployeeDocCategory.PersonalPhoto"),
            ("Graduation Certificate", "EmployeeDocCategory.GraduationCertificate"),
            ("Criminal Record (Fish)", "EmployeeDocCategory.CriminalRecord"),
            ("Military Status Certificate", "EmployeeDocCategory.MilitaryStatus"),
            ("Labor Office Card (Kaab Amal)", "EmployeeDocCategory.LaborOfficeCard"),
            ("Social Insurance Printout (Taameenat)", "EmployeeDocCategory.SocialInsurance"),
            ("Medical Fitness Certificate", "EmployeeDocCategory.MedicalFitness"),
            ("Contract", "EmployeeDocCategory.Contract"),
            ("Other", "EmployeeDocCategory.Other"),
        };

        public static List<ComboOption> CategoryOptions =>
            CategoryDefinitions.Select(d => new ComboOption { Value = d.Value, Display = Localization.T(d.Key) }).ToList();
    }

    /// <summary>One file in the general-purpose Documents library (Important/Free/Other) —
    /// unlike EmployeeDocument, this isn't tied to any single employee.</summary>
    public class DocumentLibraryItem
    {
        public int Id { get; set; }

        /// <summary>Canonical value — "Important", "Free", or "Other". Never translated: it's
        /// stored as literal text and matched against elsewhere (the category filter), so the
        /// UI shows a localized label for it while this field always stays in English.</summary>
        public string Category { get; set; } = "Other";

        public string DisplayName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string StoredPath { get; set; } = string.Empty;
        public long? FileSizeBytes { get; set; }
        public DateTime UploadedDate { get; set; }
        public string? UploadedBy { get; set; }

        public string Extension => System.IO.Path.GetExtension(DisplayName).ToLowerInvariant();
    }

    public class EmploymentHistoryEntry
    {
        public int Id { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string EventType { get; set; } = "Other";
        public string? OldDepartment { get; set; }
        public string? NewDepartment { get; set; }
        public string? OldPosition { get; set; }
        public string? NewPosition { get; set; }
        public decimal? OldSalary { get; set; }
        public decimal? NewSalary { get; set; }
        public string? PerformanceRating { get; set; }
        public string? Notes { get; set; }
        public DateTime RecordedDate { get; set; }

        /// <summary>The fixed set of event types (matches CK_EmploymentHistory_type). Value is
        /// the canonical English text actually stored/checked by that constraint; Display is
        /// localized. Never bind a raw translated string array here — see ComboOption.</summary>
        public static readonly (string Value, string Key)[] EventTypeDefinitions =
        {
            ("Hire", "HistoryEventType.Hire"),
            ("Promotion", "HistoryEventType.Promotion"),
            ("Department Transfer", "HistoryEventType.DepartmentTransfer"),
            ("Position Change", "HistoryEventType.PositionChange"),
            ("Salary Change", "HistoryEventType.SalaryChange"),
            ("Performance Review", "HistoryEventType.PerformanceReview"),
            ("Termination", "HistoryEventType.Termination"),
            ("Rehire", "HistoryEventType.Rehire"),
            ("Other", "HistoryEventType.Other"),
        };

        public static List<ComboOption> EventTypeOptions =>
            EventTypeDefinitions.Select(d => new ComboOption { Value = d.Value, Display = Localization.T(d.Key) }).ToList();

        /// <summary>Human-readable one-line summary of what changed, for display in the grid.</summary>
        public string Summary
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(OldDepartment) || !string.IsNullOrWhiteSpace(NewDepartment))
                    return $"{OldDepartment ?? "-"} → {NewDepartment ?? "-"}";
                if (!string.IsNullOrWhiteSpace(OldPosition) || !string.IsNullOrWhiteSpace(NewPosition))
                    return $"{OldPosition ?? "-"} → {NewPosition ?? "-"}";
                if (OldSalary.HasValue || NewSalary.HasValue)
                    return $"{OldSalary:N2} → {NewSalary:N2}";
                if (!string.IsNullOrWhiteSpace(PerformanceRating))
                    return $"Rating: {PerformanceRating}";
                return Notes ?? "";
            }
        }
    }

    /// <summary>One snapshot of an employee's salary breakdown, logged every time the Salary
    /// Details expander is saved (EmployeesView) — an append-only audit trail, separate from
    /// the general EmploymentHistoryEntry log, dedicated to the salary/insurance/tax figures.</summary>
    public class SalaryHistoryEntry
    {
        public int Id { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public DateTime RecordedDate { get; set; }
        public decimal BasicSalary { get; set; }
        public decimal FixedAllowance { get; set; }
        public decimal SalaryCompletion { get; set; }
        public decimal OtherEntitlements { get; set; }
        public bool IsInsured { get; set; }
        public decimal SocialInsuranceAmount { get; set; }
        public decimal MonthlyTax { get; set; }
        public string? ChangedBy { get; set; }

        /// <summary>Localized Yes/No for the grid — Insured/Not Insured is a fixed two-value
        /// status, so this reuses InsuranceStatus.* the same way the Insurance Status combo
        /// on the main Employee form does, never storing the translated word itself.</summary>
        public string InsuredDisplay => Localization.T(IsInsured ? "InsuranceStatus.Insured" : "InsuranceStatus.NotInsured");
    }

    public class LeaveBalance
    {
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public int Entitlement { get; set; }
        public int CarriedOver { get; set; }
        public int TotalAvailable => Entitlement + CarriedOver;
        public int UsedThisYear { get; set; }
        public int RemainingBalance => TotalAvailable - UsedThisYear;
    }

    public class PayrollAdjustment
    {
        public int Id { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string Type { get; set; } = "Bonus"; // Bonus / Penalty / Incentive
        public string Month { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? EmployeeName { get; set; }
    }

    public class CashAdvance
    {
        public int Id { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int Installments { get; set; } = 1;
        public decimal MonthlyDeduction { get; set; }
        public string StartMonth { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
        public DateTime RequestDate { get; set; }
        public string? Notes { get; set; }
        public string? EmployeeName { get; set; }

        /// <summary>How many monthly deductions have occurred by (and including) asOfMonth.</summary>
        public int InstallmentsElapsed(string asOfMonth)
        {
            int start = MonthIndex(StartMonth);
            int asOf = MonthIndex(asOfMonth);
            int elapsed = asOf - start + 1;
            return Math.Clamp(elapsed, 0, Installments);
        }

        public decimal PaidAsOf(string asOfMonth) => Math.Min(Amount, InstallmentsElapsed(asOfMonth) * MonthlyDeduction);
        public decimal RemainingAsOf(string asOfMonth) => Amount - PaidAsOf(asOfMonth);

        private static int MonthIndex(string yyyyMM)
        {
            var parts = yyyyMM.Split('-');
            return int.Parse(parts[0]) * 12 + int.Parse(parts[1]);
        }
    }

    public class Role
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public override string ToString() => Name;
    }

    public class Permission
    {
        public int Id { get; set; }
        public string ModuleKey { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }

    /// <summary>One row of the Roles x Permissions matrix shown on the Settings screen —
    /// whether the given role can view / add-edit-delete within the given module.</summary>
    public class RolePermission
    {
        public int RoleId { get; set; }
        public int PermissionId { get; set; }
        public string ModuleKey { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public bool CanView { get; set; }
        public bool CanEdit { get; set; }
    }

    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string PasswordSalt { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? EmployeeCode { get; set; }
        public int RoleId { get; set; }
        public string? RoleName { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; }
        public DateTime? LastLogin { get; set; }
        public override string ToString() => $"{Username} ({RoleName})";
    }

    public class PayrollRecord
    {
        public int PayrollID { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public DateTime PayPeriodStart { get; set; }
        public DateTime PayPeriodEnd { get; set; }
        public decimal BasicSalary { get; set; }
        public decimal Allowances { get; set; }
        public decimal Deductions { get; set; }
        public decimal OvertimeHours { get; set; }
        public decimal OvertimeAmount { get; set; }
        public decimal NetSalary { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string Status { get; set; } = "Pending";
        public string? EmployeeName { get; set; }
    }
}
