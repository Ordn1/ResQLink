-- =============================================
-- Add Security Columns to Users and Volunteers
-- Safe to run multiple times (checks if columns exist first)
-- =============================================

USE [db32781]
GO

PRINT 'Adding security columns to Users table...'

-- Add FailedLoginAttempts to Users if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = 'FailedLoginAttempts')
BEGIN
    ALTER TABLE [dbo].[Users] ADD [FailedLoginAttempts] [int] NOT NULL DEFAULT 0
    PRINT '✓ Added FailedLoginAttempts to Users'
END
ELSE
BEGIN
    PRINT '- FailedLoginAttempts already exists in Users'
END

-- Add LockoutEnd to Users if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = 'LockoutEnd')
BEGIN
    ALTER TABLE [dbo].[Users] ADD [LockoutEnd] [datetime2](7) NULL
    PRINT '✓ Added LockoutEnd to Users'
END
ELSE
BEGIN
    PRINT '- LockoutEnd already exists in Users'
END

-- Add RequiresPasswordReset to Users if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = 'RequiresPasswordReset')
BEGIN
    ALTER TABLE [dbo].[Users] ADD [RequiresPasswordReset] [bit] NOT NULL DEFAULT 0
    PRINT '✓ Added RequiresPasswordReset to Users'
END
ELSE
BEGIN
    PRINT '- RequiresPasswordReset already exists in Users'
END

PRINT ''
PRINT 'Adding security columns to Volunteers table...'

-- Add FailedLoginAttempts to Volunteers if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Volunteers]') AND name = 'FailedLoginAttempts')
BEGIN
    ALTER TABLE [dbo].[Volunteers] ADD [FailedLoginAttempts] [int] NOT NULL DEFAULT 0
    PRINT '✓ Added FailedLoginAttempts to Volunteers'
END
ELSE
BEGIN
    PRINT '- FailedLoginAttempts already exists in Volunteers'
END

-- Add LockoutEnd to Volunteers if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Volunteers]') AND name = 'LockoutEnd')
BEGIN
    ALTER TABLE [dbo].[Volunteers] ADD [LockoutEnd] [datetime2](7) NULL
    PRINT '✓ Added LockoutEnd to Volunteers'
END
ELSE
BEGIN
    PRINT '- LockoutEnd already exists in Volunteers'
END

-- Add RequiresPasswordReset to Volunteers if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Volunteers]') AND name = 'RequiresPasswordReset')
BEGIN
    ALTER TABLE [dbo].[Volunteers] ADD [RequiresPasswordReset] [bit] NOT NULL DEFAULT 0
    PRINT '✓ Added RequiresPasswordReset to Volunteers'
END
ELSE
BEGIN
    PRINT '- RequiresPasswordReset already exists in Volunteers'
END

PRINT ''
PRINT '========================================='
PRINT 'Security columns migration completed!'
PRINT '========================================='
GO
