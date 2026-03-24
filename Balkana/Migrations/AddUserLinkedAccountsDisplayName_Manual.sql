-- Manual migration: Add DisplayName column to UserLinkedAccounts.
-- Execute against your SQL Server database.
-- Used for Riot gameName#tagLine and other provider-specific friendly display names.

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[UserLinkedAccounts]') AND name = 'DisplayName'
)
BEGIN
    ALTER TABLE [dbo].[UserLinkedAccounts] ADD [DisplayName] nvarchar(200) NULL;
END
GO
