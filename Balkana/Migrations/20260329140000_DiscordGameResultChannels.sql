-- Run on SQL Server if `dotnet ef database update` is not available.
-- Maps each game to a Discord channel for tournament results announcements.

IF OBJECT_ID(N'[dbo].[DiscordGameResultChannels]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[DiscordGameResultChannels] (
        [Id] int NOT NULL IDENTITY(1,1),
        [GameId] int NOT NULL,
        [DiscordChannelId] nvarchar(64) NOT NULL,
        [DisplayLabel] nvarchar(200) NULL,
        [IsActive] bit NOT NULL CONSTRAINT [DF_DiscordGameResultChannels_IsActive] DEFAULT 1,
        CONSTRAINT [PK_DiscordGameResultChannels] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DiscordGameResultChannels_Games_GameId] FOREIGN KEY ([GameId]) REFERENCES [dbo].[Games] ([Id]) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX [IX_DiscordGameResultChannels_GameId] ON [dbo].[DiscordGameResultChannels] ([GameId]);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260329140000_DiscordGameResultChannels'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260329140000_DiscordGameResultChannels', N'9.0.0');
END
GO
