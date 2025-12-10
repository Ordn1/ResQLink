-- ============================================================================
-- QUICK START: Run this on your database to enable the new Archive system
-- ============================================================================
-- Database: resqlink (local) or db34346 (remote)
-- Date: December 10, 2025
-- ============================================================================

-- Step 1: Verify you're in the correct database
SELECT DB_NAME() AS CurrentDatabase;
-- Should show: resqlink (local) or db34346 (remote)

-- Step 2: Check if Archives table already exists
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Archives')
    PRINT '⚠️ Archives table already exists - migration may have run before'
ELSE
    PRINT '✓ Ready to create Archives table';

-- Step 3: Run the full migration
-- Execute the entire Create_Archives_Table.sql file here, or:
-- In SSMS: File -> Open -> Create_Archives_Table.sql -> Execute

-- Step 4: Verify the Archives table was created
SELECT 
    COLUMN_NAME, 
    DATA_TYPE, 
    IS_NULLABLE 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Archives'
ORDER BY ORDINAL_POSITION;

-- Expected output:
-- ArchiveId    int             NO
-- EntityType   nvarchar        NO
-- EntityId     int             NO
-- ArchivedData nvarchar        NO
-- ArchivedAt   datetime2       NO
-- ArchivedBy   int             NO
-- ArchiveReason nvarchar       YES
-- EntityName   nvarchar        YES

-- Step 5: Check if any data was migrated
SELECT 
    EntityType,
    COUNT(*) AS ArchivedCount,
    MIN(ArchivedAt) AS OldestArchive,
    MAX(ArchivedAt) AS NewestArchive
FROM Archives
GROUP BY EntityType
ORDER BY ArchivedCount DESC;

-- Step 6: View sample archived records
SELECT TOP 5
    ArchiveId,
    EntityType,
    EntityName,
    ArchivedAt,
    ArchiveReason
FROM Archives
ORDER BY ArchivedAt DESC;

-- ============================================================================
-- Testing Queries (Run these to verify the system works)
-- ============================================================================

-- Test 1: Check that old archive columns still exist (they should for now)
SELECT 
    TABLE_NAME,
    COLUMN_NAME
FROM INFORMATION_SCHEMA.COLUMNS
WHERE COLUMN_NAME IN ('IsArchived', 'ArchivedAt', 'ArchivedBy', 'ArchiveReason')
ORDER BY TABLE_NAME, COLUMN_NAME;

-- Test 2: Count archived vs active records per table
-- Categories
SELECT 
    CASE WHEN IsArchived = 1 THEN 'Archived' ELSE 'Active' END AS Status,
    COUNT(*) AS Count
FROM Categories
GROUP BY IsArchived;

-- Relief Goods
SELECT 
    CASE WHEN IsArchived = 1 THEN 'Archived' ELSE 'Active' END AS Status,
    COUNT(*) AS Count
FROM Relief_Goods
GROUP BY IsArchived;

-- Disasters
SELECT 
    CASE WHEN IsArchived = 1 THEN 'Archived' ELSE 'Active' END AS Status,
    COUNT(*) AS Count
FROM Disasters
GROUP BY IsArchived;

-- Test 3: Find any duplicates (same entity archived multiple times)
SELECT 
    EntityType,
    EntityId,
    COUNT(*) AS DuplicateCount
FROM Archives
GROUP BY EntityType, EntityId
HAVING COUNT(*) > 1;
-- Should return 0 rows (no duplicates)

-- ============================================================================
-- Maintenance Queries
-- ============================================================================

-- View Archives by type
SELECT EntityType, COUNT(*) AS Count
FROM Archives
GROUP BY EntityType
ORDER BY Count DESC;

-- Find specific archived item
SELECT 
    ArchiveId,
    EntityType,
    EntityName,
    ArchivedAt,
    ArchiveReason,
    ArchivedData -- Full JSON snapshot
FROM Archives
WHERE EntityName LIKE '%search term%'
ORDER BY ArchivedAt DESC;

-- Check archive size
SELECT 
    COUNT(*) AS TotalArchives,
    MIN(ArchivedAt) AS OldestArchive,
    MAX(ArchivedAt) AS NewestArchive,
    SUM(DATALENGTH(ArchivedData)) / 1024.0 / 1024.0 AS SizeInMB
FROM Archives;

-- Find large archived records
SELECT TOP 10
    ArchiveId,
    EntityType,
    EntityName,
    DATALENGTH(ArchivedData) AS SizeInBytes,
    DATALENGTH(ArchivedData) / 1024.0 AS SizeInKB
FROM Archives
ORDER BY DATALENGTH(ArchivedData) DESC;

-- ============================================================================
-- Cleanup (Only after testing!)
-- ============================================================================

-- Remove old archived records from Archives table (older than 1 year)
-- CAUTION: This permanently deletes data!
/*
DELETE FROM Archives
WHERE ArchivedAt < DATEADD(YEAR, -1, GETUTCDATE());

PRINT CAST(@@ROWCOUNT AS VARCHAR(10)) + ' old archives permanently deleted';
*/

-- Drop old archive columns (only after everything works!)
-- CAUTION: This cannot be undone! Make backups first!
-- Uncomment Step 3 in Create_Archives_Table.sql instead of running this

-- ============================================================================
-- Rollback (If something goes wrong)
-- ============================================================================

-- To completely remove the Archives system and revert:
/*
-- 1. Drop the Archives table
DROP TABLE IF EXISTS Archives;

-- 2. In your application, revert the service changes to use old archive fields
-- 3. Remove ArchiveService registrations from MauiProgram.cs
-- 4. The old archive columns will still be intact and functional
*/

-- ============================================================================
-- Production Deployment Checklist
-- ============================================================================
/*
Before deploying to production:

1. [ ] Test thoroughly in development environment
2. [ ] Backup your production database
3. [ ] Run this script during a maintenance window
4. [ ] Verify Archives table is created
5. [ ] Check that data was migrated correctly
6. [ ] Deploy updated application code
7. [ ] Test archive/restore functionality
8. [ ] Monitor audit logs for any issues
9. [ ] Keep old archive columns for at least 1-2 weeks
10. [ ] After verification, drop old columns using Step 3 of migration script

Backup command (before running migration):
BACKUP DATABASE [resqlink] TO DISK = 'C:\Backups\resqlink_before_archive_migration.bak'

Restore command (if rollback needed):
RESTORE DATABASE [resqlink] FROM DISK = 'C:\Backups\resqlink_before_archive_migration.bak' WITH REPLACE
*/

-- ============================================================================
PRINT '✓ Quick reference guide loaded - ready to use';
PRINT 'Next step: Run the full Create_Archives_Table.sql script';
-- ============================================================================
