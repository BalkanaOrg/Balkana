-- Run on SQL Server if `dotnet ef database update` is not available.
-- Public listing flag for tournaments (1 = listed for non-staff / anonymous).

IF NOT EXISTS (
    SELECT 1
    FROM sys.columns c
    INNER JOIN sys.tables t ON c.object_id = t.object_id
    WHERE t.name = N'Tournaments'
      AND c.name = N'IsPublic'
      AND SCHEMA_NAME(t.schema_id) = N'dbo'
)
BEGIN
    ALTER TABLE [dbo].[Tournaments]
    ADD [IsPublic] bit NOT NULL CONSTRAINT [DF_Tournaments_IsPublic] DEFAULT 1;
END
GO
