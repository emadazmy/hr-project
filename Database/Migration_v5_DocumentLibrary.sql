/* ============================================================
   Migration: adds the DocumentLibrary table (the general-purpose
   "Documents" hub — Important Documents / Free Documents / Other,
   independent of any single employee) and registers a "Documents"
   permission module so it can be granted per role from
   Settings > Roles & Permissions like every other module.

   Safe to run on an existing database — does NOT drop or modify
   any existing tables or data, only adds what's missing. Safe to
   re-run too (every change is guarded with an IF/NOT EXISTS check).

   Run this AFTER HR_ERP_Database.sql (or the earlier migrations)
   on a database that doesn't have this table yet. If you're
   building a brand new database, just run HR_ERP_Database.sql —
   it already includes everything below.
   ============================================================ */

USE HR_ERP;
GO

IF OBJECT_ID('dbo.DocumentLibrary', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.DocumentLibrary (
        id                  INT IDENTITY(1,1) PRIMARY KEY,
        category            NVARCHAR(20)  NOT NULL,   -- canonical (not translated): Important / Free / Other
        display_name        NVARCHAR(255) NOT NULL,   -- the (possibly renamed) name shown in the icon grid, incl. extension
        original_file_name  NVARCHAR(255) NOT NULL,   -- the file's name at the moment it was imported
        stored_path         NVARCHAR(500) NOT NULL,   -- path of the copy stored inside the app's "important documents" folder
        file_size           BIGINT        NULL,
        uploaded_date       DATETIME      NOT NULL DEFAULT GETDATE(),
        uploaded_by         NVARCHAR(50)  NULL,       -- username of whoever imported it
        CONSTRAINT CK_DocumentLibrary_category CHECK (category IN ('Important', 'Free', 'Other'))
    );
END
GO

-- Register the new module + grant it to every existing role the same way every other module
-- is granted (view for everyone, add/edit/delete for everyone except Viewer) — nothing needs
-- to be done by hand in Settings > Roles & Permissions after this runs.
IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE module_key = 'Documents')
BEGIN
    INSERT INTO dbo.Permissions (module_key, display_name) VALUES ('Documents', 'Documents');
END
GO

INSERT INTO dbo.RolePermissions (role_id, permission_id, can_view, can_edit)
SELECT r.id, p.id,
       1,                                      -- everyone can view
       CASE WHEN r.name = 'Viewer' THEN 0 ELSE 1 END  -- Admin/HR Manager can import/rename/delete, Viewer can't
FROM dbo.Roles r
CROSS JOIN dbo.Permissions p
WHERE p.module_key = 'Documents'
  AND NOT EXISTS (SELECT 1 FROM dbo.RolePermissions rp WHERE rp.role_id = r.id AND rp.permission_id = p.id);
GO

PRINT 'DocumentLibrary table created, and the Documents permission was granted to existing roles.';
GO
