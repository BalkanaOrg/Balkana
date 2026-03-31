-- Run on SQL Server if `dotnet ef database update` is not available.
-- UserVoting + ranked vote items for Balkana Awards categories.

IF OBJECT_ID(N'[dbo].[UserVoting]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[UserVoting] (
        [Id] int NOT NULL IDENTITY(1,1),
        [BalkanaAwardsId] int NOT NULL,
        [CategoryId] int NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_UserVoting_CreatedAt] DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_UserVoting] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserVoting_BalkanaAwards_BalkanaAwardsId] FOREIGN KEY ([BalkanaAwardsId]) REFERENCES [dbo].[BalkanaAwards] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserVoting_BalkanaAwardCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [dbo].[BalkanaAwardCategories] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserVoting_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_UserVoting_BalkanaAwardsId] ON [dbo].[UserVoting] ([BalkanaAwardsId]);
    CREATE INDEX [IX_UserVoting_CategoryId] ON [dbo].[UserVoting] ([CategoryId]);
    CREATE INDEX [IX_UserVoting_UserId] ON [dbo].[UserVoting] ([UserId]);
    CREATE UNIQUE INDEX [IX_UserVoting_UniqueBallot] ON [dbo].[UserVoting] ([BalkanaAwardsId], [CategoryId], [UserId]);
END
GO

IF OBJECT_ID(N'[dbo].[UserVotingItems]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[UserVotingItems] (
        [Id] int NOT NULL IDENTITY(1,1),
        [UserVotingId] int NOT NULL,
        [Rank] int NOT NULL,
        [PlayerId] int NULL,
        [TeamId] int NULL,
        [TournamentId] int NULL,
        [CandidateUserId] nvarchar(450) NULL,
        CONSTRAINT [PK_UserVotingItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserVotingItems_UserVoting_UserVotingId] FOREIGN KEY ([UserVotingId]) REFERENCES [dbo].[UserVoting] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserVotingItems_Players_PlayerId] FOREIGN KEY ([PlayerId]) REFERENCES [dbo].[Players] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_UserVotingItems_Teams_TeamId] FOREIGN KEY ([TeamId]) REFERENCES [dbo].[Teams] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_UserVotingItems_Tournaments_TournamentId] FOREIGN KEY ([TournamentId]) REFERENCES [dbo].[Tournaments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_UserVotingItems_AspNetUsers_CandidateUserId] FOREIGN KEY ([CandidateUserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [CK_UserVotingItems_ExactlyOneCandidate] CHECK (
            (CASE WHEN [PlayerId] IS NULL THEN 0 ELSE 1 END) +
            (CASE WHEN [TeamId] IS NULL THEN 0 ELSE 1 END) +
            (CASE WHEN [TournamentId] IS NULL THEN 0 ELSE 1 END) +
            (CASE WHEN [CandidateUserId] IS NULL THEN 0 ELSE 1 END)
            = 1
        )
    );

    CREATE INDEX [IX_UserVotingItems_UserVotingId] ON [dbo].[UserVotingItems] ([UserVotingId]);
    CREATE UNIQUE INDEX [IX_UserVotingItems_UniqueRank] ON [dbo].[UserVotingItems] ([UserVotingId], [Rank]);
    CREATE UNIQUE INDEX [IX_UserVotingItems_UniquePlayer] ON [dbo].[UserVotingItems] ([UserVotingId], [PlayerId]) WHERE [PlayerId] IS NOT NULL;
    CREATE UNIQUE INDEX [IX_UserVotingItems_UniqueTeam] ON [dbo].[UserVotingItems] ([UserVotingId], [TeamId]) WHERE [TeamId] IS NOT NULL;
    CREATE UNIQUE INDEX [IX_UserVotingItems_UniqueTournament] ON [dbo].[UserVotingItems] ([UserVotingId], [TournamentId]) WHERE [TournamentId] IS NOT NULL;
    CREATE UNIQUE INDEX [IX_UserVotingItems_UniqueUser] ON [dbo].[UserVotingItems] ([UserVotingId], [CandidateUserId]) WHERE [CandidateUserId] IS NOT NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260331122000_BalkanaAwardsVoting'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260331122000_BalkanaAwardsVoting', N'9.0.0');
END
GO

