-- Migration: Create Centralized Archives Table
-- Date: December 10, 2025
-- Description: Creates a single Archives table to store all archived records,
--              replacing individual archive fields on each table.
--              This improves database design and simplifies archive management.

USE [resqlink]
GO

PRINT '============================================================';
PRINT 'Creating Centralized Archives Table';
PRINT '============================================================';
PRINT '';

-- ============================================================================
-- STEP 1: Create Archives Table
-- ============================================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Archives')
BEGIN
    PRINT 'Creating Archives table...';
    
    CREATE TABLE [dbo].[Archives] (
        [ArchiveId] INT IDENTITY(1,1) NOT NULL,
        [EntityType] NVARCHAR(100) NOT NULL,
        [EntityId] INT NOT NULL,
        [ArchivedData] NVARCHAR(MAX) NOT NULL,
        [ArchivedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        [ArchivedBy] INT NOT NULL,
        [ArchiveReason] NVARCHAR(500) NULL,
        [EntityName] NVARCHAR(200) NULL,
        CONSTRAINT [PK_Archives] PRIMARY KEY CLUSTERED ([ArchiveId] ASC),
        CONSTRAINT [FK_Archives_Users] FOREIGN KEY ([ArchivedBy]) 
            REFERENCES [dbo].[Users] ([UserId])
    );

    -- Create indexes for faster lookups
    CREATE NONCLUSTERED INDEX [IX_Archives_EntityType_EntityId] 
        ON [dbo].[Archives] ([EntityType], [EntityId]);
    
    CREATE NONCLUSTERED INDEX [IX_Archives_ArchivedAt] 
        ON [dbo].[Archives] ([ArchivedAt] DESC);
    
    CREATE NONCLUSTERED INDEX [IX_Archives_ArchivedBy] 
        ON [dbo].[Archives] ([ArchivedBy]);

    PRINT '✓ Archives table created successfully';
END
ELSE
BEGIN
    PRINT '⚠ Archives table already exists, skipping creation';
END
GO

-- ============================================================================
-- STEP 2: Migrate Existing Archived Data (Optional)
-- ============================================================================

PRINT '';
PRINT 'Migrating existing archived records...';
PRINT '';

-- Migrate ReliefGoods
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Relief_Goods]') AND name = 'IsArchived')
BEGIN
    INSERT INTO [dbo].[Archives] ([EntityType], [EntityId], [ArchivedData], [ArchivedAt], [ArchivedBy], [ArchiveReason], [EntityName])
    SELECT 
        'ReliefGood' AS EntityType,
        [RgId] AS EntityId,
        (SELECT * FROM [dbo].[Relief_Goods] rg WHERE rg.[RgId] = t.[RgId] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) AS ArchivedData,
        ISNULL([ArchivedAt], GETUTCDATE()) AS ArchivedAt,
        ISNULL([ArchivedBy], 1) AS ArchivedBy,
        [ArchiveReason],
        [Name] AS EntityName
    FROM [dbo].[Relief_Goods] t
    WHERE [IsArchived] = 1;
    
    PRINT '✓ Migrated ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' archived Relief Goods';
END

-- Migrate Disasters
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Disasters]') AND name = 'IsArchived')
BEGIN
    INSERT INTO [dbo].[Archives] ([EntityType], [EntityId], [ArchivedData], [ArchivedAt], [ArchivedBy], [ArchiveReason], [EntityName])
    SELECT 
        'Disaster' AS EntityType,
        [DisasterId] AS EntityId,
        (SELECT * FROM [dbo].[Disasters] d WHERE d.[DisasterId] = t.[DisasterId] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) AS ArchivedData,
        ISNULL([ArchivedAt], GETUTCDATE()) AS ArchivedAt,
        ISNULL([ArchivedBy], 1) AS ArchivedBy,
        [ArchiveReason],
        [Title] AS EntityName
    FROM [dbo].[Disasters] t
    WHERE [IsArchived] = 1;
    
    PRINT '✓ Migrated ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' archived Disasters';
END

-- Migrate Categories
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Categories]') AND name = 'IsArchived')
BEGIN
    INSERT INTO [dbo].[Archives] ([EntityType], [EntityId], [ArchivedData], [ArchivedAt], [ArchivedBy], [ArchiveReason], [EntityName])
    SELECT 
        'Category' AS EntityType,
        [CategoryId] AS EntityId,
        (SELECT * FROM [dbo].[Categories] c WHERE c.[CategoryId] = t.[CategoryId] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) AS ArchivedData,
        ISNULL([ArchivedAt], GETUTCDATE()) AS ArchivedAt,
        ISNULL([ArchivedBy], 1) AS ArchivedBy,
        [ArchiveReason],
        [CategoryName] AS EntityName
    FROM [dbo].[Categories] t
    WHERE [IsArchived] = 1;
    
    PRINT '✓ Migrated ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' archived Categories';
END

-- Migrate Suppliers
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Suppliers]') AND name = 'IsArchived')
BEGIN
    INSERT INTO [dbo].[Archives] ([EntityType], [EntityId], [ArchivedData], [ArchivedAt], [ArchivedBy], [ArchiveReason], [EntityName])
    SELECT 
        'Supplier' AS EntityType,
        [SupplierId] AS EntityId,
        (SELECT * FROM [dbo].[Suppliers] s WHERE s.[SupplierId] = t.[SupplierId] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) AS ArchivedData,
        ISNULL([ArchivedAt], GETUTCDATE()) AS ArchivedAt,
        ISNULL([ArchivedBy], 1) AS ArchivedBy,
        [ArchiveReason],
        [SupplierName] AS EntityName
    FROM [dbo].[Suppliers] t
    WHERE [IsArchived] = 1;
    
    PRINT '✓ Migrated ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' archived Suppliers';
END

-- Migrate Stocks
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Stocks]') AND name = 'IsArchived')
BEGIN
    INSERT INTO [dbo].[Archives] ([EntityType], [EntityId], [ArchivedData], [ArchivedAt], [ArchivedBy], [ArchiveReason], [EntityName])
    SELECT 
        'Stock' AS EntityType,
        [StockId] AS EntityId,
        (SELECT * FROM [dbo].[Stocks] s WHERE s.[StockId] = t.[StockId] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) AS ArchivedData,
        ISNULL([ArchivedAt], GETUTCDATE()) AS ArchivedAt,
        ISNULL([ArchivedBy], 1) AS ArchivedBy,
        [ArchiveReason],
        [ItemName] AS EntityName
    FROM [dbo].[Stocks] t
    WHERE [IsArchived] = 1;
    
    PRINT '✓ Migrated ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' archived Stocks';
END

-- Migrate ProcurementRequests
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ProcurementRequests]') AND name = 'IsArchived')
BEGIN
    INSERT INTO [dbo].[Archives] ([EntityType], [EntityId], [ArchivedData], [ArchivedAt], [ArchivedBy], [ArchiveReason], [EntityName])
    SELECT 
        'ProcurementRequest' AS EntityType,
        [RequestId] AS EntityId,
        (SELECT * FROM [dbo].[ProcurementRequests] pr WHERE pr.[RequestId] = t.[RequestId] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) AS ArchivedData,
        ISNULL([ArchivedAt], GETUTCDATE()) AS ArchivedAt,
        ISNULL([ArchivedBy], 1) AS ArchivedBy,
        [ArchiveReason],
        'Request #' + CAST([RequestId] AS NVARCHAR(50)) AS EntityName
    FROM [dbo].[ProcurementRequests] t
    WHERE [IsArchived] = 1;
    
    PRINT '✓ Migrated ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' archived Procurement Requests';
END

-- Migrate BarangayBudgets
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[BarangayBudgets]') AND name = 'IsArchived')
BEGIN
    INSERT INTO [dbo].[Archives] ([EntityType], [EntityId], [ArchivedData], [ArchivedAt], [ArchivedBy], [ArchiveReason], [EntityName])
    SELECT 
        'BarangayBudget' AS EntityType,
        [BudgetId] AS EntityId,
        (SELECT * FROM [dbo].[BarangayBudgets] bb WHERE bb.[BudgetId] = t.[BudgetId] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) AS ArchivedData,
        ISNULL([ArchivedAt], GETUTCDATE()) AS ArchivedAt,
        ISNULL([ArchivedBy], 1) AS ArchivedBy,
        [ArchiveReason],
        [BarangayName] + ' ' + CAST([Year] AS NVARCHAR(4)) AS EntityName
    FROM [dbo].[BarangayBudgets] t
    WHERE [IsArchived] = 1;
    
    PRINT '✓ Migrated ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' archived Barangay Budgets';
END

GO

-- ============================================================================
-- STEP 3: Drop Archive Columns from Tables (Optional - Uncomment to execute)
-- ============================================================================

PRINT '';
PRINT '============================================================';
PRINT 'Archive columns can now be removed from individual tables';
PRINT 'Uncomment the DROP COLUMN statements below to remove them';
PRINT '============================================================';
PRINT '';

/*
-- Drop archive columns from Relief_Goods
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Relief_Goods]') AND name = 'IsArchived')
BEGIN
    ALTER TABLE [dbo].[Relief_Goods] DROP COLUMN [IsArchived];
    ALTER TABLE [dbo].[Relief_Goods] DROP COLUMN [ArchivedAt];
    ALTER TABLE [dbo].[Relief_Goods] DROP COLUMN [ArchivedBy];
    ALTER TABLE [dbo].[Relief_Goods] DROP COLUMN [ArchiveReason];
    PRINT '✓ Removed archive columns from Relief_Goods';
END

-- Drop archive columns from Disasters
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Disasters]') AND name = 'IsArchived')
BEGIN
    ALTER TABLE [dbo].[Disasters] DROP COLUMN [IsArchived];
    ALTER TABLE [dbo].[Disasters] DROP COLUMN [ArchivedAt];
    ALTER TABLE [dbo].[Disasters] DROP COLUMN [ArchivedBy];
    ALTER TABLE [dbo].[Disasters] DROP COLUMN [ArchiveReason];
    PRINT '✓ Removed archive columns from Disasters';
END

-- Drop archive columns from Categories
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Categories]') AND name = 'IsArchived')
BEGIN
    ALTER TABLE [dbo].[Categories] DROP COLUMN [IsArchived];
    ALTER TABLE [dbo].[Categories] DROP COLUMN [ArchivedAt];
    ALTER TABLE [dbo].[Categories] DROP COLUMN [ArchivedBy];
    ALTER TABLE [dbo].[Categories] DROP COLUMN [ArchiveReason];
    PRINT '✓ Removed archive columns from Categories';
END

-- Drop archive columns from Suppliers
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Suppliers]') AND name = 'IsArchived')
BEGIN
    ALTER TABLE [dbo].[Suppliers] DROP COLUMN [IsArchived];
    ALTER TABLE [dbo].[Suppliers] DROP COLUMN [ArchivedAt];
    ALTER TABLE [dbo].[Suppliers] DROP COLUMN [ArchivedBy];
    ALTER TABLE [dbo].[Suppliers] DROP COLUMN [ArchiveReason];
    PRINT '✓ Removed archive columns from Suppliers';
END

-- Drop archive columns from Stocks
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Stocks]') AND name = 'IsArchived')
BEGIN
    ALTER TABLE [dbo].[Stocks] DROP COLUMN [IsArchived];
    ALTER TABLE [dbo].[Stocks] DROP COLUMN [ArchivedAt];
    ALTER TABLE [dbo].[Stocks] DROP COLUMN [ArchivedBy];
    ALTER TABLE [dbo].[Stocks] DROP COLUMN [ArchiveReason];
    PRINT '✓ Removed archive columns from Stocks';
END

-- Drop archive columns from ProcurementRequests
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ProcurementRequests]') AND name = 'IsArchived')
BEGIN
    ALTER TABLE [dbo].[ProcurementRequests] DROP COLUMN [IsArchived];
    ALTER TABLE [dbo].[ProcurementRequests] DROP COLUMN [ArchivedAt];
    ALTER TABLE [dbo].[ProcurementRequests] DROP COLUMN [ArchivedBy];
    ALTER TABLE [dbo].[ProcurementRequests] DROP COLUMN [ArchiveReason];
    PRINT '✓ Removed archive columns from ProcurementRequests';
END

-- Drop archive columns from BarangayBudgets
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[BarangayBudgets]') AND name = 'IsArchived')
BEGIN
    ALTER TABLE [dbo].[BarangayBudgets] DROP COLUMN [IsArchived];
    ALTER TABLE [dbo].[BarangayBudgets] DROP COLUMN [ArchivedAt];
    ALTER TABLE [dbo].[BarangayBudgets] DROP COLUMN [ArchivedBy];
    ALTER TABLE [dbo].[BarangayBudgets] DROP COLUMN [ArchiveReason];
    PRINT '✓ Removed archive columns from BarangayBudgets';
END
*/

PRINT '';
PRINT '============================================================';
PRINT 'Migration Complete!';
PRINT '============================================================';
PRINT 'Next Steps:';
PRINT '1. Update your application code to use the new ArchiveService';
PRINT '2. Test the archive/restore functionality';
PRINT '3. Once verified, uncomment Step 3 to drop old columns';
PRINT '============================================================';

GO
