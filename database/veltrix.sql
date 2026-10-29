/*
 * VELTRIX database: schema + seed in ONE script (SQL Server 2019+).
 *
 * Part 1 (generated): every EF Core migration, idempotent
 *   dotnet ef migrations script --idempotent --project Veltrix.Infrastructure --startup-project Veltrix.API
 *   30 tables, CHECK constraints, unique indexes, and the reference data from HasData:
 *   the "crash" game, system settings, 10 levels, 7 achievements, 2 content pages.
 * Part 2 (hand-written, at the end): the four test accounts, one per role, exactly as
 *   DatabaseInitializer / User.Create make them (wallet with the welcome ledger row, limits, preferences, stats).
 *
 * Run it on an EMPTY database (or an existing VELTRIX database: every step checks first, so it can run twice):
 *   sqlcmd -S localhost -E -Q "CREATE DATABASE Veltrix"
 *   sqlcmd -S localhost -E -d Veltrix -i database/veltrix.sql
 * In SSMS: create the database, open this file, choose the database, Execute.
 *
 * Not included: the 15 demo players with 30 days of play. They are played by the real game code
 * (DemoDataSeeder) on the first Development start, so their rounds have real seeds and hashes.
 *
 * Virtual VLX points only. No real money anywhere.
 */

-- Filtered unique indexes need these (SSMS sets them by default, sqlcmd does not)
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024172129_InitialCreate'
)
BEGIN
    CREATE TABLE [Games] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Slug] nvarchar(50) NOT NULL,
        [Description] nvarchar(2000) NULL,
        [Type] nvarchar(20) NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [MinBet] decimal(18,2) NOT NULL,
        [MaxBet] decimal(18,2) NOT NULL,
        [BettingDurationSec] int NOT NULL,
        [PauseBetweenRoundsSec] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Games] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Games_BettingDuration] CHECK ([BettingDurationSec] BETWEEN 3 AND 120),
        CONSTRAINT [CK_Games_MaxBet_GteMinBet] CHECK ([MaxBet] >= [MinBet]),
        CONSTRAINT [CK_Games_MinBet_Positive] CHECK ([MinBet] > 0),
        CONSTRAINT [CK_Games_Pause] CHECK ([PauseBetweenRoundsSec] BETWEEN 0 AND 120)
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024172129_InitialCreate'
)
BEGIN
    CREATE TABLE [SystemSettings] (
        [Key] nvarchar(100) NOT NULL,
        [Value] nvarchar(500) NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_SystemSettings] PRIMARY KEY ([Key])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024172129_InitialCreate'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] int NOT NULL IDENTITY,
        [Email] nvarchar(256) NOT NULL,
        [Nickname] nvarchar(20) NOT NULL,
        [PasswordHash] nvarchar(100) NOT NULL,
        [Role] nvarchar(20) NOT NULL,
        [AvatarUrl] nvarchar(500) NULL,
        [DateOfBirth] date NOT NULL,
        [IsBlocked] bit NOT NULL,
        [FailedLoginCount] int NOT NULL,
        [LockoutUntil] datetime2 NULL,
        [LastLoginAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [PasswordResetCodeHash] nvarchar(100) NULL,
        [PasswordResetCodeExpiresAt] datetime2 NULL,
        [PasswordResetAttempts] int NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024172129_InitialCreate'
)
BEGIN
    CREATE TABLE [GameRounds] (
        [Id] int NOT NULL IDENTITY,
        [GameId] int NOT NULL,
        [RoundNumber] bigint NOT NULL,
        [State] nvarchar(20) NOT NULL,
        [ServerSeed] nvarchar(64) NOT NULL,
        [ServerSeedHash] nvarchar(64) NOT NULL,
        [CrashPoint] decimal(10,2) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [BettingEndsAt] datetime2 NULL,
        [StartedAt] datetime2 NULL,
        [CrashedAt] datetime2 NULL,
        [EndedAt] datetime2 NULL,
        CONSTRAINT [PK_GameRounds] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_GameRounds_CrashPoint] CHECK ([CrashPoint] >= 1),
        CONSTRAINT [FK_GameRounds_Games_GameId] FOREIGN KEY ([GameId]) REFERENCES [Games] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024172129_InitialCreate'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [Id] int NOT NULL IDENTITY,
        [ActorUserId] int NULL,
        [Action] nvarchar(100) NOT NULL,
        [EntityType] nvarchar(100) NOT NULL,
        [EntityId] nvarchar(100) NOT NULL,
        [OldValue] nvarchar(max) NULL,
        [NewValue] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AuditLogs_Users_ActorUserId] FOREIGN KEY ([ActorUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024172129_InitialCreate'
)
BEGIN
    CREATE TABLE [Notifications] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [Type] nvarchar(50) NOT NULL,
        [Title] nvarchar(150) NOT NULL,
        [Message] nvarchar(2000) NOT NULL,
        [IsRead] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Notifications_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024172129_InitialCreate'
)
BEGIN
    CREATE TABLE [PlayerLimits] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [DailyBetLimit] decimal(18,2) NULL,
        [SessionReminderMin] int NOT NULL,
        [SelfExcludedUntil] datetime2 NULL,
        CONSTRAINT [PK_PlayerLimits] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_PlayerLimits_DailyBetLimit] CHECK ([DailyBetLimit] IS NULL OR [DailyBetLimit] > 0),
        CONSTRAINT [CK_PlayerLimits_SessionReminder] CHECK ([SessionReminderMin] BETWEEN 0 AND 600),
        CONSTRAINT [FK_PlayerLimits_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024172129_InitialCreate'
)
BEGIN
    CREATE TABLE [RefreshTokens] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [TokenHash] nchar(64) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [RevokedAt] datetime2 NULL,
        CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024172129_InitialCreate'
)
BEGIN
    CREATE TABLE [Wallets] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [Balance] decimal(18,2) NOT NULL,
        [LastDailyBonusAt] datetime2 NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Wallets] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Wallets_Balance_NonNegative] CHECK ([Balance] >= 0),
        CONSTRAINT [FK_Wallets_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024172129_InitialCreate'
)
BEGIN
    CREATE TABLE [Bets] (
        [Id] int NOT NULL IDENTITY,
        [RoundId] int NOT NULL,
        [UserId] int NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Slot] int NOT NULL,
        [AutoCashOutAt] decimal(10,2) NULL,
        [CashOutMultiplier] decimal(10,2) NULL,
        [Payout] decimal(18,2) NULL,
        [Status] nvarchar(20) NOT NULL,
        [PlacedAt] datetime2 NOT NULL,
        [CashedOutAt] datetime2 NULL,
        CONSTRAINT [PK_Bets] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Bets_Amount_Positive] CHECK ([Amount] > 0),
        CONSTRAINT [CK_Bets_AutoCashOut] CHECK ([AutoCashOutAt] IS NULL OR [AutoCashOutAt] >= 1.01),
        CONSTRAINT [CK_Bets_Slot] CHECK ([Slot] IN (1, 2)),
        CONSTRAINT [FK_Bets_GameRounds_RoundId] FOREIGN KEY ([RoundId]) REFERENCES [GameRounds] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Bets_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024172129_InitialCreate'
)
BEGIN
    CREATE TABLE [WalletTransactions] (
        [Id] int NOT NULL IDENTITY,
        [WalletId] int NOT NULL,
        [Type] nvarchar(20) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [BalanceBefore] decimal(18,2) NOT NULL,
        [BalanceAfter] decimal(18,2) NOT NULL,
        [BetId] int NULL,
        [IdempotencyKey] nvarchar(100) NOT NULL,
        [Reason] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_WalletTransactions] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_WalletTransactions_Balance_Math] CHECK ([BalanceAfter] = [BalanceBefore] + [Amount]),
        CONSTRAINT [CK_WalletTransactions_BalanceAfter_NonNegative] CHECK ([BalanceAfter] >= 0),
        CONSTRAINT [FK_WalletTransactions_Bets_BetId] FOREIGN KEY ([BetId]) REFERENCES [Bets] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WalletTransactions_Wallets_WalletId] FOREIGN KEY ([WalletId]) REFERENCES [Wallets] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024172129_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'BettingDurationSec', N'CreatedAt', N'Description', N'MaxBet', N'MinBet', N'Name', N'PauseBetweenRoundsSec', N'Slug', N'Status', N'Type', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[Games]'))
        SET IDENTITY_INSERT [Games] ON;
    EXEC(N'INSERT INTO [Games] ([Id], [BettingDurationSec], [CreatedAt], [Description], [MaxBet], [MinBet], [Name], [PauseBetweenRoundsSec], [Slug], [Status], [Type], [UpdatedAt])
    VALUES (1, 10, ''2026-01-01T00:00:00.0000000Z'', N''The multiplier grows from 1.00x. Cash out before it crashes!'', 10000.0, 1.0, N''Crash'', 5, N''crash'', N''Active'', N''Crash'', NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'BettingDurationSec', N'CreatedAt', N'Description', N'MaxBet', N'MinBet', N'Name', N'PauseBetweenRoundsSec', N'Slug', N'Status', N'Type', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[Games]'))
        SET IDENTITY_INSERT [Games] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024172129_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Key', N'UpdatedAt', N'Value') AND [object_id] = OBJECT_ID(N'[SystemSettings]'))
        SET IDENTITY_INSERT [SystemSettings] ON;
    EXEC(N'INSERT INTO [SystemSettings] ([Key], [UpdatedAt], [Value])
    VALUES (N''Wallet.DailyBonusAmount'', ''2026-01-01T00:00:00.0000000Z'', N''100''),
    (N''Wallet.StartingBalance'', ''2026-01-01T00:00:00.0000000Z'', N''1000'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Key', N'UpdatedAt', N'Value') AND [object_id] = OBJECT_ID(N'[SystemSettings]'))
        SET IDENTITY_INSERT [SystemSettings] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024172129_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_ActorUserId] ON [AuditLogs] ([ActorUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024172129_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_CreatedAt] ON [AuditLogs] ([CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024172129_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Bets_RoundId_UserId_Slot] ON [Bets] ([RoundId], [UserId], [Slot]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024172129_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Bets_UserId_PlacedAt] ON [Bets] ([UserId], [PlacedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024172129_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_GameRounds_GameId_RoundNumber] ON [GameRounds] ([GameId], [RoundNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024172129_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_GameRounds_State] ON [GameRounds] ([State]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024172129_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Games_Slug] ON [Games] ([Slug]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024172129_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Notifications_UserId_IsRead] ON [Notifications] ([UserId], [IsRead]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024172129_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PlayerLimits_UserId] ON [PlayerLimits] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024172129_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RefreshTokens_TokenHash] ON [RefreshTokens] ([TokenHash]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024172129_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RefreshTokens_UserId] ON [RefreshTokens] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024172129_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024172129_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Nickname] ON [Users] ([Nickname]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024172129_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Wallets_UserId] ON [Wallets] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024172129_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_WalletTransactions_BetId] ON [WalletTransactions] ([BetId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024172129_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_WalletTransactions_IdempotencyKey] ON [WalletTransactions] ([IdempotencyKey]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024172129_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_WalletTransactions_WalletId_CreatedAt] ON [WalletTransactions] ([WalletId], [CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024172129_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261024172129_InitialCreate', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024173209_AddExtendedModels'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'PasswordResetAttempts');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [Users] DROP COLUMN [PasswordResetAttempts];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024173209_AddExtendedModels'
)
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'PasswordResetCodeExpiresAt');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [Users] DROP COLUMN [PasswordResetCodeExpiresAt];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024173209_AddExtendedModels'
)
BEGIN
    DECLARE @var2 sysname;
    SELECT @var2 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'PasswordResetCodeHash');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var2 + '];');
    ALTER TABLE [Users] DROP COLUMN [PasswordResetCodeHash];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024173209_AddExtendedModels'
)
BEGIN
    ALTER TABLE [Users] ADD [IsEmailVerified] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024173209_AddExtendedModels'
)
BEGIN
    CREATE TABLE [Achievements] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(50) NOT NULL,
        [Title] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NOT NULL,
        [Icon] nvarchar(50) NOT NULL,
        [XpReward] int NOT NULL,
        CONSTRAINT [PK_Achievements] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Achievements_XpReward] CHECK ([XpReward] >= 0)
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024173209_AddExtendedModels'
)
BEGIN
    CREATE TABLE [AutoBetConfigs] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [GameId] int NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [AutoCashOutAt] decimal(10,2) NOT NULL,
        [TotalRounds] int NOT NULL,
        [RoundsPlayed] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [StoppedAt] datetime2 NULL,
        CONSTRAINT [PK_AutoBetConfigs] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_AutoBetConfigs_Amount] CHECK ([Amount] > 0),
        CONSTRAINT [CK_AutoBetConfigs_CashOut] CHECK ([AutoCashOutAt] >= 1.01),
        CONSTRAINT [CK_AutoBetConfigs_Rounds] CHECK ([TotalRounds] BETWEEN 1 AND 1000 AND [RoundsPlayed] BETWEEN 0 AND [TotalRounds]),
        CONSTRAINT [FK_AutoBetConfigs_Games_GameId] FOREIGN KEY ([GameId]) REFERENCES [Games] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AutoBetConfigs_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024173209_AddExtendedModels'
)
BEGIN
    CREATE TABLE [BalanceAdjustments] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [AdminUserId] int NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Reason] nvarchar(500) NOT NULL,
        [WalletTransactionId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_BalanceAdjustments] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_BalanceAdjustments_Amount] CHECK ([Amount] <> 0),
        CONSTRAINT [CK_BalanceAdjustments_Reason] CHECK (LEN([Reason]) >= 5),
        CONSTRAINT [FK_BalanceAdjustments_Users_AdminUserId] FOREIGN KEY ([AdminUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_BalanceAdjustments_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_BalanceAdjustments_WalletTransactions_WalletTransactionId] FOREIGN KEY ([WalletTransactionId]) REFERENCES [WalletTransactions] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024173209_AddExtendedModels'
)
BEGIN
    CREATE TABLE [ChatMessages] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [Text] nvarchar(300) NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedByUserId] int NULL,
        [SentAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ChatMessages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ChatMessages_Users_DeletedByUserId] FOREIGN KEY ([DeletedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ChatMessages_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024173209_AddExtendedModels'
)
BEGIN
    CREATE TABLE [ChatMutes] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [MutedByUserId] int NOT NULL,
        [Reason] nvarchar(300) NULL,
        [MutedUntil] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ChatMutes] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ChatMutes_Until] CHECK ([MutedUntil] > [CreatedAt]),
        CONSTRAINT [FK_ChatMutes_Users_MutedByUserId] FOREIGN KEY ([MutedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ChatMutes_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024173209_AddExtendedModels'
)
BEGIN
    CREATE TABLE [DailyStatistics] (
        [Date] date NOT NULL,
        [ActiveUsers] int NOT NULL,
        [NewUsers] int NOT NULL,
        [RoundsPlayed] int NOT NULL,
        [BetsCount] int NOT NULL,
        [BetsVolume] decimal(18,2) NOT NULL,
        [PayoutVolume] decimal(18,2) NOT NULL,
        [AverageCrashPoint] decimal(10,2) NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_DailyStatistics] PRIMARY KEY ([Date]),
        CONSTRAINT [CK_DailyStatistics_NonNegative] CHECK ([ActiveUsers] >= 0 AND [NewUsers] >= 0 AND [RoundsPlayed] >= 0 AND [BetsCount] >= 0 AND [BetsVolume] >= 0 AND [PayoutVolume] >= 0)
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024173209_AddExtendedModels'
)
BEGIN
    CREATE TABLE [EmailVerificationCodes] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [CodeHash] nvarchar(100) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [UsedAt] datetime2 NULL,
        CONSTRAINT [PK_EmailVerificationCodes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EmailVerificationCodes_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024173209_AddExtendedModels'
)
BEGIN
    CREATE TABLE [GameRoundEvents] (
        [Id] int NOT NULL IDENTITY,
        [RoundId] int NOT NULL,
        [FromState] nvarchar(20) NULL,
        [ToState] nvarchar(20) NOT NULL,
        [Note] nvarchar(500) NULL,
        [OccurredAt] datetime2 NOT NULL,
        CONSTRAINT [PK_GameRoundEvents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_GameRoundEvents_GameRounds_RoundId] FOREIGN KEY ([RoundId]) REFERENCES [GameRounds] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024173209_AddExtendedModels'
)
BEGIN
    CREATE TABLE [Levels] (
        [Number] int NOT NULL,
        [Title] nvarchar(50) NOT NULL,
        [XpRequired] int NOT NULL,
        CONSTRAINT [PK_Levels] PRIMARY KEY ([Number]),
        CONSTRAINT [CK_Levels_Xp] CHECK ([XpRequired] >= 0)
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024173209_AddExtendedModels'
)
BEGIN
    CREATE TABLE [LoginAttempts] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NULL,
        [Email] nvarchar(256) NOT NULL,
        [IpAddress] nvarchar(45) NULL,
        [UserAgent] nvarchar(512) NULL,
        [Succeeded] bit NOT NULL,
        [FailureReason] nvarchar(50) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_LoginAttempts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_LoginAttempts_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024173209_AddExtendedModels'
)
BEGIN
    CREATE TABLE [PasswordResetCodes] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [CodeHash] nvarchar(100) NOT NULL,
        [Attempts] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [UsedAt] datetime2 NULL,
        CONSTRAINT [PK_PasswordResetCodes] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_PasswordResetCodes_Attempts] CHECK ([Attempts] BETWEEN 0 AND 5),
        CONSTRAINT [FK_PasswordResetCodes_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024173209_AddExtendedModels'
)
BEGIN
    CREATE TABLE [PlaySessions] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [StartedAt] datetime2 NOT NULL,
        [LastActivityAt] datetime2 NOT NULL,
        [EndedAt] datetime2 NULL,
        [RemindersSent] int NOT NULL,
        CONSTRAINT [PK_PlaySessions] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_PlaySessions_Activity] CHECK ([LastActivityAt] >= [StartedAt]),
        CONSTRAINT [FK_PlaySessions_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024173209_AddExtendedModels'
)
BEGIN
    CREATE TABLE [RoundCancellations] (
        [Id] int NOT NULL IDENTITY,
        [RoundId] int NOT NULL,
        [CancelledByUserId] int NULL,
        [Reason] nvarchar(500) NOT NULL,
        [RefundedBetsCount] int NOT NULL,
        [RefundedTotal] decimal(18,2) NOT NULL,
        [CancelledAt] datetime2 NOT NULL,
        CONSTRAINT [PK_RoundCancellations] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_RoundCancellations_Refunds] CHECK ([RefundedBetsCount] >= 0 AND [RefundedTotal] >= 0),
        CONSTRAINT [FK_RoundCancellations_GameRounds_RoundId] FOREIGN KEY ([RoundId]) REFERENCES [GameRounds] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RoundCancellations_Users_CancelledByUserId] FOREIGN KEY ([CancelledByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024173209_AddExtendedModels'
)
BEGIN
    CREATE TABLE [SelfExclusions] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [Period] nvarchar(20) NOT NULL,
        [StartsAt] datetime2 NOT NULL,
        [EndsAt] datetime2 NOT NULL,
        CONSTRAINT [PK_SelfExclusions] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_SelfExclusions_Period] CHECK ([EndsAt] > [StartsAt]),
        CONSTRAINT [FK_SelfExclusions_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024173209_AddExtendedModels'
)
BEGIN
    CREATE TABLE [StaticPages] (
        [Id] int NOT NULL IDENTITY,
        [Slug] nvarchar(50) NOT NULL,
        [Language] nvarchar(5) NOT NULL,
        [Title] nvarchar(150) NOT NULL,
        [Content] nvarchar(max) NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_StaticPages] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_StaticPages_Language] CHECK ([Language] IN ('en', 'ka'))
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024173209_AddExtendedModels'
)
BEGIN
    CREATE TABLE [UserPreferences] (
        [UserId] int NOT NULL,
        [Theme] nvarchar(20) NOT NULL,
        [SoundEnabled] bit NOT NULL,
        [AnimationsEnabled] bit NOT NULL,
        [Language] nvarchar(5) NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_UserPreferences] PRIMARY KEY ([UserId]),
        CONSTRAINT [CK_UserPreferences_Language] CHECK ([Language] IN ('en', 'ka')),
        CONSTRAINT [FK_UserPreferences_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024173209_AddExtendedModels'
)
BEGIN
    CREATE TABLE [UserTwoFactors] (
        [UserId] int NOT NULL,
        [SecretKey] nvarchar(200) NOT NULL,
        [IsEnabled] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [EnabledAt] datetime2 NULL,
        CONSTRAINT [PK_UserTwoFactors] PRIMARY KEY ([UserId]),
        CONSTRAINT [FK_UserTwoFactors_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024173209_AddExtendedModels'
)
BEGIN
    CREATE TABLE [UserAchievements] (
        [UserId] int NOT NULL,
        [AchievementId] int NOT NULL,
        [UnlockedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_UserAchievements] PRIMARY KEY ([UserId], [AchievementId]),
        CONSTRAINT [FK_UserAchievements_Achievements_AchievementId] FOREIGN KEY ([AchievementId]) REFERENCES [Achievements] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_UserAchievements_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024173209_AddExtendedModels'
)
BEGIN
    CREATE TABLE [PlayerStats] (
        [UserId] int NOT NULL,
        [TotalBets] int NOT NULL,
        [CashOutCount] int NOT NULL,
        [TotalWagered] decimal(18,2) NOT NULL,
        [TotalPayout] decimal(18,2) NOT NULL,
        [BiggestWin] decimal(18,2) NOT NULL,
        [BiggestMultiplier] decimal(10,2) NOT NULL,
        [Xp] int NOT NULL,
        [LevelNumber] int NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_PlayerStats] PRIMARY KEY ([UserId]),
        CONSTRAINT [CK_PlayerStats_CashOuts] CHECK ([CashOutCount] <= [TotalBets]),
        CONSTRAINT [CK_PlayerStats_NonNegative] CHECK ([TotalBets] >= 0 AND [CashOutCount] >= 0 AND [TotalWagered] >= 0 AND [TotalPayout] >= 0 AND [Xp] >= 0),
        CONSTRAINT [FK_PlayerStats_Levels_LevelNumber] FOREIGN KEY ([LevelNumber]) REFERENCES [Levels] ([Number]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PlayerStats_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024173209_AddExtendedModels'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'Description', N'Icon', N'Title', N'XpReward') AND [object_id] = OBJECT_ID(N'[Achievements]'))
        SET IDENTITY_INSERT [Achievements] ON;
    EXEC(N'INSERT INTO [Achievements] ([Id], [Code], [Description], [Icon], [Title], [XpReward])
    VALUES (1, N''FIRST_BET'', N''Place your first bet.'', N''rocket'', N''First bet'', 10),
    (2, N''FIRST_CASHOUT'', N''Cash out before the crash for the first time.'', N''coins'', N''First cash-out'', 20),
    (3, N''CASHOUT_10X'', N''Cash out at 10.00x or higher.'', N''bolt'', N''To the moon'', 100),
    (4, N''BETS_100'', N''Place 100 bets.'', N''trophy'', N''Regular'', 50),
    (5, N''BIG_WIN_1000'', N''Win 1000 VLX or more in a single bet.'', N''star'', N''Big win'', 100)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'Description', N'Icon', N'Title', N'XpReward') AND [object_id] = OBJECT_ID(N'[Achievements]'))
        SET IDENTITY_INSERT [Achievements] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024173209_AddExtendedModels'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Number', N'Title', N'XpRequired') AND [object_id] = OBJECT_ID(N'[Levels]'))
        SET IDENTITY_INSERT [Levels] ON;
    EXEC(N'INSERT INTO [Levels] ([Number], [Title], [XpRequired])
    VALUES (1, N''Rookie'', 0),
    (2, N''Spotter'', 100),
    (3, N''Climber'', 250),
    (4, N''Pilot'', 500),
    (5, N''Navigator'', 1000),
    (6, N''Ace'', 2000),
    (7, N''Veteran'', 3500),
    (8, N''Elite'', 5500),
    (9, N''Legend'', 8000),
    (10, N''Mythic'', 12000)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Number', N'Title', N'XpRequired') AND [object_id] = OBJECT_ID(N'[Levels]'))
        SET IDENTITY_INSERT [Levels] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024173209_AddExtendedModels'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Content', N'Language', N'Slug', N'Title', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[StaticPages]'))
        SET IDENTITY_INSERT [StaticPages] ON;
    EXEC(N'INSERT INTO [StaticPages] ([Id], [Content], [Language], [Slug], [Title], [UpdatedAt])
    VALUES (1, CONCAT(CAST(N''1. During **betting** you place a bet with virtual VLX points.'' AS nvarchar(max)), nchar(10), N''2. The round starts and the multiplier grows from **1.00x**.'', nchar(10), N''3. Press **Cash out** at any moment: you get *bet × current multiplier*.'', nchar(10), N''4. If the round **crashes** before you cash out, the bet is lost.'', nchar(10), N''5. The crash point is decided **before** the round starts. Check it on the Provably Fair page.'', nchar(10), nchar(10), N''VLX points have no monetary value and cannot be bought or withdrawn.''), N''en'', N''rules'', N''Crash game rules'', ''2026-01-01T00:00:00.0000000Z''),
    (2, CONCAT(CAST(N''Every round''''s crash point is random and independent of previous rounds.'' AS nvarchar(max)), nchar(10), N''A long series of low crashes does **not** make a high crash more likely.'', nchar(10), nchar(10), N''The game has a built-in house edge, so over many rounds **every** strategy'', nchar(10), N''(martingale, always 2x, waiting for "patterns") loses points on average.'', nchar(10), nchar(10), N''Play for fun, use the limits and the session reminder, and take breaks.''), N''en'', N''no-winning-strategy'', N''Why there is no winning strategy'', ''2026-01-01T00:00:00.0000000Z'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Content', N'Language', N'Slug', N'Title', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[StaticPages]'))
        SET IDENTITY_INSERT [StaticPages] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024173209_AddExtendedModels'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Achievements_Code] ON [Achievements] ([Code]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024173209_AddExtendedModels'
)
BEGIN
    CREATE INDEX [IX_AutoBetConfigs_GameId] ON [AutoBetConfigs] ([GameId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024173209_AddExtendedModels'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_AutoBetConfigs_UserId_GameId] ON [AutoBetConfigs] ([UserId], [GameId]) WHERE [IsActive] = 1');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024173209_AddExtendedModels'
)
BEGIN
    CREATE INDEX [IX_BalanceAdjustments_AdminUserId] ON [BalanceAdjustments] ([AdminUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024173209_AddExtendedModels'
)
BEGIN
    CREATE INDEX [IX_BalanceAdjustments_UserId_CreatedAt] ON [BalanceAdjustments] ([UserId], [CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024173209_AddExtendedModels'
)
BEGIN
    CREATE UNIQUE INDEX [IX_BalanceAdjustments_WalletTransactionId] ON [BalanceAdjustments] ([WalletTransactionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024173209_AddExtendedModels'
)
BEGIN
    CREATE INDEX [IX_ChatMessages_DeletedByUserId] ON [ChatMessages] ([DeletedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024173209_AddExtendedModels'
)
BEGIN
    CREATE INDEX [IX_ChatMessages_SentAt] ON [ChatMessages] ([SentAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024173209_AddExtendedModels'
)
BEGIN
    CREATE INDEX [IX_ChatMessages_UserId] ON [ChatMessages] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024173209_AddExtendedModels'
)
BEGIN
    CREATE INDEX [IX_ChatMutes_MutedByUserId] ON [ChatMutes] ([MutedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024173209_AddExtendedModels'
)
BEGIN
    CREATE INDEX [IX_ChatMutes_UserId_MutedUntil] ON [ChatMutes] ([UserId], [MutedUntil]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024173209_AddExtendedModels'
)
BEGIN
    CREATE INDEX [IX_EmailVerificationCodes_UserId_CreatedAt] ON [EmailVerificationCodes] ([UserId], [CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024173209_AddExtendedModels'
)
BEGIN
    CREATE INDEX [IX_GameRoundEvents_RoundId_OccurredAt] ON [GameRoundEvents] ([RoundId], [OccurredAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024173209_AddExtendedModels'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Levels_XpRequired] ON [Levels] ([XpRequired]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024173209_AddExtendedModels'
)
BEGIN
    CREATE INDEX [IX_LoginAttempts_CreatedAt] ON [LoginAttempts] ([CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024173209_AddExtendedModels'
)
BEGIN
    CREATE INDEX [IX_LoginAttempts_UserId_CreatedAt] ON [LoginAttempts] ([UserId], [CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024173209_AddExtendedModels'
)
BEGIN
    CREATE INDEX [IX_PasswordResetCodes_UserId_CreatedAt] ON [PasswordResetCodes] ([UserId], [CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024173209_AddExtendedModels'
)
BEGIN
    CREATE INDEX [IX_PlayerStats_BiggestMultiplier] ON [PlayerStats] ([BiggestMultiplier]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024173209_AddExtendedModels'
)
BEGIN
    CREATE INDEX [IX_PlayerStats_BiggestWin] ON [PlayerStats] ([BiggestWin]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024173209_AddExtendedModels'
)
BEGIN
    CREATE INDEX [IX_PlayerStats_LevelNumber] ON [PlayerStats] ([LevelNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024173209_AddExtendedModels'
)
BEGIN
    CREATE INDEX [IX_PlaySessions_UserId_StartedAt] ON [PlaySessions] ([UserId], [StartedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024173209_AddExtendedModels'
)
BEGIN
    CREATE INDEX [IX_RoundCancellations_CancelledByUserId] ON [RoundCancellations] ([CancelledByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024173209_AddExtendedModels'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RoundCancellations_RoundId] ON [RoundCancellations] ([RoundId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024173209_AddExtendedModels'
)
BEGIN
    CREATE INDEX [IX_SelfExclusions_UserId_EndsAt] ON [SelfExclusions] ([UserId], [EndsAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024173209_AddExtendedModels'
)
BEGIN
    CREATE UNIQUE INDEX [IX_StaticPages_Slug_Language] ON [StaticPages] ([Slug], [Language]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024173209_AddExtendedModels'
)
BEGIN
    CREATE INDEX [IX_UserAchievements_AchievementId] ON [UserAchievements] ([AchievementId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024173209_AddExtendedModels'
)
BEGIN
    INSERT INTO UserPreferences (UserId, Theme, SoundEnabled, AnimationsEnabled, Language, UpdatedAt)
    SELECT u.Id, 'System', 1, 1, 'en', SYSUTCDATETIME()
    FROM Users u
    WHERE NOT EXISTS (SELECT 1 FROM UserPreferences p WHERE p.UserId = u.Id);

    INSERT INTO PlayerStats (UserId, TotalBets, CashOutCount, TotalWagered, TotalPayout, BiggestWin, BiggestMultiplier, Xp, LevelNumber, UpdatedAt)
    SELECT u.Id, 0, 0, 0, 0, 0, 0, 0, 1, SYSUTCDATETIME()
    FROM Users u
    WHERE NOT EXISTS (SELECT 1 FROM PlayerStats s WHERE s.UserId = u.Id);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024173209_AddExtendedModels'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261024173209_AddExtendedModels', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025121135_RefreshTokenRevokedReason'
)
BEGIN
    ALTER TABLE [RefreshTokens] ADD [RevokedReason] nvarchar(30) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025121135_RefreshTokenRevokedReason'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261025121135_RefreshTokenRevokedReason', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025160346_Phase4LimitsAndIndexes'
)
BEGIN
    ALTER TABLE [PlayerLimits] ADD [PendingDailyBetLimit] decimal(18,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025160346_Phase4LimitsAndIndexes'
)
BEGIN
    ALTER TABLE [PlayerLimits] ADD [PendingLimitAppliesAt] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025160346_Phase4LimitsAndIndexes'
)
BEGIN
    EXEC(N'ALTER TABLE [PlayerLimits] ADD CONSTRAINT [CK_PlayerLimits_PendingDailyBetLimit] CHECK ([PendingDailyBetLimit] IS NULL OR [PendingDailyBetLimit] > 0)');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025160346_Phase4LimitsAndIndexes'
)
BEGIN
    EXEC(N'ALTER TABLE [PlayerLimits] ADD CONSTRAINT [CK_PlayerLimits_PendingHasDate] CHECK ([PendingDailyBetLimit] IS NULL OR [PendingLimitAppliesAt] IS NOT NULL)');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025160346_Phase4LimitsAndIndexes'
)
BEGIN
    CREATE INDEX [IX_Notifications_UserId_CreatedAt] ON [Notifications] ([UserId], [CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025160346_Phase4LimitsAndIndexes'
)
BEGIN
    CREATE INDEX [IX_Bets_PlacedAt] ON [Bets] ([PlacedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261025160346_Phase4LimitsAndIndexes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261025160346_Phase4LimitsAndIndexes', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028170010_GeorgianDefaultLanguage'
)
BEGIN
    UPDATE [UserPreferences] SET [Language] = N'ka' WHERE [Language] = N'en';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028170010_GeorgianDefaultLanguage'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261028170010_GeorgianDefaultLanguage', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028174117_AchievementRewardsAndXp'
)
BEGIN
    ALTER TABLE [Achievements] ADD [VlxReward] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028174117_AchievementRewardsAndXp'
)
BEGIN
    EXEC(N'UPDATE [Achievements] SET [VlxReward] = 10
    WHERE [Id] = 1;
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028174117_AchievementRewardsAndXp'
)
BEGIN
    EXEC(N'UPDATE [Achievements] SET [VlxReward] = 20
    WHERE [Id] = 2;
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028174117_AchievementRewardsAndXp'
)
BEGIN
    EXEC(N'UPDATE [Achievements] SET [VlxReward] = 100
    WHERE [Id] = 3;
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028174117_AchievementRewardsAndXp'
)
BEGIN
    EXEC(N'UPDATE [Achievements] SET [VlxReward] = 50
    WHERE [Id] = 4;
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028174117_AchievementRewardsAndXp'
)
BEGIN
    EXEC(N'UPDATE [Achievements] SET [VlxReward] = 100
    WHERE [Id] = 5;
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028174117_AchievementRewardsAndXp'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'Description', N'Icon', N'Title', N'VlxReward', N'XpReward') AND [object_id] = OBJECT_ID(N'[Achievements]'))
        SET IDENTITY_INSERT [Achievements] ON;
    EXEC(N'INSERT INTO [Achievements] ([Id], [Code], [Description], [Icon], [Title], [VlxReward], [XpReward])
    VALUES (6, N''CASHOUTS_50'', N''Cash out 50 times.'', N''target'', N''Sharp eye'', 50, 75),
    (7, N''AUTO_CASHOUT'', N''Win a bet with auto cash-out.'', N''timer'', N''Set and forget'', 10, 20)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'Description', N'Icon', N'Title', N'VlxReward', N'XpReward') AND [object_id] = OBJECT_ID(N'[Achievements]'))
        SET IDENTITY_INSERT [Achievements] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028174117_AchievementRewardsAndXp'
)
BEGIN
    EXEC(N'ALTER TABLE [Achievements] ADD CONSTRAINT [CK_Achievements_VlxReward] CHECK ([VlxReward] >= 0)');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028174117_AchievementRewardsAndXp'
)
BEGIN
    UPDATE [PlayerStats] SET [Xp] = [TotalBets] + 2 * [CashOutCount];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028174117_AchievementRewardsAndXp'
)
BEGIN
    UPDATE ps SET [LevelNumber] = (SELECT MAX(l.[Number]) FROM [Levels] l WHERE l.[XpRequired] <= ps.[Xp]) FROM [PlayerStats] ps;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028174117_AchievementRewardsAndXp'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261028174117_AchievementRewardsAndXp', N'8.0.11');
END;
GO

COMMIT;
GO



/* ---------------------------------------------------------------------------------------------
 * Part 2: test accounts (same as DatabaseInitializer.TestUsers + User.Create)
 *   admin@veltrix.dev    / Admin123!     Admin
 *   operator@veltrix.dev / Operator123!  Operator
 *   player1@veltrix.dev  / Player123!    Player
 *   player2@veltrix.dev  / Player123!    Player
 * The hashes are BCrypt (work factor 11) of these public test passwords. Each account gets the
 * starting balance from SystemSettings with a "Welcome balance" ledger row, like a real sign-up.
 * --------------------------------------------------------------------------------------------- */

SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @now datetime2 = SYSUTCDATETIME();
DECLARE @balance decimal(18, 2) =
    ISNULL((SELECT TRY_CAST([Value] AS decimal(18, 2)) FROM [SystemSettings] WHERE [Key] = N'Wallet.StartingBalance'), 1000);

DECLARE @accounts TABLE ([Email] nvarchar(256), [Nickname] nvarchar(20), [Role] nvarchar(20), [PasswordHash] nvarchar(200));
INSERT INTO @accounts VALUES
    (N'admin@veltrix.dev',    N'admin',    N'Admin',    N'$2a$11$MfjWDSskHKntel.KaWYycexQtuow8wieFezKm3/.AvhhLkahChHsm'),
    (N'operator@veltrix.dev', N'operator', N'Operator', N'$2a$11$uTOqRvtllFLflMBenFsgbuJffAShcsxf9n0eU/0.lKbDu7Pc2kkpe'),
    (N'player1@veltrix.dev',  N'player1',  N'Player',   N'$2a$11$2wJX1hJsvxoo1oJJ26Vw.O9GaHVrgdmhEQH4rE/mPvxZHRklngFgq'),
    (N'player2@veltrix.dev',  N'player2',  N'Player',   N'$2a$11$BnN2xP6W9NAuC/CGSBleYeNDAi1gLebXHRBYassR.Pl./cYh/fkYe');

DECLARE @email nvarchar(256), @nickname nvarchar(20), @role nvarchar(20), @hash nvarchar(200), @userId int, @walletId int;

DECLARE account CURSOR LOCAL FAST_FORWARD FOR SELECT [Email], [Nickname], [Role], [PasswordHash] FROM @accounts;
OPEN account;
FETCH NEXT FROM account INTO @email, @nickname, @role, @hash;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (SELECT 1 FROM [Users] WHERE [Email] = @email)
    BEGIN
        INSERT INTO [Users] ([Email], [Nickname], [PasswordHash], [Role], [DateOfBirth], [IsBlocked], [FailedLoginCount], [CreatedAt], [IsEmailVerified])
        VALUES (@email, @nickname, @hash, @role, '2000-01-01', 0, 0, @now, 0);
        SET @userId = SCOPE_IDENTITY();

        INSERT INTO [Wallets] ([UserId], [Balance]) VALUES (@userId, @balance);
        SET @walletId = SCOPE_IDENTITY();

        IF @balance > 0
            INSERT INTO [WalletTransactions] ([WalletId], [Type], [Amount], [BalanceBefore], [BalanceAfter], [IdempotencyKey], [Reason], [CreatedAt])
            VALUES (@walletId, N'Bonus', @balance, 0, @balance, N'welcome:' + @email, N'Welcome balance', @now);

        INSERT INTO [PlayerLimits] ([UserId], [SessionReminderMin]) VALUES (@userId, 60);
        INSERT INTO [UserPreferences] ([UserId], [Theme], [SoundEnabled], [AnimationsEnabled], [Language], [UpdatedAt])
        VALUES (@userId, N'System', 1, 1, N'ka', @now);
        INSERT INTO [PlayerStats] ([UserId], [TotalBets], [CashOutCount], [TotalWagered], [TotalPayout], [BiggestWin], [BiggestMultiplier], [Xp], [LevelNumber], [UpdatedAt])
        VALUES (@userId, 0, 0, 0, 0, 0, 0, 0, 1, @now);
    END;
    FETCH NEXT FROM account INTO @email, @nickname, @role, @hash;
END;
CLOSE account;
DEALLOCATE account;

COMMIT TRANSACTION;
GO
