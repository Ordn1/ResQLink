-- Migration: Complete Remote Database Schema Fix
-- Date: December 10, 2025
-- Description: Adds all missing archive fields and login security fields to remote database
--              Run this on your remote database (db32781 on MonsterASP)

USE [db32781]
GO

PRINT 'Starting Complete Remote Schema Fix Migration...';
GO

-- ============================================================================
-- PART 1: Add Archive Fields to all archivable entities
-- ============================================================================

-- Add archive fields to Relief_Goods table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Relief_Goods]') AND name = 'IsArchived')
BEGIN
    ALTER TABLE [dbo].[Relief_Goods]
    ADD [IsArchived] BIT NOT NULL DEFAULT 0,
        [ArchivedAt] DATETIME2 NULL,
        [ArchivedBy] INT NULL,
        [ArchiveReason] NVARCHAR(500) NULL;
    
    PRINT 'Archive fields added to Relief_Goods table';
END
ELSE
BEGIN
    PRINT 'Archive fields already exist in Relief_Goods table';
END;
GO

-- Add archive fields to Disasters table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Disasters]') AND name = 'IsArchived')
BEGIN
    ALTER TABLE [dbo].[Disasters]
    ADD [IsArchived] BIT NOT NULL DEFAULT 0,
        [ArchivedAt] DATETIME2 NULL,
        [ArchivedBy] INT NULL,
        [ArchiveReason] NVARCHAR(500) NULL;
    
    PRINT 'Archive fields added to Disasters table';
END
ELSE
BEGIN
    PRINT 'Archive fields already exist in Disasters table';
END;
GO

-- Add archive fields to Categories table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Categories]') AND name = 'IsArchived')
BEGIN
    ALTER TABLE [dbo].[Categories]
    ADD [IsArchived] BIT NOT NULL DEFAULT 0,
        [ArchivedAt] DATETIME2 NULL,
        [ArchivedBy] INT NULL,
        [ArchiveReason] NVARCHAR(500) NULL;
    
    PRINT 'Archive fields added to Categories table';
END
ELSE
BEGIN
    PRINT 'Archive fields already exist in Categories table';
END;
GO

-- Add archive fields to Suppliers table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Suppliers]') AND name = 'IsArchived')
BEGIN
    ALTER TABLE [dbo].[Suppliers]
    ADD [IsArchived] BIT NOT NULL DEFAULT 0,
        [ArchivedAt] DATETIME2 NULL,
        [ArchivedBy] INT NULL,
        [ArchiveReason] NVARCHAR(500) NULL;
    
    PRINT 'Archive fields added to Suppliers table';
END
ELSE
BEGIN
    PRINT 'Archive fields already exist in Suppliers table';
END;
GO

-- Add archive fields to Stocks table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Stocks]') AND name = 'IsArchived')
BEGIN
    ALTER TABLE [dbo].[Stocks]
    ADD [IsArchived] BIT NOT NULL DEFAULT 0,
        [ArchivedAt] DATETIME2 NULL,
        [ArchivedBy] INT NULL,
        [ArchiveReason] NVARCHAR(500) NULL;
    
    PRINT 'Archive fields added to Stocks table';
END
ELSE
BEGIN
    PRINT 'Archive fields already exist in Stocks table';
END;
GO

-- Add archive fields to ProcurementRequests table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ProcurementRequests]') AND name = 'IsArchived')
BEGIN
    ALTER TABLE [dbo].[ProcurementRequests]
    ADD [IsArchived] BIT NOT NULL DEFAULT 0,
        [ArchivedAt] DATETIME2 NULL,
        [ArchivedBy] INT NULL,
        [ArchiveReason] NVARCHAR(500) NULL;
    
    PRINT 'Archive fields added to ProcurementRequests table';
END
ELSE
BEGIN
    PRINT 'Archive fields already exist in ProcurementRequests table';
END;
GO

-- Add archive fields to BarangayBudgets table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[BarangayBudgets]') AND name = 'IsArchived')
BEGIN
    ALTER TABLE [dbo].[BarangayBudgets]
    ADD [IsArchived] BIT NOT NULL DEFAULT 0,
        [ArchivedAt] DATETIME2 NULL,
        [ArchivedBy] INT NULL,
        [ArchiveReason] NVARCHAR(500) NULL;
    
    PRINT 'Archive fields added to BarangayBudgets table';
END
ELSE
BEGIN
    PRINT 'Archive fields already exist in BarangayBudgets table';
END;
GO

-- ============================================================================
-- PART 2: Add Login Security Fields
-- ============================================================================

-- Add security fields to Users table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = 'FailedLoginAttempts')
BEGIN
    ALTER TABLE [dbo].[Users]
    ADD [FailedLoginAttempts] INT NOT NULL DEFAULT 0;
    PRINT 'FailedLoginAttempts added to Users table';
END
ELSE
BEGIN
    PRINT 'FailedLoginAttempts already exists in Users table';
END;
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = 'LockoutEnd')
BEGIN
    ALTER TABLE [dbo].[Users]
    ADD [LockoutEnd] DATETIME2 NULL;
    PRINT 'LockoutEnd added to Users table';
END
ELSE
BEGIN
    PRINT 'LockoutEnd already exists in Users table';
END;
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = 'RequiresPasswordReset')
BEGIN
    ALTER TABLE [dbo].[Users]
    ADD [RequiresPasswordReset] BIT NOT NULL DEFAULT 0;
    PRINT 'RequiresPasswordReset added to Users table';
END
ELSE
BEGIN
    PRINT 'RequiresPasswordReset already exists in Users table';
END;
GO

-- Add security fields to Volunteers table (if it exists)
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Volunteers')
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Volunteers]') AND name = 'FailedLoginAttempts')
    BEGIN
        ALTER TABLE [dbo].[Volunteers]
        ADD [FailedLoginAttempts] INT NOT NULL DEFAULT 0;
        PRINT 'FailedLoginAttempts added to Volunteers table';
    END
    ELSE
    BEGIN
        PRINT 'FailedLoginAttempts already exists in Volunteers table';
    END;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Volunteers]') AND name = 'LockoutEnd')
    BEGIN
        ALTER TABLE [dbo].[Volunteers]
        ADD [LockoutEnd] DATETIME2 NULL;
        PRINT 'LockoutEnd added to Volunteers table';
    END
    ELSE
    BEGIN
        PRINT 'LockoutEnd already exists in Volunteers table';
    END;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Volunteers]') AND name = 'RequiresPasswordReset')
    BEGIN
        ALTER TABLE [dbo].[Volunteers]
        ADD [RequiresPasswordReset] BIT NOT NULL DEFAULT 0;
        PRINT 'RequiresPasswordReset added to Volunteers table';
    END
    ELSE
    BEGIN
        PRINT 'RequiresPasswordReset already exists in Volunteers table';
    END;
END;
GO

-- ============================================================================
-- PART 3: Create indexes for performance
-- ============================================================================

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Relief_Goods_IsArchived' AND object_id = OBJECT_ID(N'[dbo].[Relief_Goods]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Relief_Goods_IsArchived] ON [dbo].[Relief_Goods] ([IsArchived]) INCLUDE ([IsActive]);
    PRINT 'Index created on Relief_Goods.IsArchived';
END;
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Disasters_IsArchived' AND object_id = OBJECT_ID(N'[dbo].[Disasters]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Disasters_IsArchived] ON [dbo].[Disasters] ([IsArchived]) INCLUDE ([Status]);
    PRINT 'Index created on Disasters.IsArchived';
END;
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Categories_IsArchived' AND object_id = OBJECT_ID(N'[dbo].[Categories]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Categories_IsArchived] ON [dbo].[Categories] ([IsArchived]) INCLUDE ([IsActive]);
    PRINT 'Index created on Categories.IsArchived';
END;
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Suppliers_IsArchived' AND object_id = OBJECT_ID(N'[dbo].[Suppliers]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Suppliers_IsArchived] ON [dbo].[Suppliers] ([IsArchived]) INCLUDE ([IsActive]);
    PRINT 'Index created on Suppliers.IsArchived';
END;
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Stocks_IsArchived' AND object_id = OBJECT_ID(N'[dbo].[Stocks]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Stocks_IsArchived] ON [dbo].[Stocks] ([IsArchived]) INCLUDE ([IsActive]);
    PRINT 'Index created on Stocks.IsArchived';
END;
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProcurementRequests_IsArchived' AND object_id = OBJECT_ID(N'[dbo].[ProcurementRequests]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ProcurementRequests_IsArchived] ON [dbo].[ProcurementRequests] ([IsArchived]) INCLUDE ([Status]);
    PRINT 'Index created on ProcurementRequests.IsArchived';
END;
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_BarangayBudgets_IsArchived' AND object_id = OBJECT_ID(N'[dbo].[BarangayBudgets]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_BarangayBudgets_IsArchived] ON [dbo].[BarangayBudgets] ([IsArchived]) INCLUDE ([Status]);
    PRINT 'Index created on BarangayBudgets.IsArchived';
END;
GO

PRINT 'Complete Remote Schema Fix Migration Completed Successfully!';
PRINT '';
PRINT '==================================================================';
PRINT 'Summary:';
PRINT '- Archive fields added to 7 tables';
PRINT '- Login security fields added to Users and Volunteers tables';
PRINT '- Performance indexes created';
PRINT '==================================================================';
GO
