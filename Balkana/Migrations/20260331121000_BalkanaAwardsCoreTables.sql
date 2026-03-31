-- Run on SQL Server if `dotnet ef database update` is not available.
-- Core Balkana Awards tables: yearly event, categories, and eligibility lists.

IF OBJECT_ID(N'[dbo].[BalkanaAwards]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[BalkanaAwards] (
        [Id] int NOT NULL IDENTITY(1,1),
        [Year] int NOT NULL,
        [EventDate] datetime2 NOT NULL,
        [VotingOpensAt] datetime2 NULL,
        [VotingClosesAt] datetime2 NULL,
        CONSTRAINT [PK_BalkanaAwards] PRIMARY KEY ([Id])
    );
    CREATE UNIQUE INDEX [IX_BalkanaAwards_Year] ON [dbo].[BalkanaAwards] ([Year]);
END
GO

IF OBJECT_ID(N'[dbo].[BalkanaAwardCategories]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[BalkanaAwardCategories] (
        [Id] int NOT NULL IDENTITY(1,1),
        [Key] nvarchar(100) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [TargetType] nvarchar(20) NOT NULL, -- Player|Team|Tournament|User
        [IsCommunityVoted] bit NOT NULL CONSTRAINT [DF_BalkanaAwardCategories_IsCommunityVoted] DEFAULT 0,
        [IsRanked] bit NOT NULL CONSTRAINT [DF_BalkanaAwardCategories_IsRanked] DEFAULT 0,
        [MaxRanks] int NOT NULL CONSTRAINT [DF_BalkanaAwardCategories_MaxRanks] DEFAULT 1,
        [SortOrder] int NOT NULL CONSTRAINT [DF_BalkanaAwardCategories_SortOrder] DEFAULT 0,
        CONSTRAINT [PK_BalkanaAwardCategories] PRIMARY KEY ([Id])
    );
    CREATE UNIQUE INDEX [IX_BalkanaAwardCategories_Key] ON [dbo].[BalkanaAwardCategories] ([Key]);
END
GO

IF OBJECT_ID(N'[dbo].[BalkanaAwardEligibilityPlayers]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[BalkanaAwardEligibilityPlayers] (
        [BalkanaAwardsId] int NOT NULL,
        [CategoryId] int NOT NULL,
        [PlayerId] int NOT NULL,
        CONSTRAINT [PK_BalkanaAwardEligibilityPlayers] PRIMARY KEY ([BalkanaAwardsId], [CategoryId], [PlayerId]),
        CONSTRAINT [FK_BalkanaAwardEligibilityPlayers_BalkanaAwards_BalkanaAwardsId] FOREIGN KEY ([BalkanaAwardsId]) REFERENCES [dbo].[BalkanaAwards] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_BalkanaAwardEligibilityPlayers_BalkanaAwardCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [dbo].[BalkanaAwardCategories] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_BalkanaAwardEligibilityPlayers_Players_PlayerId] FOREIGN KEY ([PlayerId]) REFERENCES [dbo].[Players] ([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_BalkanaAwardEligibilityPlayers_CategoryId] ON [dbo].[BalkanaAwardEligibilityPlayers] ([CategoryId]);
    CREATE INDEX [IX_BalkanaAwardEligibilityPlayers_PlayerId] ON [dbo].[BalkanaAwardEligibilityPlayers] ([PlayerId]);
END
GO

-- Seed categories (idempotent by Key)
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = N'BalkanaAwardCategories' AND SCHEMA_NAME(schema_id) = N'dbo')
BEGIN
    IF NOT EXISTS (SELECT 1 FROM [dbo].[BalkanaAwardCategories] WHERE [Key] = N'cs_poty')
        INSERT INTO [dbo].[BalkanaAwardCategories] ([Key],[Name],[TargetType],[IsCommunityVoted],[IsRanked],[MaxRanks],[SortOrder])
        VALUES (N'cs_poty', N'Counter-Strike Player of the Year (#1-#10)', N'Player', 0, 1, 10, 10);

    IF NOT EXISTS (SELECT 1 FROM [dbo].[BalkanaAwardCategories] WHERE [Key] = N'lol_poty')
        INSERT INTO [dbo].[BalkanaAwardCategories] ([Key],[Name],[TargetType],[IsCommunityVoted],[IsRanked],[MaxRanks],[SortOrder])
        VALUES (N'lol_poty', N'League of Legends Player of the Year (#1-#10)', N'Player', 0, 1, 10, 20);

    IF NOT EXISTS (SELECT 1 FROM [dbo].[BalkanaAwardCategories] WHERE [Key] = N'entry_frg')
        INSERT INTO [dbo].[BalkanaAwardCategories] ([Key],[Name],[TargetType],[IsCommunityVoted],[IsRanked],[MaxRanks],[SortOrder])
        VALUES (N'entry_frg', N'Entry Fragger of the Year', N'Player', 1, 0, 1, 30);

    IF NOT EXISTS (SELECT 1 FROM [dbo].[BalkanaAwardCategories] WHERE [Key] = N'awper')
        INSERT INTO [dbo].[BalkanaAwardCategories] ([Key],[Name],[TargetType],[IsCommunityVoted],[IsRanked],[MaxRanks],[SortOrder])
        VALUES (N'awper', N'AWPer of the Year', N'Player', 1, 0, 1, 40);

    IF NOT EXISTS (SELECT 1 FROM [dbo].[BalkanaAwardCategories] WHERE [Key] = N'igl')
        INSERT INTO [dbo].[BalkanaAwardCategories] ([Key],[Name],[TargetType],[IsCommunityVoted],[IsRanked],[MaxRanks],[SortOrder])
        VALUES (N'igl', N'IGL of the Year', N'Player', 1, 0, 1, 50);

    IF NOT EXISTS (SELECT 1 FROM [dbo].[BalkanaAwardCategories] WHERE [Key] = N'team_of_year')
        INSERT INTO [dbo].[BalkanaAwardCategories] ([Key],[Name],[TargetType],[IsCommunityVoted],[IsRanked],[MaxRanks],[SortOrder])
        VALUES (N'team_of_year', N'Team of the Year', N'Team', 1, 0, 1, 60);

    IF NOT EXISTS (SELECT 1 FROM [dbo].[BalkanaAwardCategories] WHERE [Key] = N'tournament_of_year')
        INSERT INTO [dbo].[BalkanaAwardCategories] ([Key],[Name],[TargetType],[IsCommunityVoted],[IsRanked],[MaxRanks],[SortOrder])
        VALUES (N'tournament_of_year', N'Tournament of the Year', N'Tournament', 1, 0, 1, 70);

    IF NOT EXISTS (SELECT 1 FROM [dbo].[BalkanaAwardCategories] WHERE [Key] = N'content_creator')
        INSERT INTO [dbo].[BalkanaAwardCategories] ([Key],[Name],[TargetType],[IsCommunityVoted],[IsRanked],[MaxRanks],[SortOrder])
        VALUES (N'content_creator', N'Content Creator of the Year', N'User', 1, 0, 1, 80);

    IF NOT EXISTS (SELECT 1 FROM [dbo].[BalkanaAwardCategories] WHERE [Key] = N'streamer')
        INSERT INTO [dbo].[BalkanaAwardCategories] ([Key],[Name],[TargetType],[IsCommunityVoted],[IsRanked],[MaxRanks],[SortOrder])
        VALUES (N'streamer', N'Streamer of the Year', N'User', 1, 0, 1, 90);

    IF NOT EXISTS (SELECT 1 FROM [dbo].[BalkanaAwardCategories] WHERE [Key] = N'pbp_caster')
        INSERT INTO [dbo].[BalkanaAwardCategories] ([Key],[Name],[TargetType],[IsCommunityVoted],[IsRanked],[MaxRanks],[SortOrder])
        VALUES (N'pbp_caster', N'Play-by-Play Caster of the Year', N'User', 1, 0, 1, 100);

    IF NOT EXISTS (SELECT 1 FROM [dbo].[BalkanaAwardCategories] WHERE [Key] = N'color_caster')
        INSERT INTO [dbo].[BalkanaAwardCategories] ([Key],[Name],[TargetType],[IsCommunityVoted],[IsRanked],[MaxRanks],[SortOrder])
        VALUES (N'color_caster', N'Color Caster of the Year', N'User', 1, 0, 1, 110);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260331121000_BalkanaAwardsCoreTables'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260331121000_BalkanaAwardsCoreTables', N'9.0.0');
END
GO

