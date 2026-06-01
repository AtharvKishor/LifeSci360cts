-- Run this in SSMS against LifeSci360_Services
USE LifeSci360_Services;
GO

-- Add Notes column to Samples table
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Samples') AND name = 'Notes')
BEGIN
    ALTER TABLE Samples ADD Notes NVARCHAR(MAX) NULL;
    PRINT 'Notes column added successfully.';
END
ELSE
    PRINT 'Notes column already exists.';
GO
