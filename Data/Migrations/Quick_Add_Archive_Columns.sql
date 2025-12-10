-- Quick Fix: Add ONLY Missing Archive Columns
-- Run this on remote database (db32781) if columns are missing
-- This is a simplified version that won't fail if columns already exist

USE [db32781]
GO

PRINT 'Adding missing archive columns...';

-- Relief_Goods
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Relief_Goods]') AND name = 'IsArchived')
    ALTER TABLE [dbo].[Relief_Goods] ADD [IsArchived] BIT NOT NULL DEFAULT 0;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Relief_Goods]') AND name = 'ArchivedAt')
    ALTER TABLE [dbo].[Relief_Goods] ADD [ArchivedAt] DATETIME2 NULL;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Relief_Goods]') AND name = 'ArchivedBy')
    ALTER TABLE [dbo].[Relief_Goods] ADD [ArchivedBy] INT NULL;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Relief_Goods]') AND name = 'ArchiveReason')
    ALTER TABLE [dbo].[Relief_Goods] ADD [ArchiveReason] NVARCHAR(500) NULL;

-- Disasters
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Disasters]') AND name = 'IsArchived')
    ALTER TABLE [dbo].[Disasters] ADD [IsArchived] BIT NOT NULL DEFAULT 0;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Disasters]') AND name = 'ArchivedAt')
    ALTER TABLE [dbo].[Disasters] ADD [ArchivedAt] DATETIME2 NULL;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Disasters]') AND name = 'ArchivedBy')
    ALTER TABLE [dbo].[Disasters] ADD [ArchivedBy] INT NULL;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Disasters]') AND name = 'ArchiveReason')
    ALTER TABLE [dbo].[Disasters] ADD [ArchiveReason] NVARCHAR(500) NULL;

-- Categories
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Categories]') AND name = 'IsArchived')
    ALTER TABLE [dbo].[Categories] ADD [IsArchived] BIT NOT NULL DEFAULT 0;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Categories]') AND name = 'ArchivedAt')
    ALTER TABLE [dbo].[Categories] ADD [ArchivedAt] DATETIME2 NULL;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Categories]') AND name = 'ArchivedBy')
    ALTER TABLE [dbo].[Categories] ADD [ArchivedBy] INT NULL;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Categories]') AND name = 'ArchiveReason')
    ALTER TABLE [dbo].[Categories] ADD [ArchiveReason] NVARCHAR(500) NULL;

-- Suppliers
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Suppliers]') AND name = 'IsArchived')
    ALTER TABLE [dbo].[Suppliers] ADD [IsArchived] BIT NOT NULL DEFAULT 0;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Suppliers]') AND name = 'ArchivedAt')
    ALTER TABLE [dbo].[Suppliers] ADD [ArchivedAt] DATETIME2 NULL;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Suppliers]') AND name = 'ArchivedBy')
    ALTER TABLE [dbo].[Suppliers] ADD [ArchivedBy] INT NULL;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Suppliers]') AND name = 'ArchiveReason')
    ALTER TABLE [dbo].[Suppliers] ADD [ArchiveReason] NVARCHAR(500) NULL;

-- Stocks
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Stocks]') AND name = 'IsArchived')
    ALTER TABLE [dbo].[Stocks] ADD [IsArchived] BIT NOT NULL DEFAULT 0;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Stocks]') AND name = 'ArchivedAt')
    ALTER TABLE [dbo].[Stocks] ADD [ArchivedAt] DATETIME2 NULL;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Stocks]') AND name = 'ArchivedBy')
    ALTER TABLE [dbo].[Stocks] ADD [ArchivedBy] INT NULL;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Stocks]') AND name = 'ArchiveReason')
    ALTER TABLE [dbo].[Stocks] ADD [ArchiveReason] NVARCHAR(500) NULL;

-- ProcurementRequests
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ProcurementRequests]') AND name = 'IsArchived')
    ALTER TABLE [dbo].[ProcurementRequests] ADD [IsArchived] BIT NOT NULL DEFAULT 0;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ProcurementRequests]') AND name = 'ArchivedAt')
    ALTER TABLE [dbo].[ProcurementRequests] ADD [ArchivedAt] DATETIME2 NULL;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ProcurementRequests]') AND name = 'ArchivedBy')
    ALTER TABLE [dbo].[ProcurementRequests] ADD [ArchivedBy] INT NULL;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ProcurementRequests]') AND name = 'ArchiveReason')
    ALTER TABLE [dbo].[ProcurementRequests] ADD [ArchiveReason] NVARCHAR(500) NULL;

-- BarangayBudgets
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[BarangayBudgets]') AND name = 'IsArchived')
    ALTER TABLE [dbo].[BarangayBudgets] ADD [IsArchived] BIT NOT NULL DEFAULT 0;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[BarangayBudgets]') AND name = 'ArchivedAt')
    ALTER TABLE [dbo].[BarangayBudgets] ADD [ArchivedAt] DATETIME2 NULL;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[BarangayBudgets]') AND name = 'ArchivedBy')
    ALTER TABLE [dbo].[BarangayBudgets] ADD [ArchivedBy] INT NULL;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[BarangayBudgets]') AND name = 'ArchiveReason')
    ALTER TABLE [dbo].[BarangayBudgets] ADD [ArchiveReason] NVARCHAR(500) NULL;

PRINT 'Archive columns added successfully!';
PRINT 'You can now try the sync push again.';
