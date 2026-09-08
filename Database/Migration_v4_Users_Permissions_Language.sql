/* ============================================================
   Migration: adds Users, Roles, Permissions, RolePermissions and
   AppSettings tables — backs the new Settings screen (Users &
   Permissions management + English/Arabic language switch).

   Safe to run on an existing database — does NOT drop or modify
   any existing tables or data, only adds what's missing. Safe to
   re-run too (every change is guarded with an IF check).

   Run this AFTER HR_ERP_Database.sql (or Migration_v3) on a
   database that doesn't have these tables yet. If you're building
   a brand new database, just run HR_ERP_Database.sql — it already
   includes everything below.
   ============================================================ */

USE HR_ERP;
GO

-- 1. Roles (a named permission set — Administrator, HR Manager, Viewer, ...)
IF OBJECT_ID('dbo.Roles', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Roles (
        id          INT IDENTITY(1,1) PRIMARY KEY,
        name        NVARCHAR(50)  NOT NULL,
        description NVARCHAR(200) NULL,
        CONSTRAINT UQ_Roles_name UNIQUE (name)
    );
END
GO

-- 2. Permissions (fixed catalog of app modules/screens — one row per
--    nav entry, including Settings itself)
IF OBJECT_ID('dbo.Permissions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Permissions (
        id            INT IDENTITY(1,1) PRIMARY KEY,
        module_key    NVARCHAR(50)  NOT NULL,   -- matches MainWindow nav keys, e.g. 'Employees'
        display_name  NVARCHAR(100) NOT NULL,
        CONSTRAINT UQ_Permissions_module UNIQUE (module_key)
    );
END
GO

-- 3. RolePermissions (role x module -> can view / can add-edit-delete)
IF OBJECT_ID('dbo.RolePermissions', 'U') IS NULL
BEGIN
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
END
GO

-- 4. Users (login accounts — separate from `employee`; a user can
--    optionally be linked to an employee record, e.g. an HR staff
--    member who is also on the payroll)
IF OBJECT_ID('dbo.Users', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users (
        id              INT IDENTITY(1,1) PRIMARY KEY,
        username        NVARCHAR(50)   NOT NULL,
        password_hash   NVARCHAR(256)  NOT NULL,  -- PBKDF2 hash, base64
        password_salt   NVARCHAR(256)  NOT NULL,  -- base64 salt
        full_name       NVARCHAR(100)  NOT NULL,
        employee_code   VARCHAR(30)    NULL,
        role_id         INT            NOT NULL,
        is_active       BIT            NOT NULL DEFAULT 1,
        created_date    DATETIME       NOT NULL DEFAULT GETDATE(),
        last_login      DATETIME       NULL,
        CONSTRAINT UQ_Users_username UNIQUE (username),
        CONSTRAINT FK_Users_Role FOREIGN KEY (role_id) REFERENCES dbo.Roles(id),
        CONSTRAINT FK_Users_Employee FOREIGN KEY (employee_code) REFERENCES dbo.employee(code)
    );
END
GO

-- 5. AppSettings (simple key/value store — currently just Language,
--    room to grow for other app-wide settings later)
IF OBJECT_ID('dbo.AppSettings', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AppSettings (
        [key]   NVARCHAR(50)  NOT NULL PRIMARY KEY,
        [value] NVARCHAR(200) NOT NULL
    );
END
GO

/* ============================================================
   Seed data — only inserted if the tables are empty, so this
   migration is safe to re-run without duplicating rows.
   ============================================================ */

IF NOT EXISTS (SELECT 1 FROM dbo.Roles)
BEGIN
    INSERT INTO dbo.Roles (name, description) VALUES
    ('Administrator', 'Full access to every module, including Users & Permissions.'),
    ('HR Manager', 'Full access to HR/payroll modules; no access to Settings.'),
    ('Viewer', 'Read-only access to most modules.');
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Permissions)
BEGIN
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
    ('Settings', 'Settings (Users & Permissions, Language)');
END
GO

-- Administrator: full view+edit on everything
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions rp
               JOIN dbo.Roles r ON r.id = rp.role_id WHERE r.name = 'Administrator')
BEGIN
    INSERT INTO dbo.RolePermissions (role_id, permission_id, can_view, can_edit)
    SELECT r.id, p.id, 1, 1
    FROM dbo.Roles r CROSS JOIN dbo.Permissions p
    WHERE r.name = 'Administrator';
END
GO

-- HR Manager: view+edit everything except Settings (view+edit on all
-- operational modules, no access at all to Settings)
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions rp
               JOIN dbo.Roles r ON r.id = rp.role_id WHERE r.name = 'HR Manager')
BEGIN
    INSERT INTO dbo.RolePermissions (role_id, permission_id, can_view, can_edit)
    SELECT r.id, p.id,
           CASE WHEN p.module_key = 'Settings' THEN 0 ELSE 1 END,
           CASE WHEN p.module_key = 'Settings' THEN 0 ELSE 1 END
    FROM dbo.Roles r CROSS JOIN dbo.Permissions p
    WHERE r.name = 'HR Manager';
END
GO

-- Viewer: view-only on everything except Settings (no Settings access at all)
IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions rp
               JOIN dbo.Roles r ON r.id = rp.role_id WHERE r.name = 'Viewer')
BEGIN
    INSERT INTO dbo.RolePermissions (role_id, permission_id, can_view, can_edit)
    SELECT r.id, p.id,
           CASE WHEN p.module_key = 'Settings' THEN 0 ELSE 1 END,
           0
    FROM dbo.Roles r CROSS JOIN dbo.Permissions p
    WHERE r.name = 'Viewer';
END
GO

-- Default administrator login: username "admin", password "admin123"
-- (hash generated with PBKDF2-SHA256, 100,000 iterations — matches
-- Data/AuthHelper.cs). CHANGE THIS PASSWORD after first login via the
-- Settings > Users screen.
IF NOT EXISTS (SELECT 1 FROM dbo.Users)
BEGIN
    INSERT INTO dbo.Users (username, password_hash, password_salt, full_name, employee_code, role_id, is_active)
    SELECT 'admin',
           'aKSKDKli4lPmRRDefZGvmrH836SS837Oyj4vrBufOtA=',
           'd0RSvDAi4lVIzvVtAtUSMg==',
           'System Administrator',
           NULL,
           r.id,
           1
    FROM dbo.Roles r WHERE r.name = 'Administrator';
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppSettings WHERE [key] = 'Language')
BEGIN
    INSERT INTO dbo.AppSettings ([key], [value]) VALUES ('Language', 'en');
END
GO

PRINT 'Users, Roles, Permissions, RolePermissions and AppSettings created/seeded successfully.';
PRINT 'Default login -> username: admin / password: admin123 (change it via Settings > Users).';
GO
