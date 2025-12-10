-- Migration: Add Archive Fields for Soft Delete Support
-- Date: December 9, 2025
-- Description: Adds IsArchived, ArchivedAt, ArchivedBy, and ArchiveReason fields
--              to support archiving instead of deleting records

-- Add archive fields to ReliefGoods table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Relief_Goods]') AND name = 'IsArchived')
BEGIN
    ALTER TABLE [dbo].[Relief_Goods]
    ADD [IsArchived] BIT NOT NULL DEFAULT 0,
        [ArchivedAt] DATETIME2 NULL,
        [ArchivedBy] INT NULL,
        [ArchiveReason] NVARCHAR(500) NULL;
    
    PRINT 'Archive fields added to Relief_Goods table';
END;

-- Add archive fields to Disasters table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Disasters]') AND name = 'IsArchived')
BEGIN
    ALTER TABLE [dbo].[Disasters]
    ADD [IsArchived] BIT NOT NULL DEFAULT 0,
        [ArchivedAt] DATETIME2 NULL,
        [ArchivedBy] INT NULL,
        [ArchiveReason] NVARCHAR(500) NULL;
    
    PRINT 'Archive fields added to Disasters table';
END;

-- Add archive fields to Categories table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Categories]') AND name = 'IsArchived')
BEGIN
    ALTER TABLE [dbo].[Categories]
    ADD [IsArchived] BIT NOT NULL DEFAULT 0,
        [ArchivedAt] DATETIME2 NULL,
        [ArchivedBy] INT NULL,
        [ArchiveReason] NVARCHAR(500) NULL;
    
    PRINT 'Archive fields added to Categories table';
END;

-- Add archive fields to Suppliers table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Suppliers]') AND name = 'IsArchived')
BEGIN
    ALTER TABLE [dbo].[Suppliers]
    ADD [IsArchived] BIT NOT NULL DEFAULT 0,
        [ArchivedAt] DATETIME2 NULL,
        [ArchivedBy] INT NULL,
        [ArchiveReason] NVARCHAR(500) NULL;
    
    PRINT 'Archive fields added to Suppliers table';
END;

-- Add archive fields to Stocks table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Stocks]') AND name = 'IsArchived')
BEGIN
    ALTER TABLE [dbo].[Stocks]
    ADD [IsArchived] BIT NOT NULL DEFAULT 0,
        [ArchivedAt] DATETIME2 NULL,
        [ArchivedBy] INT NULL,
        [ArchiveReason] NVARCHAR(500) NULL;
    
    PRINT 'Archive fields added to Stocks table';
END;

-- Add archive fields to ProcurementRequests table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ProcurementRequests]') AND name = 'IsArchived')
BEGIN
    ALTER TABLE [dbo].[ProcurementRequests]
    ADD [IsArchived] BIT NOT NULL DEFAULT 0,
        [ArchivedAt] DATETIME2 NULL,
        [ArchivedBy] INT NULL,
        [ArchiveReason] NVARCHAR(500) NULL;
    
    PRINT 'Archive fields added to ProcurementRequests table';
END;

-- Add archive fields to BarangayBudgets table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[BarangayBudgets]') AND name = 'IsArchived')
BEGIN
    ALTER TABLE [dbo].[BarangayBudgets]
    ADD [IsArchived] BIT NOT NULL DEFAULT 0,
        [ArchivedAt] DATETIME2 NULL,
        [ArchivedBy] INT NULL,
        [ArchiveReason] NVARCHAR(500) NULL;
    
    PRINT 'Archive fields added to BarangayBudgets table';
END;

-- Create indexes for performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Relief_Goods_IsArchived' AND object_id = OBJECT_ID(N'[dbo].[Relief_Goods]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Relief_Goods_IsArchived] ON [dbo].[Relief_Goods] ([IsArchived]) INCLUDE ([IsActive]);
    PRINT 'Index created on Relief_Goods.IsArchived';
END;

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Disasters_IsArchived' AND object_id = OBJECT_ID(N'[dbo].[Disasters]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Disasters_IsArchived] ON [dbo].[Disasters] ([IsArchived]) INCLUDE ([Status]);
    PRINT 'Index created on Disasters.IsArchived';
END;

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Categories_IsArchived' AND object_id = OBJECT_ID(N'[dbo].[Categories]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Categories_IsArchived] ON [dbo].[Categories] ([IsArchived]) INCLUDE ([IsActive]);
    PRINT 'Index created on Categories.IsArchived';
END;

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Suppliers_IsArchived' AND object_id = OBJECT_ID(N'[dbo].[Suppliers]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Suppliers_IsArchived] ON [dbo].[Suppliers] ([IsArchived]) INCLUDE ([IsActive]);
    PRINT 'Index created on Suppliers.IsArchived';
END;

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Stocks_IsArchived' AND object_id = OBJECT_ID(N'[dbo].[Stocks]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Stocks_IsArchived] ON [dbo].[Stocks] ([IsArchived]) INCLUDE ([IsActive]);
    PRINT 'Index created on Stocks.IsArchived';
END;

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProcurementRequests_IsArchived' AND object_id = OBJECT_ID(N'[dbo].[ProcurementRequests]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ProcurementRequests_IsArchived] ON [dbo].[ProcurementRequests] ([IsArchived]) INCLUDE ([Status]);
    PRINT 'Index created on ProcurementRequests.IsArchived';
END;

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_BarangayBudgets_IsArchived' AND object_id = OBJECT_ID(N'[dbo].[BarangayBudgets]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_BarangayBudgets_IsArchived] ON [dbo].[BarangayBudgets] ([IsArchived]) INCLUDE ([Status]);
    PRINT 'Index created on BarangayBudgets.IsArchived';
END;

PRINT 'Archive fields migration completed successfully!';
