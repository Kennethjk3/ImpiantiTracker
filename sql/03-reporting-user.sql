-- A login that can only read the maintenance plan, e.g. for an Excel or Power BI connection.
-- Least privilege: it can't see or change the raw tables.
-- Run 01-maintenance-plan.sql first (it creates the view).
USE master;
GO

IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'report_reader')
    CREATE LOGIN report_reader WITH PASSWORD = N'Change_Me_2026!', CHECK_POLICY = ON;
GO

USE Impianti;
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'report_reader')
    CREATE USER report_reader FOR LOGIN report_reader;
GO

-- The view and its tables share the owner (dbo), so SELECT on the view is enough
-- ("ownership chaining"): no permission on the tables themselves is needed.
GRANT SELECT ON dbo.vw_MaintenancePlan TO report_reader;
GO

-- To allow reading every table instead:
-- ALTER ROLE db_datareader ADD MEMBER report_reader;

-- Check what the user is allowed to do:
SELECT pr.name, pe.permission_name, pe.state_desc, OBJECT_NAME(pe.major_id) AS [Object]
FROM sys.database_permissions AS pe
JOIN sys.database_principals AS pr ON pr.principal_id = pe.grantee_principal_id
WHERE pr.name = N'report_reader';
GO
