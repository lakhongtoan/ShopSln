-- Script to add UpdatedAt column to CampingTips table
-- Run this script directly in SQL Server Management Studio or via sqlcmd

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CampingTips]') AND name = 'UpdatedAt')
BEGIN
    ALTER TABLE [dbo].[CampingTips]
    ADD [UpdatedAt] datetime2 NULL;
    
    PRINT 'Column UpdatedAt added successfully to CampingTips table.';
END
ELSE
BEGIN
    PRINT 'Column UpdatedAt already exists in CampingTips table.';
END
GO

