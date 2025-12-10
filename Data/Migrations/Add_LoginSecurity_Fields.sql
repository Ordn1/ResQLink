-- Migration: Add Login Security Fields to Users and Volunteers tables
-- Date: December 9, 2025
-- Description: Adds FailedLoginAttempts, LockoutEnd, and RequiresPasswordReset fields
--              to support 3-attempt lockout with 5-minute cooldown

-- Add security fields to Users table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = 'FailedLoginAttempts')
BEGIN
    ALTER TABLE [dbo].[Users]
    ADD [FailedLoginAttempts] INT NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = 'LockoutEnd')
BEGIN
    ALTER TABLE [dbo].[Users]
    ADD [LockoutEnd] DATETIME2 NULL;
END;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = 'RequiresPasswordReset')
BEGIN
    ALTER TABLE [dbo].[Users]
    ADD [RequiresPasswordReset] BIT NOT NULL DEFAULT 0;
END;

-- Add security fields to Volunteers table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Volunteers]') AND name = 'FailedLoginAttempts')
BEGIN
    ALTER TABLE [dbo].[Volunteers]
    ADD [FailedLoginAttempts] INT NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Volunteers]') AND name = 'LockoutEnd')
BEGIN
    ALTER TABLE [dbo].[Volunteers]
    ADD [LockoutEnd] DATETIME2 NULL;
END;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Volunteers]') AND name = 'RequiresPasswordReset')
BEGIN
    ALTER TABLE [dbo].[Volunteers]
    ADD [RequiresPasswordReset] BIT NOT NULL DEFAULT 0;
END;

PRINT 'Login security fields added successfully to Users and Volunteers tables.';
