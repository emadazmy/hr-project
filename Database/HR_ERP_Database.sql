/* ============================================================
   HR ERP Module - SQL Server Database Script (v2)
   Rewritten to match the biometric/attendance-device style schema:
   department, employee, JobPositions, Shifts, WorkShifts, Attendance,
   attend_summary, Holidays, Leaves, plus the existing Payroll module
   (kept, and re-wired to employee.code to match the new convention).

   Improvements over the raw snippet this was based on:
   - Every table has a real primary key.
   - Date-like columns that were TEXT (varchar) are now DATE/DATETIME.
   - Foreign keys tie Attendance/attend_summary/Leaves/Payroll back to
     employee.code, employee.depart back to department.name, and
     Attendance.shift_ to Shifts.
   - Sensible defaults (approved = 0, Leaves.Status = 'Pending', etc.)
   - Indexes on the columns you'll filter/join on most (employee_code, dates).

   Run this entire script in SSMS or the SQL Server Object Explorer.
   ============================================================ */

IF DB_ID('HR_ERP') IS NULL
BEGIN
    CREATE DATABASE HR_ERP;
END
GO

USE HR_ERP;
GO

/* Drop in dependency order if re-running */
IF OBJECT_ID('dbo.SalaryHistory', 'U') IS NOT NULL DROP TABLE dbo.SalaryHistory;
IF OBJECT_ID('dbo.DocumentLibrary', 'U') IS NOT NULL DROP TABLE dbo.DocumentLibrary;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
IF OBJECT_ID('dbo.RolePermissions', 'U') IS NOT NULL DROP TABLE dbo.RolePermissions;
IF OBJECT_ID('dbo.Permissions', 'U') IS NOT NULL DROP TABLE dbo.Permissions;
IF OBJECT_ID('dbo.Roles', 'U') IS NOT NULL DROP TABLE dbo.Roles;
IF OBJECT_ID('dbo.AppSettings', 'U') IS NOT NULL DROP TABLE dbo.AppSettings;
IF OBJECT_ID('dbo.EmploymentHistory', 'U') IS NOT NULL DROP TABLE dbo.EmploymentHistory;
IF OBJECT_ID('dbo.CashAdvances', 'U') IS NOT NULL DROP TABLE dbo.CashAdvances;
IF OBJECT_ID('dbo.PayrollAdjustments', 'U') IS NOT NULL DROP TABLE dbo.PayrollAdjustments;
IF OBJECT_ID('dbo.EmployeeDocuments', 'U') IS NOT NULL DROP TABLE dbo.EmployeeDocuments;
IF OBJECT_ID('dbo.Payroll', 'U') IS NOT NULL DROP TABLE dbo.Payroll;
IF OBJECT_ID('dbo.attend_summary', 'U') IS NOT NULL DROP TABLE dbo.attend_summary;
IF OBJECT_ID('dbo.Attendance', 'U') IS NOT NULL DROP TABLE dbo.Attendance;
IF OBJECT_ID('dbo.Leaves', 'U') IS NOT NULL DROP TABLE dbo.Leaves;
IF OBJECT_ID('dbo.employee', 'U') IS NOT NULL DROP TABLE dbo.employee;
IF OBJECT_ID('dbo.JobPositions', 'U') IS NOT NULL DROP TABLE dbo.JobPositions;
IF OBJECT_ID('dbo.department', 'U') IS NOT NULL DROP TABLE dbo.department;
IF OBJECT_ID('dbo.Shifts', 'U') IS NOT NULL DROP TABLE dbo.Shifts;
IF OBJECT_ID('dbo.WorkShifts', 'U') IS NOT NULL DROP TABLE dbo.WorkShifts;
IF OBJECT_ID('dbo.Holidays', 'U') IS NOT NULL DROP TABLE dbo.Holidays;
GO

/* ----------------------------------------------------------
   1. department0
   ---------------------------------------------------------- */
CREATE TABLE dbo.department (
    id          INT IDENTITY(1,1) PRIMARY KEY,
    code        VARCHAR(10)     NOT NULL,
    name        NVARCHAR(100)    NOT NULL,
    empl_code   VARCHAR(10)     NULL,       -- department head / manager employee code
    type        VARCHAR(20)     NULL,
    [date]      DATE            NULL,       -- date department was established
    parent_code VARCHAR(10)     NULL,       -- code of the main department this is a sub-department of (NULL = main department)
    CONSTRAINT UQ_department_code UNIQUE (code),
    CONSTRAINT UQ_department_name UNIQUE (name),
    CONSTRAINT FK_department_parent FOREIGN KEY (parent_code)
        REFERENCES dbo.department(code)
);
GO

/* ----------------------------------------------------------
   2. JobPositions
   ---------------------------------------------------------- */
CREATE TABLE dbo.JobPositions (
    ID              INT IDENTITY(1,1) PRIMARY KEY,
    Code            VARCHAR(20)     NULL,
    Title           NVARCHAR(50)    NOT NULL,
    Depart          NVARCHAR(100)    NULL,
    JobDescription  NVARCHAR(MAX)   NULL,
    CONSTRAINT FK_JobPositions_department FOREIGN KEY (Depart)
        REFERENCES dbo.department(name)
);
GO

/* ----------------------------------------------------------
   3. Shifts (rule-based shift definitions incl. tolerances)
   ---------------------------------------------------------- */
CREATE TABLE dbo.Shifts (
    ShiftID                     INT IDENTITY(1,1) PRIMARY KEY,
    ShiftName                   NVARCHAR(50)    NOT NULL,
    StartTime                   TIME(7)         NOT NULL,
    EndTime                     TIME(7)         NOT NULL,
    LateToleranceMinutes        INT             NOT NULL DEFAULT 0,
    EarlyLeaveToleranceMinutes  INT             NOT NULL DEFAULT 0,
    OvertimeGraceMinutes        INT             NOT NULL DEFAULT 0,
    workingHours                INT             NULL,
    DetectionStartTime          TIME(7)         NULL,
    DetectionEndTime            TIME(7)         NULL
);
GO

/* ----------------------------------------------------------
   4. WorkShifts (simple fixed shift templates)
   ---------------------------------------------------------- */
CREATE TABLE dbo.WorkShifts (
    id              INT IDENTITY(1,1) PRIMARY KEY,
    name            VARCHAR(50)     NOT NULL,
    start_time      TIME(7)         NOT NULL,
    end_time        TIME(7)         NOT NULL,
    break_minutes   INT             NULL DEFAULT 0,
    total_hours     DECIMAL(4,2)    NULL
);
GO

/* ----------------------------------------------------------
   5. employee
   ---------------------------------------------------------- */
CREATE TABLE dbo.employee (
    id                  INT IDENTITY(1,1) PRIMARY KEY,
    code                VARCHAR(10)     NOT NULL,
    depart              NVARCHAR(100)   NULL,
    name                NVARCHAR(100)   NOT NULL,
    [position]          NVARCHAR(100)   NULL,
    gender              NVARCHAR(20)    NULL,
    dateofbirth         DATE            NULL,
    nationalid          VARCHAR(14)     NULL,
    hire_date           DATE            NULL,
    employment_type     NVARCHAR(50)    NULL,   -- e.g. Full-time / Part-time / Contract
    employment_status   NVARCHAR(50)    NOT NULL DEFAULT 'Active', -- Active / Terminated
    termination_date    DATE            NULL,
    termination_reason  NVARCHAR(100)   NULL,
    phone               VARCHAR(20)     NULL,
    email               VARCHAR(50)     NULL,
    address             NVARCHAR(100)   NULL,
    emergency_contact   NVARCHAR(50)    NULL,
    e_relation          NVARCHAR(20)    NULL,
    e_phone1            VARCHAR(20)     NULL,
    e_phone2            VARCHAR(20)     NULL,
    e_email             VARCHAR(50)     NULL,
    e_address           NVARCHAR(100)   NULL,
    creat_date          DATETIME        NOT NULL DEFAULT GETDATE(),
    insurance_no        VARCHAR(15)     NULL,
    marital_st          NVARCHAR(20)    NULL,
    late_ex             BIT             NOT NULL DEFAULT 0,  -- exempt from late-tolerance rules
    workhours           INT             NULL,
    weekend1            NVARCHAR(50)    NULL,   -- e.g. 'Friday'
    weekend2            NVARCHAR(50)    NULL,   -- e.g. 'Saturday'
    basic_salary        DECIMAL(12,2)   NOT NULL DEFAULT 0,  -- fixed monthly base pay
    allowance           DECIMAL(12,2)   NOT NULL DEFAULT 0,  -- fixed monthly allowance
    overtime_rate       DECIMAL(10,2)   NOT NULL DEFAULT 0,  -- flat currency paid per overtime hour
    late_deduction_rate DECIMAL(10,2)   NOT NULL DEFAULT 0,  -- currency deducted per late/early-leave hour
    salary_completion   DECIMAL(12,2)   NOT NULL DEFAULT 0,  -- fixed monthly salary top-up/completion
    other_entitlements  DECIMAL(12,2)   NOT NULL DEFAULT 0,  -- any other fixed monthly entitlement
    is_insured          BIT             NOT NULL DEFAULT 0,  -- Insurance Status: Insured / Not Insured
    social_insurance_amount DECIMAL(12,2) NOT NULL DEFAULT 0, -- monthly deduction, applied only while is_insured = 1
    monthly_tax         DECIMAL(12,2)   NOT NULL DEFAULT 0,  -- fixed monthly tax deduction
    annual_leave_days   INT             NOT NULL DEFAULT 21, -- entitlement for the current year
    leave_carried_over  INT             NOT NULL DEFAULT 0,  -- days carried over from previous years (HR-maintained)
    manager_code        VARCHAR(10)     NULL,       -- code of the employee this person reports to directly
    CONSTRAINT UQ_employee_code UNIQUE (code),
    CONSTRAINT FK_employee_department FOREIGN KEY (depart)
        REFERENCES dbo.department(name),
    CONSTRAINT FK_employee_manager FOREIGN KEY (manager_code)
        REFERENCES dbo.employee(code)
);
GO
CREATE INDEX IX_employee_depart ON dbo.employee(depart);
GO

/* ----------------------------------------------------------
   6. Attendance (raw check-in/check-out events)
   ---------------------------------------------------------- */
CREATE TABLE dbo.Attendance (
    id                  INT IDENTITY(1,1) PRIMARY KEY,
    employee_code       VARCHAR(10)     NOT NULL,
    depart              NVARCHAR(100)   NULL,
    name                NVARCHAR(100)   NULL,
    checkin_date        DATE            NULL,
    checkin             TIME(7)         NULL,
    checkout_date       DATE            NULL,
    checkout            TIME(7)         NULL,
    shift_              INT             NULL,
    WorkingHours        INT             NULL,
    MinutesLate         INT             NULL DEFAULT 0,
    MinutesEarlyLeave   INT             NULL DEFAULT 0,
    Overtime            DECIMAL(4,2)    NULL DEFAULT 0,
    status1             NVARCHAR(50)    NULL,   -- Present / Absent / Late / Leave / Holiday
    approved            BIT             NOT NULL DEFAULT 0,
    daynam              NVARCHAR(20)    NULL,
    TWHours             AS (CAST(ISNULL(WorkingHours,0) AS DECIMAL(6,2)) + ISNULL(Overtime,0)) PERSISTED,
    absent_reason       NVARCHAR(MAX)   NULL,
    CONSTRAINT FK_Attendance_employee FOREIGN KEY (employee_code)
        REFERENCES dbo.employee(code),
    CONSTRAINT FK_Attendance_Shifts FOREIGN KEY (shift_)
        REFERENCES dbo.Shifts(ShiftID)
);
GO
CREATE INDEX IX_Attendance_EmployeeDate ON dbo.Attendance(employee_code, checkin_date);
GO

/* ----------------------------------------------------------
   7. attend_summary (per-employee, per-month rollup)
   ---------------------------------------------------------- */
CREATE TABLE dbo.attend_summary (
    id                  INT IDENTITY(1,1) PRIMARY KEY,
    employee_code       VARCHAR(10)     NOT NULL,
    depart              NVARCHAR(100)   NULL,
    name                NVARCHAR(100)   NULL,
    [month]             NVARCHAR(50)    NOT NULL,  -- e.g. '2026-08'
    days_count          INT             NULL DEFAULT 0,
    present             INT             NULL DEFAULT 0,
    late                INT             NULL DEFAULT 0,
    late_hours          DECIMAL(4,1)    NULL DEFAULT 0,
    earlyL              INT             NULL DEFAULT 0,
    earlyL_hours        DECIMAL(4,1)    NULL DEFAULT 0,
    leaves              INT             NULL DEFAULT 0,
    holidays            INT             NULL DEFAULT 0,
    absent              INT             NULL DEFAULT 0,
    overtime            INT             NULL DEFAULT 0,
    overtime_hours      DECIMAL(4,1)    NULL DEFAULT 0,
    CONSTRAINT FK_attend_summary_employee FOREIGN KEY (employee_code)
        REFERENCES dbo.employee(code),
    CONSTRAINT UQ_attend_summary_emp_month UNIQUE (employee_code, [month])
);
GO

/* ----------------------------------------------------------
   8. Holidays
   ---------------------------------------------------------- */
CREATE TABLE dbo.Holidays (
    HolidayID       INT IDENTITY(1,1) PRIMARY KEY,
    HolidayDate     DATE            NOT NULL,
    Description     NVARCHAR(100)   NULL
);
GO

/* ----------------------------------------------------------
   9. Leaves
   ---------------------------------------------------------- */
CREATE TABLE dbo.Leaves (
    LeaveID     INT IDENTITY(1,1) PRIMARY KEY,
    Emp_code    VARCHAR(10)     NOT NULL,
    Empl_name   NVARCHAR(50)    NULL,
    StartDate   DATE            NOT NULL,
    EndDate     DATE            NOT NULL,
    LeaveType   NVARCHAR(50)    NULL,
    Reason      NVARCHAR(100)   NULL,
    Status      NVARCHAR(20)    NOT NULL DEFAULT 'Pending',  -- Pending/Approved/Rejected (added for approval workflow)
    CONSTRAINT FK_Leaves_employee FOREIGN KEY (Emp_code)
        REFERENCES dbo.employee(code)
);
GO

/* ----------------------------------------------------------
   10. Payroll (kept from the original module; re-wired to
       employee_code so it follows the same convention as
       Attendance / Leaves / attend_summary)
   ---------------------------------------------------------- */
CREATE TABLE dbo.Payroll (
    PayrollID       INT IDENTITY(1,1) PRIMARY KEY,
    employee_code   VARCHAR(10)     NOT NULL,
    PayPeriodStart  DATE            NOT NULL,
    PayPeriodEnd    DATE            NOT NULL,
    BasicSalary     DECIMAL(12,2)   NOT NULL DEFAULT 0,
    Allowances      DECIMAL(12,2)   NOT NULL DEFAULT 0,
    Deductions      DECIMAL(12,2)   NOT NULL DEFAULT 0,
    OvertimeHours   DECIMAL(6,2)    NOT NULL DEFAULT 0,
    OvertimeAmount  DECIMAL(12,2)   NOT NULL DEFAULT 0,
    NetSalary       AS (BasicSalary + Allowances + OvertimeAmount - Deductions) PERSISTED,
    PaymentDate     DATE            NULL,
    Status          NVARCHAR(20)    NOT NULL DEFAULT 'Pending', -- Pending/Paid
    CONSTRAINT FK_Payroll_employee FOREIGN KEY (employee_code)
        REFERENCES dbo.employee(code)
);
GO

/* ----------------------------------------------------------
   11. EmployeeDocuments (centralized personal-profile document hub —
       National ID, Birth Certificate, Photos, Graduation Certificate,
       Criminal Record, Military Status Certificate, Labor Office Card,
       Social Insurance Printout, Medical Fitness Certificate, Contracts,
       etc. The files themselves are copied into an "EmployeeDocuments"
       folder alongside the app; this table just tracks what was
       imported, its category, and where it landed.)
   ---------------------------------------------------------- */
CREATE TABLE dbo.EmployeeDocuments (
    id              INT IDENTITY(1,1) PRIMARY KEY,
    employee_code   VARCHAR(10)     NOT NULL,
    document_type   NVARCHAR(60)    NOT NULL DEFAULT 'Other', -- category, see CK_EmployeeDocuments_type
    file_name       NVARCHAR(255)   NOT NULL,   -- original file name shown to the user
    stored_path     NVARCHAR(500)   NOT NULL,   -- path of the copy stored inside the app's folder
    description     NVARCHAR(255)   NULL,
    uploaded_date   DATETIME        NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_EmployeeDocuments_employee FOREIGN KEY (employee_code)
        REFERENCES dbo.employee(code),
    CONSTRAINT CK_EmployeeDocuments_type CHECK (document_type IN (
        'National ID Card', 'Birth Certificate', 'Personal Photo', 'Graduation Certificate',
        'Criminal Record (Fish)', 'Military Status Certificate', 'Labor Office Card (Kaab Amal)',
        'Social Insurance Printout (Taameenat)', 'Medical Fitness Certificate', 'Contract', 'Other'
    ))
);
GO
CREATE INDEX IX_EmployeeDocuments_employee ON dbo.EmployeeDocuments(employee_code);
GO

/* ----------------------------------------------------------
   11b. DocumentLibrary (the general-purpose "Documents" hub —
        Important Documents / Free Documents / Other, independent
        of any single employee. Files are copied into an
        "important documents" folder alongside the app; this table
        just tracks what was imported, its category, and where it
        landed.)
   ---------------------------------------------------------- */
CREATE TABLE dbo.DocumentLibrary (
    id                  INT IDENTITY(1,1) PRIMARY KEY,
    category            NVARCHAR(20)  NOT NULL,   -- canonical (not translated): Important / Free / Other
    display_name        NVARCHAR(255) NOT NULL,
    original_file_name  NVARCHAR(255) NOT NULL,
    stored_path         NVARCHAR(500) NOT NULL,
    file_size           BIGINT        NULL,
    uploaded_date       DATETIME      NOT NULL DEFAULT GETDATE(),
    uploaded_by         NVARCHAR(50)  NULL,
    CONSTRAINT CK_DocumentLibrary_category CHECK (category IN ('Important', 'Free', 'Other'))
);
GO

/* ----------------------------------------------------------
   12. EmploymentHistory (unified employment history log — covers
       Career Progression, Employment Record, Job History Log,
       Internal Mobility Record, and Performance History all in one
       table via event_type, rather than five overlapping tables.
       Department/Position/Salary transfers are logged automatically
       by the app when an employee's record is edited; Performance
       Review and other entries are logged manually.)
   ---------------------------------------------------------- */
CREATE TABLE dbo.EmploymentHistory (
    id                  INT IDENTITY(1,1) PRIMARY KEY,
    employee_code       VARCHAR(10)     NOT NULL,
    event_date          DATE            NOT NULL DEFAULT CAST(GETDATE() AS DATE),
    event_type          NVARCHAR(50)    NOT NULL, -- see CK_EmploymentHistory_type
    old_department      NVARCHAR(100)   NULL,
    new_department      NVARCHAR(100)   NULL,
    old_position        NVARCHAR(100)   NULL,
    new_position         NVARCHAR(100)   NULL,
    old_salary          DECIMAL(12,2)   NULL,
    new_salary          DECIMAL(12,2)   NULL,
    performance_rating  NVARCHAR(20)    NULL,     -- e.g. Excellent / Good / Average / Needs Improvement
    notes               NVARCHAR(500)   NULL,
    recorded_date       DATETIME        NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_EmploymentHistory_employee FOREIGN KEY (employee_code)
        REFERENCES dbo.employee(code),
    CONSTRAINT CK_EmploymentHistory_type CHECK (event_type IN (
        'Hire', 'Promotion', 'Department Transfer', 'Position Change', 'Salary Change',
        'Performance Review', 'Termination', 'Rehire', 'Other'
    ))
);
GO
CREATE INDEX IX_EmploymentHistory_employee ON dbo.EmploymentHistory(employee_code, event_date);
GO

/* ----------------------------------------------------------
   11b. SalaryHistory (append-only log of salary snapshots — one
        row per Save from the Salary Details expander in
        EmployeesView, dated, so HR can see how a salary breakdown
        changed over time)
   ---------------------------------------------------------- */
CREATE TABLE dbo.SalaryHistory (
    id                      INT IDENTITY(1,1) PRIMARY KEY,
    employee_code           VARCHAR(10)    NOT NULL,
    recorded_date           DATETIME       NOT NULL DEFAULT GETDATE(),
    basic_salary            DECIMAL(12,2)  NOT NULL DEFAULT 0,
    fixed_allowance         DECIMAL(12,2)  NOT NULL DEFAULT 0,
    salary_completion       DECIMAL(12,2)  NOT NULL DEFAULT 0,
    other_entitlements      DECIMAL(12,2)  NOT NULL DEFAULT 0,
    is_insured              BIT            NOT NULL DEFAULT 0,
    social_insurance_amount DECIMAL(12,2)  NOT NULL DEFAULT 0,
    monthly_tax             DECIMAL(12,2)  NOT NULL DEFAULT 0,
    changed_by              NVARCHAR(50)   NULL,
    CONSTRAINT FK_SalaryHistory_employee FOREIGN KEY (employee_code)
        REFERENCES dbo.employee(code)
);
GO
CREATE INDEX IX_SalaryHistory_employee ON dbo.SalaryHistory(employee_code);
GO

/* ----------------------------------------------------------
   12. PayrollAdjustments (Bonuses / Penalties / Incentives —
       one-off amounts for a specific month that feed straight
       into automatic payroll generation)
   ---------------------------------------------------------- */
CREATE TABLE dbo.PayrollAdjustments (
    id              INT IDENTITY(1,1) PRIMARY KEY,
    employee_code   VARCHAR(10)     NOT NULL,
    [type]          NVARCHAR(20)    NOT NULL,   -- Bonus / Penalty / Incentive
    [month]         NVARCHAR(7)     NOT NULL,   -- 'YYYY-MM'
    amount          DECIMAL(12,2)   NOT NULL,   -- always a positive number; type decides +/- effect
    description     NVARCHAR(255)   NULL,
    created_date    DATETIME        NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_PayrollAdjustments_employee FOREIGN KEY (employee_code)
        REFERENCES dbo.employee(code),
    CONSTRAINT CK_PayrollAdjustments_type CHECK ([type] IN ('Bonus', 'Penalty', 'Incentive'))
);
GO
CREATE INDEX IX_PayrollAdjustments_emp_month ON dbo.PayrollAdjustments(employee_code, [month]);
GO

/* ----------------------------------------------------------
   13. CashAdvances (salary advances, deducted automatically from
       payroll over one or more monthly installments)
   ---------------------------------------------------------- */
CREATE TABLE dbo.CashAdvances (
    id                  INT IDENTITY(1,1) PRIMARY KEY,
    employee_code       VARCHAR(10)     NOT NULL,
    amount              DECIMAL(12,2)   NOT NULL,          -- total advance amount
    installments        INT             NOT NULL DEFAULT 1, -- number of months to spread it over
    monthly_deduction   DECIMAL(12,2)   NOT NULL,          -- amount/installments, rounded
    start_month         NVARCHAR(7)     NOT NULL,          -- 'YYYY-MM' of the first deduction
    status              NVARCHAR(20)    NOT NULL DEFAULT 'Active', -- Active / Completed
    request_date        DATETIME        NOT NULL DEFAULT GETDATE(),
    notes               NVARCHAR(255)   NULL,
    CONSTRAINT FK_CashAdvances_employee FOREIGN KEY (employee_code)
        REFERENCES dbo.employee(code)
);
GO
CREATE INDEX IX_CashAdvances_employee ON dbo.CashAdvances(employee_code);
GO

/* ----------------------------------------------------------
   14. Roles / Permissions / RolePermissions / Users / AppSettings
       (login accounts, role-based module permissions, and the
       Language app setting — backs the Settings screen)
   ---------------------------------------------------------- */
CREATE TABLE dbo.Roles (
    id          INT IDENTITY(1,1) PRIMARY KEY,
    name        NVARCHAR(50)  NOT NULL,
    description NVARCHAR(200) NULL,
    CONSTRAINT UQ_Roles_name UNIQUE (name)
);
GO

CREATE TABLE dbo.Permissions (
    id            INT IDENTITY(1,1) PRIMARY KEY,
    module_key    NVARCHAR(50)  NOT NULL,   -- matches MainWindow nav keys, e.g. 'Employees'
    display_name  NVARCHAR(100) NOT NULL,
    CONSTRAINT UQ_Permissions_module UNIQUE (module_key)
);
GO

CREATE TABLE dbo.RolePermissions (
    id             INT IDENTITY(1,1) PRIMARY KEY,
    role_id        INT NOT NULL,
    permission_id  INT NOT NULL,
    can_view       BIT NOT NULL DEFAULT 0,
    can_edit       BIT NOT NULL DEFAULT 0,   -- add/edit/delete within that module
    CONSTRAINT FK_RolePermissions_Role FOREIGN KEY (role_id)
        REFERENCES dbo.Roles(id) ON DELETE CASCADE,
    CONSTRAINT FK_RolePermissions_Permission FOREIGN KEY (permission_id)
        REFERENCES dbo.Permissions(id) ON DELETE CASCADE,
    CONSTRAINT UQ_RolePermissions UNIQUE (role_id, permission_id)
);
GO

CREATE TABLE dbo.Users (
    id              INT IDENTITY(1,1) PRIMARY KEY,
    username        NVARCHAR(50)   NOT NULL,
    password_hash   NVARCHAR(256)  NOT NULL,  -- PBKDF2-SHA256 hash, base64
    password_salt   NVARCHAR(256)  NOT NULL,  -- base64 salt
    full_name       NVARCHAR(100)  NOT NULL,
    employee_code   VARCHAR(10)    NULL,
    role_id         INT            NOT NULL,
    is_active       BIT            NOT NULL DEFAULT 1,
    created_date    DATETIME       NOT NULL DEFAULT GETDATE(),
    last_login      DATETIME       NULL,
    CONSTRAINT UQ_Users_username UNIQUE (username),
    CONSTRAINT FK_Users_Role FOREIGN KEY (role_id) REFERENCES dbo.Roles(id),
    CONSTRAINT FK_Users_Employee FOREIGN KEY (employee_code) REFERENCES dbo.employee(code)
);
GO

CREATE TABLE dbo.AppSettings (
    [key]   NVARCHAR(50)  NOT NULL PRIMARY KEY,
    [value] NVARCHAR(200) NOT NULL
);
GO

/* ============================================================
   Seed data
   ============================================================ */
INSERT INTO dbo.department (code, name, empl_code, type, [date], parent_code) VALUES
('HR', 'Human Resources', NULL, 'Support', '2020-01-01', NULL),
('IT', 'IT', NULL, 'Support', '2020-01-01', NULL),
('FIN', 'Finance', NULL, 'Support', '2020-01-01', NULL),
('SLS', 'Sales', NULL, 'Revenue', '2020-01-01', NULL);
GO

-- Example sub-departments (main departments above have parent_code = NULL)
INSERT INTO dbo.department (code, name, empl_code, type, [date], parent_code) VALUES
('IT-DEV', 'IT - Development', NULL, 'Support', '2021-01-01', 'IT'),
('IT-SUP', 'IT - Support', NULL, 'Support', '2021-01-01', 'IT'),
('SLS-DOM', 'Sales - Domestic', NULL, 'Revenue', '2021-01-01', 'SLS');
GO

INSERT INTO dbo.JobPositions (Code, Title, Depart, JobDescription) VALUES
('HRM', 'HR Manager', 'Human Resources', 'Oversees recruitment, payroll coordination and employee relations.'),
('SWE', 'Software Engineer', 'IT', 'Builds and maintains internal systems.'),
('ACC', 'Accountant', 'Finance', 'Handles bookkeeping and financial reporting.'),
('SLX', 'Sales Executive', 'Sales', 'Manages client accounts and new business.');
GO

INSERT INTO dbo.Shifts (ShiftName, StartTime, EndTime, LateToleranceMinutes, EarlyLeaveToleranceMinutes, OvertimeGraceMinutes, workingHours, DetectionStartTime, DetectionEndTime) VALUES
('Morning', '09:00', '17:00', 10, 10, 15, 8, '07:00', '19:00'),
('Night', '21:00', '05:00', 10, 10, 15, 8, '19:00', '07:00');
GO

INSERT INTO dbo.WorkShifts (name, start_time, end_time, break_minutes, total_hours) VALUES
('Standard Day', '09:00', '17:00', 60, 7.0),
('Half Day', '09:00', '13:00', 0, 4.0);
GO

INSERT INTO dbo.employee (code, depart, name, [position], gender, dateofbirth, nationalid, hire_date, employment_type, employment_status, phone, email, address, creat_date, marital_st, late_ex, workhours, weekend1, weekend2, basic_salary, allowance, overtime_rate, late_deduction_rate) VALUES
('EMP001', 'Human Resources', 'Ahmed Hassan', 'HR Manager', 'Male', '1990-05-14', NULL, '2021-01-10', 'Full-time', 'Active', '01000000001', 'ahmed.hassan@example.com', 'Cairo, Egypt', GETDATE(), 'Married', 0, 8, 'Friday', 'Saturday', 12000, 500, 60, 20),
('EMP002', 'IT', 'Mona Ali', 'Software Engineer', 'Female', '1995-08-22', NULL, '2022-03-01', 'Full-time', 'Active', '01000000002', 'mona.ali@example.com', 'Zagazig, Egypt', GETDATE(), 'Single', 0, 8, 'Friday', 'Saturday', 10000, 300, 50, 15);
GO

INSERT INTO dbo.Payroll (employee_code, PayPeriodStart, PayPeriodEnd, BasicSalary, Allowances, Deductions, OvertimeHours, OvertimeAmount, Status) VALUES
('EMP001', '2026-08-01', '2026-08-31', 12000, 500, 0, 0, 0, 'Pending'),
('EMP002', '2026-08-01', '2026-08-31', 10000, 300, 0, 0, 0, 'Pending');
GO

INSERT INTO dbo.Roles (name, description) VALUES
('Administrator', 'Full access to every module, including Users & Permissions.'),
('HR Manager', 'Full access to HR/payroll modules; no access to Settings.'),
('Viewer', 'Read-only access to most modules.');
GO

INSERT INTO dbo.Permissions (module_key, display_name) VALUES
('Dashboard', 'Dashboard'),
('Employees', 'Employees'),
('Departments', 'Departments & Positions'),
('OrgStructure', 'Org Structure'),
('Shifts', 'Shifts'),
('Attendance', 'Attendance'),
('Leave', 'Leave & Holidays'),
('Adjustments', 'Bonuses & Penalties'),
('CashAdvances', 'Cash Advances'),
('Payroll', 'Payroll'),
('Documents', 'Documents'),
('Settings', 'Settings (Users & Permissions, Language)');
GO

-- Administrator: full view+edit on everything
INSERT INTO dbo.RolePermissions (role_id, permission_id, can_view, can_edit)
SELECT r.id, p.id, 1, 1
FROM dbo.Roles r CROSS JOIN dbo.Permissions p
WHERE r.name = 'Administrator';
GO

-- HR Manager: view+edit everything except Settings
INSERT INTO dbo.RolePermissions (role_id, permission_id, can_view, can_edit)
SELECT r.id, p.id,
       CASE WHEN p.module_key = 'Settings' THEN 0 ELSE 1 END,
       CASE WHEN p.module_key = 'Settings' THEN 0 ELSE 1 END
FROM dbo.Roles r CROSS JOIN dbo.Permissions p
WHERE r.name = 'HR Manager';
GO

-- Viewer: view-only on everything except Settings
INSERT INTO dbo.RolePermissions (role_id, permission_id, can_view, can_edit)
SELECT r.id, p.id,
       CASE WHEN p.module_key = 'Settings' THEN 0 ELSE 1 END,
       0
FROM dbo.Roles r CROSS JOIN dbo.Permissions p
WHERE r.name = 'Viewer';
GO

-- Default administrator login: username "admin", password "admin123"
-- (PBKDF2-SHA256, 100,000 iterations — matches Data/AuthHelper.cs).
-- CHANGE THIS PASSWORD after first login via Settings > Users.
INSERT INTO dbo.Users (username, password_hash, password_salt, full_name, employee_code, role_id, is_active)
SELECT 'admin',
       'aKSKDKli4lPmRRDefZGvmrH836SS837Oyj4vrBufOtA=',
       'd0RSvDAi4lVIzvVtAtUSMg==',
       'System Administrator',
       NULL,
       r.id,
       1
FROM dbo.Roles r WHERE r.name = 'Administrator';
GO

INSERT INTO dbo.AppSettings ([key], [value]) VALUES ('Language', 'en');
GO

PRINT 'HR_ERP database (v2 schema, with Payroll retained) created and seeded successfully.';
PRINT 'Default login -> username: admin / password: admin123 (change it via Settings > Users).';
GO
