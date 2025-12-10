-- Check EXACT table names in remote database
-- This will show if there's a naming mismatch

USE [db32781]
GO

PRINT 'Checking table names in database...';
PRINT '';

SELECT 
    TABLE_NAME,
    CASE 
        WHEN TABLE_NAME = 'Relief_Goods' THEN '✓'
        WHEN TABLE_NAME = 'ReliefGoods' THEN '⚠ Wrong name - should be Relief_Goods'
        ELSE ''
    END AS Status
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
  AND TABLE_NAME LIKE '%Relief%'
  OR TABLE_NAME LIKE '%Disaster%'
  OR TABLE_NAME LIKE '%Categor%'
  OR TABLE_NAME LIKE '%Supplier%'
  OR TABLE_NAME LIKE '%Stock%'
  OR TABLE_NAME LIKE '%Procurement%'
  OR TABLE_NAME LIKE '%Barangay%'
ORDER BY TABLE_NAME;

PRINT '';
PRINT '============================================================';
PRINT 'Checking which tables have archive columns...';
PRINT '============================================================';
PRINT '';

-- Check each possible table name variant
DECLARE @tables TABLE (TableName NVARCHAR(128));

INSERT INTO @tables VALUES 
    ('Relief_Goods'), ('ReliefGoods'),
    ('Disasters'), ('Disaster'),
    ('Categories'), ('Category'),
    ('Suppliers'), ('Supplier'),
    ('Stocks'), ('Stock'),
    ('ProcurementRequests'), ('ProcurementRequest'),
    ('BarangayBudgets'), ('BarangayBudget');

SELECT 
    t.TableName,
    CASE WHEN EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
        WHERE TABLE_NAME = t.TableName AND COLUMN_NAME = 'IsArchived'
    ) THEN '✓ Has IsArchived' ELSE '✗ Missing IsArchived' END AS ArchiveStatus,
    CASE WHEN EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.TABLES 
        WHERE TABLE_NAME = t.TableName
    ) THEN '✓ Exists' ELSE '✗ Does not exist' END AS TableExists
FROM @tables t
WHERE EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = t.TableName)
ORDER BY t.TableName;
