-- Diagnostic Script: Check Remote Database Schema
-- Run this on your remote database (db32781) to see what columns exist
-- This will help identify which tables are missing archive fields

USE [db32781]
GO

PRINT '============================================================';
PRINT 'CHECKING ARCHIVE FIELDS IN REMOTE DATABASE';
PRINT '============================================================';
PRINT '';

-- Check Relief_Goods
PRINT '1. Relief_Goods table:';
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Relief_Goods')
BEGIN
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Relief_Goods]') AND name = 'IsArchived')
        PRINT '   ✓ IsArchived exists'
    ELSE
        PRINT '   ✗ IsArchived MISSING';
    
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Relief_Goods]') AND name = 'ArchivedAt')
        PRINT '   ✓ ArchivedAt exists'
    ELSE
        PRINT '   ✗ ArchivedAt MISSING';
    
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Relief_Goods]') AND name = 'ArchivedBy')
        PRINT '   ✓ ArchivedBy exists'
    ELSE
        PRINT '   ✗ ArchivedBy MISSING';
    
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Relief_Goods]') AND name = 'ArchiveReason')
        PRINT '   ✓ ArchiveReason exists'
    ELSE
        PRINT '   ✗ ArchiveReason MISSING';
END
ELSE
    PRINT '   ✗ TABLE DOES NOT EXIST';
PRINT '';

-- Check Disasters
PRINT '2. Disasters table:';
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Disasters')
BEGIN
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Disasters]') AND name = 'IsArchived')
        PRINT '   ✓ IsArchived exists'
    ELSE
        PRINT '   ✗ IsArchived MISSING';
    
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Disasters]') AND name = 'ArchivedAt')
        PRINT '   ✓ ArchivedAt exists'
    ELSE
        PRINT '   ✗ ArchivedAt MISSING';
    
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Disasters]') AND name = 'ArchivedBy')
        PRINT '   ✓ ArchivedBy exists'
    ELSE
        PRINT '   ✗ ArchivedBy MISSING';
    
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Disasters]') AND name = 'ArchiveReason')
        PRINT '   ✓ ArchiveReason exists'
    ELSE
        PRINT '   ✗ ArchiveReason MISSING';
END
ELSE
    PRINT '   ✗ TABLE DOES NOT EXIST';
PRINT '';

-- Check Categories
PRINT '3. Categories table:';
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Categories')
BEGIN
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Categories]') AND name = 'IsArchived')
        PRINT '   ✓ IsArchived exists'
    ELSE
        PRINT '   ✗ IsArchived MISSING';
    
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Categories]') AND name = 'ArchivedAt')
        PRINT '   ✓ ArchivedAt exists'
    ELSE
        PRINT '   ✗ ArchivedAt MISSING';
    
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Categories]') AND name = 'ArchivedBy')
        PRINT '   ✓ ArchivedBy exists'
    ELSE
        PRINT '   ✗ ArchivedBy MISSING';
    
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Categories]') AND name = 'ArchiveReason')
        PRINT '   ✓ ArchiveReason exists'
    ELSE
        PRINT '   ✗ ArchiveReason MISSING';
END
ELSE
    PRINT '   ✗ TABLE DOES NOT EXIST';
PRINT '';

-- Check Suppliers
PRINT '4. Suppliers table:';
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Suppliers')
BEGIN
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Suppliers]') AND name = 'IsArchived')
        PRINT '   ✓ IsArchived exists'
    ELSE
        PRINT '   ✗ IsArchived MISSING';
    
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Suppliers]') AND name = 'ArchivedAt')
        PRINT '   ✓ ArchivedAt exists'
    ELSE
        PRINT '   ✗ ArchivedAt MISSING';
    
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Suppliers]') AND name = 'ArchivedBy')
        PRINT '   ✓ ArchivedBy exists'
    ELSE
        PRINT '   ✗ ArchivedBy MISSING';
    
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Suppliers]') AND name = 'ArchiveReason')
        PRINT '   ✓ ArchiveReason exists'
    ELSE
        PRINT '   ✗ ArchiveReason MISSING';
END
ELSE
    PRINT '   ✗ TABLE DOES NOT EXIST';
PRINT '';

-- Check Stocks
PRINT '5. Stocks table:';
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Stocks')
BEGIN
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Stocks]') AND name = 'IsArchived')
        PRINT '   ✓ IsArchived exists'
    ELSE
        PRINT '   ✗ IsArchived MISSING';
    
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Stocks]') AND name = 'ArchivedAt')
        PRINT '   ✓ ArchivedAt exists'
    ELSE
        PRINT '   ✗ ArchivedAt MISSING';
    
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Stocks]') AND name = 'ArchivedBy')
        PRINT '   ✓ ArchivedBy exists'
    ELSE
        PRINT '   ✗ ArchivedBy MISSING';
    
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Stocks]') AND name = 'ArchiveReason')
        PRINT '   ✓ ArchiveReason exists'
    ELSE
        PRINT '   ✗ ArchiveReason MISSING';
END
ELSE
    PRINT '   ✗ TABLE DOES NOT EXIST';
PRINT '';

-- Check ProcurementRequests
PRINT '6. ProcurementRequests table:';
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ProcurementRequests')
BEGIN
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ProcurementRequests]') AND name = 'IsArchived')
        PRINT '   ✓ IsArchived exists'
    ELSE
        PRINT '   ✗ IsArchived MISSING';
    
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ProcurementRequests]') AND name = 'ArchivedAt')
        PRINT '   ✓ ArchivedAt exists'
    ELSE
        PRINT '   ✗ ArchivedAt MISSING';
    
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ProcurementRequests]') AND name = 'ArchivedBy')
        PRINT '   ✓ ArchivedBy exists'
    ELSE
        PRINT '   ✗ ArchivedBy MISSING';
    
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ProcurementRequests]') AND name = 'ArchiveReason')
        PRINT '   ✓ ArchiveReason exists'
    ELSE
        PRINT '   ✗ ArchiveReason MISSING';
END
ELSE
    PRINT '   ✗ TABLE DOES NOT EXIST';
PRINT '';

-- Check BarangayBudgets
PRINT '7. BarangayBudgets table:';
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'BarangayBudgets')
BEGIN
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[BarangayBudgets]') AND name = 'IsArchived')
        PRINT '   ✓ IsArchived exists'
    ELSE
        PRINT '   ✗ IsArchived MISSING';
    
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[BarangayBudgets]') AND name = 'ArchivedAt')
        PRINT '   ✓ ArchivedAt exists'
    ELSE
        PRINT '   ✗ ArchivedAt MISSING';
    
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[BarangayBudgets]') AND name = 'ArchivedBy')
        PRINT '   ✓ ArchivedBy exists'
    ELSE
        PRINT '   ✗ ArchivedBy MISSING';
    
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[BarangayBudgets]') AND name = 'ArchiveReason')
        PRINT '   ✓ ArchiveReason exists'
    ELSE
        PRINT '   ✗ ArchiveReason MISSING';
END
ELSE
    PRINT '   ✗ TABLE DOES NOT EXIST';
PRINT '';

PRINT '============================================================';
PRINT 'CHECK COMPLETE';
PRINT '============================================================';
