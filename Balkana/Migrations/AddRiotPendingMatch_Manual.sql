-- Run this script if the RiotPendingMatches migration was not applied automatically.
-- Execute against your SQL Server database.

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RiotPendingMatches')
BEGIN
    CREATE TABLE [RiotPendingMatches] (
        [Id] int NOT NULL IDENTITY(1,1),
        [MatchId] nvarchar(100) NOT NULL,
        [TournamentCode] nvarchar(100) NULL,
        [RiotTournamentCodeId] int NULL,
        [RawPayload] nvarchar(max) NOT NULL,
        [Status] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ImportedAt] datetime2 NULL,
        [ImportedMatchDbId] int NULL,
        [SeriesId] int NULL,
        [ErrorMessage] nvarchar(500) NULL,
        CONSTRAINT [PK_RiotPendingMatches] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RiotPendingMatches_Matches_ImportedMatchDbId] FOREIGN KEY ([ImportedMatchDbId]) REFERENCES [Matches] ([Id]),
        CONSTRAINT [FK_RiotPendingMatches_RiotTournamentCodes_RiotTournamentCodeId] FOREIGN KEY ([RiotTournamentCodeId]) REFERENCES [RiotTournamentCodes] ([Id]),
        CONSTRAINT [FK_RiotPendingMatches_Series_SeriesId] FOREIGN KEY ([SeriesId]) REFERENCES [Series] ([Id])
    );

    CREATE INDEX [IX_RiotPendingMatches_ImportedMatchDbId] ON [RiotPendingMatches] ([ImportedMatchDbId]);
    CREATE INDEX [IX_RiotPendingMatches_MatchId] ON [RiotPendingMatches] ([MatchId]);
    CREATE INDEX [IX_RiotPendingMatches_RiotTournamentCodeId] ON [RiotPendingMatches] ([RiotTournamentCodeId]);
    CREATE INDEX [IX_RiotPendingMatches_SeriesId] ON [RiotPendingMatches] ([SeriesId]);
    CREATE INDEX [IX_RiotPendingMatches_Status_CreatedAt] ON [RiotPendingMatches] ([Status], [CreatedAt]);

    -- Record the migration so EF won't try to run it again
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260220000000_AddRiotPendingMatch', N'9.0.0');
END
GO
