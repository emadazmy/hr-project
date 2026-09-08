/* ============================================================
   Migration: adds EmployeeDocuments table + annual leave
   entitlement/carry-over columns on employee, for databases
   created before these were added to HR_ERP_Database.sql.

   Safe to run on an existing database — does NOT drop or
   modify any existing tables or data, only adds what's missing.
   Safe to re-run too (every change is guarded with an IF check).
   ============================================================ */

USE HR_ERP;
GO

-- 1. Leave entitlement/carry-over columns on employee
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.employee') AND name = 'annual_leave_days')
BEGIN
    ALTER TABLE dbo.employee ADD annual_leave_days INT NOT NULL DEFAULT 21;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.employee') AND name = 'leave_carried_over')
BEGIN
    ALTER TABLE dbo.employee ADD leave_carried_over INT NOT NULL DEFAULT 0;
END
GO

-- 2. EmployeeDocuments table
IF OBJECT_ID('dbo.EmployeeDocuments', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.EmployeeDocuments (
        id              INT IDENTITY(1,1) PRIMARY KEY,
        employee_code   VARCHAR(30)     NOT NULL,
        file_name       NVARCHAR(255)   NOT NULL,
        stored_path     NVARCHAR(500)   NOT NULL,
        description     NVARCHAR(255)   NULL,
        uploaded_date   DATETIME        NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_EmployeeDocuments_employee FOREIGN KEY (employee_code)
            REFERENCES dbo.employee(code)
    );

    CREATE INDEX IX_EmployeeDocuments_employee ON dbo.EmployeeDocuments(employee_code);
END
GO

-- 3. PayrollAdjustments table (Bonuses / Penalties / Incentives)
IF OBJECT_ID('dbo.PayrollAdjustments', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PayrollAdjustments (
        id              INT IDENTITY(1,1) PRIMARY KEY,
        employee_code   VARCHAR(30)     NOT NULL,
        [type]          NVARCHAR(20)    NOT NULL,
        [month]         NVARCHAR(7)     NOT NULL,
        amount          DECIMAL(12,2)   NOT NULL,
        description     NVARCHAR(255)   NULL,
        created_date    DATETIME        NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_PayrollAdjustments_employee FOREIGN KEY (employee_code)
            REFERENCES dbo.employee(code),
        CONSTRAINT CK_PayrollAdjustments_type CHECK ([type] IN ('Bonus', 'Penalty', 'Incentive'))
    );
    CREATE INDEX IX_PayrollAdjustments_emp_month ON dbo.PayrollAdjustments(employee_code, [month]);
END
GO

-- 4. CashAdvances table
IF OBJECT_ID('dbo.CashAdvances', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CashAdvances (
        id                  INT IDENTITY(1,1) PRIMARY KEY,
        employee_code       VARCHAR(30)     NOT NULL,
        amount              DECIMAL(12,2)   NOT NULL,
        installments        INT             NOT NULL DEFAULT 1,
        monthly_deduction   DECIMAL(12,2)   NOT NULL,
        start_month         NVARCHAR(7)     NOT NULL,
        status              NVARCHAR(20)    NOT NULL DEFAULT 'Active',
        request_date        DATETIME        NOT NULL DEFAULT GETDATE(),
        notes               NVARCHAR(255)   NULL,
        CONSTRAINT FK_CashAdvances_employee FOREIGN KEY (employee_code)
            REFERENCES dbo.employee(code)
    );
    CREATE INDEX IX_CashAdvances_employee ON dbo.CashAdvances(employee_code);
END
GO

-- 5. parent_code column on department (main/sub-department hierarchy)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.department') AND name = 'parent_code')
BEGIN
    ALTER TABLE dbo.department ADD parent_code VARCHAR(10) NULL;
    ALTER TABLE dbo.department ADD CONSTRAINT FK_department_parent FOREIGN KEY (parent_code) REFERENCES dbo.department(code);
END
GO

-- 6. document_type column on EmployeeDocuments (personal-profile document hub categories)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.EmployeeDocuments') AND name = 'document_type')
BEGIN
    ALTER TABLE dbo.EmployeeDocuments ADD document_type NVARCHAR(60) NOT NULL DEFAULT 'Other';
    ALTER TABLE dbo.EmployeeDocuments ADD CONSTRAINT CK_EmployeeDocuments_type CHECK (document_type IN (
        'National ID Card', 'Birth Certificate', 'Personal Photo', 'Graduation Certificate',
        'Criminal Record (Fish)', 'Military Status Certificate', 'Labor Office Card (Kaab Amal)',
        'Social Insurance Printout (Taameenat)', 'Medical Fitness Certificate', 'Contract', 'Other'
    ));
END
GO

-- 7. EmploymentHistory table
IF OBJECT_ID('dbo.EmploymentHistory', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.EmploymentHistory (
        id                  INT IDENTITY(1,1) PRIMARY KEY,
        employee_code       VARCHAR(30)     NOT NULL,
        event_date          DATE            NOT NULL DEFAULT CAST(GETDATE() AS DATE),
        event_type          NVARCHAR(50)    NOT NULL,
        old_department      NVARCHAR(100)   NULL,
        new_department      NVARCHAR(100)   NULL,
        old_position        NVARCHAR(100)   NULL,
        new_position        NVARCHAR(100)   NULL,
        old_salary          DECIMAL(12,2)   NULL,
        new_salary          DECIMAL(12,2)   NULL,
        performance_rating  NVARCHAR(20)    NULL,
        notes               NVARCHAR(500)   NULL,
        recorded_date       DATETIME        NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_EmploymentHistory_employee FOREIGN KEY (employee_code)
            REFERENCES dbo.employee(code),
        CONSTRAINT CK_EmploymentHistory_type CHECK (event_type IN (
            'Hire', 'Promotion', 'Department Transfer', 'Position Change', 'Salary Change',
            'Performance Review', 'Termination', 'Rehire', 'Other'
        ))
    );
    CREATE INDEX IX_EmploymentHistory_employee ON dbo.EmploymentHistory(employee_code, event_date);
END
GO

-- 8. manager_code column on employee (reporting-line hierarchy for Org Chart)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.employee') AND name = 'manager_code')
BEGIN
    ALTER TABLE dbo.employee ADD manager_code VARCHAR(30) NULL;
    ALTER TABLE dbo.employee ADD CONSTRAINT FK_employee_manager FOREIGN KEY (manager_code) REFERENCES dbo.employee(code);
END
GO

PRINT 'Migration complete: EmployeeDocuments (+ document_type), PayrollAdjustments, CashAdvances, EmploymentHistory tables, leave-balance columns, department hierarchy, and employee reporting lines are up to date.';
GO
