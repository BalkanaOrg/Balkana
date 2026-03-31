-- Run on SQL Server if `dotnet ef database update` is not available.
-- Snapshot yearly results (e.g. CS/LoL Player of the Year #1-#10) to keep them stable even if stats change later.

IF OBJECT_ID(N'[dbo].[BalkanaAwardResults]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[BalkanaAwardResults] (
        [Id] int NOT NULL IDENTITY(1,1),
        [BalkanaAwardsId] int NOT NULL,
        [CategoryId] int NOT NULL,
        [Rank] int NOT NULL,
        [PlayerId] int NOT NULL,
        CONSTRAINT [PK_BalkanaAwardResults] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BalkanaAwardResults_BalkanaAwards_BalkanaAwardsId] FOREIGN KEY ([BalkanaAwardsId]) REFERENCES [dbo].[BalkanaAwards] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_BalkanaAwardResults_BalkanaAwardCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [dbo].[BalkanaAwardCategories] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_BalkanaAwardResults_Players_PlayerId] FOREIGN KEY ([PlayerId]) REFERENCES [dbo].[Players] ([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_BalkanaAwardResults_BalkanaAwardsId] ON [dbo].[BalkanaAwardResults] ([BalkanaAwardsId]);
    CREATE INDEX [IX_BalkanaAwardResults_CategoryId] ON [dbo].[BalkanaAwardResults] ([CategoryId]);
    CREATE UNIQUE INDEX [IX_BalkanaAwardResults_UniqueRank] ON [dbo].[BalkanaAwardResults] ([BalkanaAwardsId], [CategoryId], [Rank]);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260331123000_BalkanaAwardsResults'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260331123000_BalkanaAwardsResults', N'9.0.0');
END
GO

