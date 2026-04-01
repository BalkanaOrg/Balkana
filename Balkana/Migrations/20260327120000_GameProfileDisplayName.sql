-- Run this on SQL Server if `dotnet ef database update` is not available.
-- Equivalent to migration 20260327120000_GameProfileDisplayName.

IF NOT EXISTS (
    SELECT 1
    FROM sys.columns c
    INNER JOIN sys.tables t ON c.object_id = t.object_id
    WHERE t.name = N'GameProfiles'
      AND c.name = N'DisplayName'
      AND SCHEMA_NAME(t.schema_id) = N'dbo'
)
BEGIN
    ALTER TABLE [dbo].[GameProfiles] ADD [DisplayName] nvarchar(max) NULL;
END
GO

-- Keep EF migration history in sync (prevents duplicate AddColumn on next `dotnet ef database update`).
IF NOT EXISTS (
    SELECT 1 FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260327120000_GameProfileDisplayName'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260327120000_GameProfileDisplayName', N'9.0.0');
END
GO
