-- Force Schema Refresh on Remote Database
-- Run this on remote database (db32781) to ensure SQL Server recognizes the new columns

USE [db32781]
GO

PRINT 'Refreshing database schema metadata...';

-- Update statistics for all tables with archive fields
UPDATE STATISTICS [dbo].[Relief_Goods];
UPDATE STATISTICS [dbo].[Disasters];
UPDATE STATISTICS [dbo].[Categories];
UPDATE STATISTICS [dbo].[Suppliers];
UPDATE STATISTICS [dbo].[Stocks];
UPDATE STATISTICS [dbo].[ProcurementRequests];
UPDATE STATISTICS [dbo].[BarangayBudgets];

-- Clear plan cache to force recompilation
DBCC FREEPROCCACHE;

-- Verify columns exist
SELECT 
    t.name AS TableName,
    c.name AS ColumnName,
    ty.name AS DataType,
    c.is_nullable AS IsNullable
FROM sys.tables t
INNER JOIN sys.columns c ON t.object_id = c.object_id
INNER JOIN sys.types ty ON c.user_type_id = ty.user_type_id
WHERE t.name IN ('Relief_Goods', 'Disasters', 'Categories', 'Suppliers', 'Stocks', 'ProcurementRequests', 'BarangayBudgets')
  AND c.name IN ('IsArchived', 'ArchivedAt', 'ArchivedBy', 'ArchiveReason')
ORDER BY t.name, c.column_id;

PRINT '';
PRINT 'Schema refresh complete!';
PRINT 'If the query above shows all 28 rows (7 tables × 4 columns), the schema is correct.';
PRINT 'Try the sync push again now.';
