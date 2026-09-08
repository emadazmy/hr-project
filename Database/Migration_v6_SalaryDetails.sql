/* ============================================================
   Migration: adds the salary breakdown fields used by the new
   "Salary Details" expander in Employees (Entitlements: Basic
   Salary / Fixed Allowances / Salary Completion / Other
   Entitlements — Deductions: Social Insurance / Monthly Tax),
   the Insurance Status field on the main Add/Edit Employee form,
   and a dated SalaryHistory log of every change.

   Safe to run on an existing database — does NOT drop or modify
   any existing tables or data, only adds what's missing. Safe to
   re-run too (every change is guarded with an IF check).

   Run this AFTER HR_ERP_Database.sql (or the earlier migrations)
   on a database that doesn't have these columns yet. If you're
   building a brand new database, just run HR_ERP_Database.sql —
   it already includes everything below.
   ============================================================ */

USE HR_ERP;
GO

-- New salary/insurance columns on employee. basic_salary and allowance already existed
-- (they've simply moved, in the UI, from the main Add/Edit form into the new Salary Details
-- expander); everything else here is new.
IF COL_LENGTH('dbo.employee', 'salary_completion') IS NULL
    ALTER TABLE dbo.employee ADD salary_completion DECIMAL(12,2) NOT NULL DEFAULT 0;
GO

IF COL_LENGTH('dbo.employee', 'other_entitlements') IS NULL
    ALTER TABLE dbo.employee ADD other_entitlements DECIMAL(12,2) NOT NULL DEFAULT 0;
GO

IF COL_LENGTH('dbo.employee', 'is_insured') IS NULL
    ALTER TABLE dbo.employee ADD is_insured BIT NOT NULL DEFAULT 0;
GO

IF COL_LENGTH('dbo.employee', 'social_insurance_amount') IS NULL
    ALTER TABLE dbo.employee ADD social_insurance_amount DECIMAL(12,2) NOT NULL DEFAULT 0;
GO

IF COL_LENGTH('dbo.employee', 'monthly_tax') IS NULL
    ALTER TABLE dbo.employee ADD monthly_tax DECIMAL(12,2) NOT NULL DEFAULT 0;
GO

-- Append-only history: one snapshot row per Save from the Salary Details expander.
IF OBJECT_ID('dbo.SalaryHistory', 'U') IS NULL
BEGIN
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
    CREATE INDEX IX_SalaryHistory_employee ON dbo.SalaryHistory(employee_code);
END
GO

PRINT 'Salary Details fields and SalaryHistory table added successfully.';
GO
