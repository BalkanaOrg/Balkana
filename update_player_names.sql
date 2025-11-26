-- ============================================================================
-- SQL Script to Make Player FirstName and LastName Nullable
-- This script handles both schema changes and data updates
-- Run this script directly on your SQL Server database
-- ============================================================================

-- Step 1: Preview records that will be affected
-- Uncomment to see what will be updated:
/*
SELECT Id, Nickname, FirstName, LastName
FROM Players
WHERE FirstName = 'Random' OR LastName = 'Randomov';
*/

-- Step 2: Update data first (set 'Random' and 'Randomov' to NULL)
-- This must be done before altering the columns to nullable
PRINT 'Updating data: Setting FirstName = NULL where FirstName = ''Random''...';
UPDATE Players
SET FirstName = NULL
WHERE FirstName = 'Random';

PRINT 'Updating data: Setting LastName = NULL where LastName = ''Randomov''...';
UPDATE Players
SET LastName = NULL
WHERE LastName = 'Randomov';

-- Step 3: Alter the columns to allow NULL
-- Note: This preserves the existing column length (NVARCHAR(30) based on NameMaxLength)
-- If you get errors about constraints, you may need to drop them first

PRINT 'Altering FirstName column to allow NULL...';
-- Preserve existing length (30 characters based on NameMaxLength constant)
ALTER TABLE Players
ALTER COLUMN FirstName NVARCHAR(30) NULL;

PRINT 'Altering LastName column to allow NULL...';
-- Preserve existing length (30 characters based on NameMaxLength constant)
ALTER TABLE Players
ALTER COLUMN LastName NVARCHAR(30) NULL;

PRINT 'Migration completed successfully!';
PRINT 'FirstName and LastName columns are now nullable.';

-- Step 4: Verify the changes
SELECT 
    COLUMN_NAME,
    IS_NULLABLE,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Players' 
    AND COLUMN_NAME IN ('FirstName', 'LastName');

