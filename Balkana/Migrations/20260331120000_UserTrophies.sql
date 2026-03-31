-- Run on SQL Server if `dotnet ef database update` is not available.
-- Adds UserTrophies (userId -> trophyId) join table for Balkana Awards and other user-awarded trophies.

IF OBJECT_ID(N'[dbo].[UserTrophies]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[UserTrophies] (
        [Id] int NOT NULL IDENTITY(1,1),
        [UserId] nvarchar(450) NOT NULL,
        [TrophyId] int NOT NULL,
        [DateAwarded] datetime2 NOT NULL CONSTRAINT [DF_UserTrophies_DateAwarded] DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_UserTrophies] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserTrophies_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserTrophies_Trophies_TrophyId] FOREIGN KEY ([TrophyId]) REFERENCES [dbo].[Trophies] ([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_UserTrophies_UserId] ON [dbo].[UserTrophies] ([UserId]);
    CREATE INDEX [IX_UserTrophies_TrophyId] ON [dbo].[UserTrophies] ([TrophyId]);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260331120000_UserTrophies'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260331120000_UserTrophies', N'9.0.0');
END
GO

