-- Run on SQL Server if `dotnet ef database update` is not available.
-- Prize pool amount awarded per team row on TournamentPlacements.

IF NOT EXISTS (
    SELECT 1
    FROM sys.columns c
    INNER JOIN sys.tables t ON c.object_id = t.object_id
    WHERE t.name = N'TournamentPlacements'
      AND c.name = N'PrizePoolAwarded'
      AND SCHEMA_NAME(t.schema_id) = N'dbo'
)
BEGIN
    ALTER TABLE [dbo].[TournamentPlacements]
    ADD [PrizePoolAwarded] decimal(18,2) NOT NULL CONSTRAINT [DF_TournamentPlacements_PrizePoolAwarded] DEFAULT 0;
END
GO
