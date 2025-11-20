-- SQL script to manually create UserLinkedAccounts table
-- Run this if the migration doesn't apply automatically

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UserLinkedAccounts]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[UserLinkedAccounts] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [Type] nvarchar(50) NOT NULL,
        [Identifier] nvarchar(500) NOT NULL,
        CONSTRAINT [PK_UserLinkedAccounts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserLinkedAccounts_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_UserLinkedAccounts_UserId] ON [dbo].[UserLinkedAccounts] ([UserId]);
    
    CREATE UNIQUE INDEX [IX_UserLinkedAccounts_UserId_Type] ON [dbo].[UserLinkedAccounts] ([UserId], [Type]);
END

