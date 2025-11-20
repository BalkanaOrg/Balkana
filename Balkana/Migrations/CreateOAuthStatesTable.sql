-- SQL script to manually create OAuthStates table
-- Run this if the migration doesn't apply automatically

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[OAuthStates]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[OAuthStates] (
        [State] nvarchar(450) NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        [Provider] nvarchar(50) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [CodeVerifier] nvarchar(512) NULL,
        CONSTRAINT [PK_OAuthStates] PRIMARY KEY ([State])
    );

    CREATE INDEX [IX_OAuthStates_UserId_Provider] ON [dbo].[OAuthStates] ([UserId], [Provider]);
END

IF EXISTS (SELECT * FROM sys.columns WHERE Name = N'CodeVerifier' AND Object_ID = Object_ID(N'[dbo].[OAuthStates]')) = 0
BEGIN
    ALTER TABLE [dbo].[OAuthStates] ADD [CodeVerifier] nvarchar(512) NULL;
END

