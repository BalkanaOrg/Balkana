-- Run on SQL Server if `dotnet ef database update` is not available.
-- Player per-tournament points + organisation slice on placements.

IF NOT EXISTS (
    SELECT 1
    FROM sys.columns c
    INNER JOIN sys.tables t ON c.object_id = t.object_id
    WHERE t.name = N'TournamentPlacements'
      AND c.name = N'OrganisationPointsAwarded'
      AND SCHEMA_NAME(t.schema_id) = N'dbo'
)
BEGIN
    ALTER TABLE [dbo].[TournamentPlacements]
    ADD [OrganisationPointsAwarded] int NOT NULL CONSTRAINT [DF_TournamentPlacements_OrganisationPointsAwarded] DEFAULT 0;
END
GO

IF OBJECT_ID(N'[dbo].[PlayerPoints]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PlayerPoints] (
        [Id] int NOT NULL IDENTITY(1,1),
        [PlayerId] int NOT NULL,
        [PointsAwarded] int NOT NULL,
        [TournamentId] int NOT NULL,
        CONSTRAINT [PK_PlayerPoints] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PlayerPoints_Players_PlayerId] FOREIGN KEY ([PlayerId]) REFERENCES [dbo].[Players] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_PlayerPoints_Tournaments_TournamentId] FOREIGN KEY ([TournamentId]) REFERENCES [dbo].[Tournaments] ([Id]) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX [IX_PlayerPoints_PlayerId_TournamentId] ON [dbo].[PlayerPoints] ([PlayerId], [TournamentId]);
    CREATE INDEX [IX_PlayerPoints_TournamentId] ON [dbo].[PlayerPoints] ([TournamentId]);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260329120000_PlayerPoints'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260329120000_PlayerPoints', N'9.0.0');
END
GO
