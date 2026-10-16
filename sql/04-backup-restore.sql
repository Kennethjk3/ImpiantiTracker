-- Back up the database, check the backup, and practise a restore into a copy.
-- A backup you've never restored is a hope, not a backup.
USE master;   -- a database can't be restored while a session is using it
GO

-- Recovery model: FULL allows point-in-time restores but needs regular log backups
-- (BACKUP LOG), otherwise the transaction log keeps growing. SIMPLE needs only full/differential backups.
SELECT name, recovery_model_desc FROM sys.databases WHERE name = N'Impianti';
GO

-- 1. Full backup. COPY_ONLY leaves any scheduled backup chain untouched; CHECKSUM detects corruption.
BACKUP DATABASE Impianti
TO DISK = N'/var/opt/mssql/data/Impianti_full.bak'
WITH COPY_ONLY, INIT, CHECKSUM, STATS = 25, NAME = N'Impianti full backup';
GO

-- 2. Is the file readable and complete?
RESTORE VERIFYONLY FROM DISK = N'/var/opt/mssql/data/Impianti_full.bak' WITH CHECKSUM;
GO

-- 3. The logical file names inside the backup (needed by MOVE below).
--    A database created by EF Core is normally "Impianti" and "Impianti_log"; if the
--    names shown here differ, use those in step 4.
RESTORE FILELISTONLY FROM DISK = N'/var/opt/mssql/data/Impianti_full.bak';
GO

-- 4. Restore as a separate database, so the original is never overwritten.
RESTORE DATABASE Impianti_RestoreTest
FROM DISK = N'/var/opt/mssql/data/Impianti_full.bak'
WITH MOVE N'Impianti'     TO N'/var/opt/mssql/data/Impianti_RestoreTest.mdf',
     MOVE N'Impianti_log' TO N'/var/opt/mssql/data/Impianti_RestoreTest_log.ldf',
     REPLACE, STATS = 25;
GO

-- 5. Prove it worked: the copy should have the same rows as the original.
SELECT
    (SELECT COUNT(*) FROM Impianti.dbo.ServiceVisits)             AS VisitsOriginal,
    (SELECT COUNT(*) FROM Impianti_RestoreTest.dbo.ServiceVisits) AS VisitsRestored;
GO

-- 6. Clean up the test copy.
DROP DATABASE Impianti_RestoreTest;
GO
