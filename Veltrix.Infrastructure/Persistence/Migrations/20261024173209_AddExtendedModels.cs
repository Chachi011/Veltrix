using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Veltrix.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExtendedModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordResetAttempts",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PasswordResetCodeExpiresAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PasswordResetCodeHash",
                table: "Users");

            migrationBuilder.AddColumn<bool>(
                name: "IsEmailVerified",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Achievements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    XpReward = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Achievements", x => x.Id);
                    table.CheckConstraint("CK_Achievements_XpReward", "[XpReward] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "AutoBetConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    GameId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AutoCashOutAt = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    TotalRounds = table.Column<int>(type: "int", nullable: false),
                    RoundsPlayed = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StoppedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutoBetConfigs", x => x.Id);
                    table.CheckConstraint("CK_AutoBetConfigs_Amount", "[Amount] > 0");
                    table.CheckConstraint("CK_AutoBetConfigs_CashOut", "[AutoCashOutAt] >= 1.01");
                    table.CheckConstraint("CK_AutoBetConfigs_Rounds", "[TotalRounds] BETWEEN 1 AND 1000 AND [RoundsPlayed] BETWEEN 0 AND [TotalRounds]");
                    table.ForeignKey(
                        name: "FK_AutoBetConfigs_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AutoBetConfigs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BalanceAdjustments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    AdminUserId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    WalletTransactionId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BalanceAdjustments", x => x.Id);
                    table.CheckConstraint("CK_BalanceAdjustments_Amount", "[Amount] <> 0");
                    table.CheckConstraint("CK_BalanceAdjustments_Reason", "LEN([Reason]) >= 5");
                    table.ForeignKey(
                        name: "FK_BalanceAdjustments_Users_AdminUserId",
                        column: x => x.AdminUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BalanceAdjustments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BalanceAdjustments_WalletTransactions_WalletTransactionId",
                        column: x => x.WalletTransactionId,
                        principalTable: "WalletTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedByUserId = table.Column<int>(type: "int", nullable: true),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatMessages_Users_DeletedByUserId",
                        column: x => x.DeletedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChatMessages_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChatMutes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    MutedByUserId = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    MutedUntil = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMutes", x => x.Id);
                    table.CheckConstraint("CK_ChatMutes_Until", "[MutedUntil] > [CreatedAt]");
                    table.ForeignKey(
                        name: "FK_ChatMutes_Users_MutedByUserId",
                        column: x => x.MutedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChatMutes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DailyStatistics",
                columns: table => new
                {
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    ActiveUsers = table.Column<int>(type: "int", nullable: false),
                    NewUsers = table.Column<int>(type: "int", nullable: false),
                    RoundsPlayed = table.Column<int>(type: "int", nullable: false),
                    BetsCount = table.Column<int>(type: "int", nullable: false),
                    BetsVolume = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PayoutVolume = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AverageCrashPoint = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyStatistics", x => x.Date);
                    table.CheckConstraint("CK_DailyStatistics_NonNegative", "[ActiveUsers] >= 0 AND [NewUsers] >= 0 AND [RoundsPlayed] >= 0 AND [BetsCount] >= 0 AND [BetsVolume] >= 0 AND [PayoutVolume] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "EmailVerificationCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CodeHash = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailVerificationCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailVerificationCodes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GameRoundEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoundId = table.Column<int>(type: "int", nullable: false),
                    FromState = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ToState = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameRoundEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameRoundEvents_GameRounds_RoundId",
                        column: x => x.RoundId,
                        principalTable: "GameRounds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Levels",
                columns: table => new
                {
                    Number = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    XpRequired = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Levels", x => x.Number);
                    table.CheckConstraint("CK_Levels_Xp", "[XpRequired] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "LoginAttempts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Succeeded = table.Column<bool>(type: "bit", nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoginAttempts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PasswordResetCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CodeHash = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetCodes", x => x.Id);
                    table.CheckConstraint("CK_PasswordResetCodes_Attempts", "[Attempts] BETWEEN 0 AND 5");
                    table.ForeignKey(
                        name: "FK_PasswordResetCodes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlaySessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastActivityAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RemindersSent = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaySessions", x => x.Id);
                    table.CheckConstraint("CK_PlaySessions_Activity", "[LastActivityAt] >= [StartedAt]");
                    table.ForeignKey(
                        name: "FK_PlaySessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoundCancellations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoundId = table.Column<int>(type: "int", nullable: false),
                    CancelledByUserId = table.Column<int>(type: "int", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RefundedBetsCount = table.Column<int>(type: "int", nullable: false),
                    RefundedTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoundCancellations", x => x.Id);
                    table.CheckConstraint("CK_RoundCancellations_Refunds", "[RefundedBetsCount] >= 0 AND [RefundedTotal] >= 0");
                    table.ForeignKey(
                        name: "FK_RoundCancellations_GameRounds_RoundId",
                        column: x => x.RoundId,
                        principalTable: "GameRounds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoundCancellations_Users_CancelledByUserId",
                        column: x => x.CancelledByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SelfExclusions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Period = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StartsAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SelfExclusions", x => x.Id);
                    table.CheckConstraint("CK_SelfExclusions_Period", "[EndsAt] > [StartsAt]");
                    table.ForeignKey(
                        name: "FK_SelfExclusions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StaticPages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Slug = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Language = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaticPages", x => x.Id);
                    table.CheckConstraint("CK_StaticPages_Language", "[Language] IN ('en', 'ka')");
                });

            migrationBuilder.CreateTable(
                name: "UserPreferences",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Theme = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SoundEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AnimationsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    Language = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPreferences", x => x.UserId);
                    table.CheckConstraint("CK_UserPreferences_Language", "[Language] IN ('en', 'ka')");
                    table.ForeignKey(
                        name: "FK_UserPreferences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTwoFactors",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    SecretKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EnabledAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTwoFactors", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserTwoFactors_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserAchievements",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    AchievementId = table.Column<int>(type: "int", nullable: false),
                    UnlockedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAchievements", x => new { x.UserId, x.AchievementId });
                    table.ForeignKey(
                        name: "FK_UserAchievements_Achievements_AchievementId",
                        column: x => x.AchievementId,
                        principalTable: "Achievements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserAchievements_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerStats",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    TotalBets = table.Column<int>(type: "int", nullable: false),
                    CashOutCount = table.Column<int>(type: "int", nullable: false),
                    TotalWagered = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalPayout = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BiggestWin = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BiggestMultiplier = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Xp = table.Column<int>(type: "int", nullable: false),
                    LevelNumber = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerStats", x => x.UserId);
                    table.CheckConstraint("CK_PlayerStats_CashOuts", "[CashOutCount] <= [TotalBets]");
                    table.CheckConstraint("CK_PlayerStats_NonNegative", "[TotalBets] >= 0 AND [CashOutCount] >= 0 AND [TotalWagered] >= 0 AND [TotalPayout] >= 0 AND [Xp] >= 0");
                    table.ForeignKey(
                        name: "FK_PlayerStats_Levels_LevelNumber",
                        column: x => x.LevelNumber,
                        principalTable: "Levels",
                        principalColumn: "Number",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerStats_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Achievements",
                columns: new[] { "Id", "Code", "Description", "Icon", "Title", "XpReward" },
                values: new object[,]
                {
                    { 1, "FIRST_BET", "Place your first bet.", "rocket", "First bet", 10 },
                    { 2, "FIRST_CASHOUT", "Cash out before the crash for the first time.", "coins", "First cash-out", 20 },
                    { 3, "CASHOUT_10X", "Cash out at 10.00x or higher.", "bolt", "To the moon", 100 },
                    { 4, "BETS_100", "Place 100 bets.", "trophy", "Regular", 50 },
                    { 5, "BIG_WIN_1000", "Win 1000 VLX or more in a single bet.", "star", "Big win", 100 }
                });

            migrationBuilder.InsertData(
                table: "Levels",
                columns: new[] { "Number", "Title", "XpRequired" },
                values: new object[,]
                {
                    { 1, "Rookie", 0 },
                    { 2, "Spotter", 100 },
                    { 3, "Climber", 250 },
                    { 4, "Pilot", 500 },
                    { 5, "Navigator", 1000 },
                    { 6, "Ace", 2000 },
                    { 7, "Veteran", 3500 },
                    { 8, "Elite", 5500 },
                    { 9, "Legend", 8000 },
                    { 10, "Mythic", 12000 }
                });

            migrationBuilder.InsertData(
                table: "StaticPages",
                columns: new[] { "Id", "Content", "Language", "Slug", "Title", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "1. During **betting** you place a bet with virtual VLX points.\n2. The round starts and the multiplier grows from **1.00x**.\n3. Press **Cash out** at any moment: you get *bet × current multiplier*.\n4. If the round **crashes** before you cash out, the bet is lost.\n5. The crash point is decided **before** the round starts. Check it on the Provably Fair page.\n\nVLX points have no monetary value and cannot be bought or withdrawn.", "en", "rules", "Crash game rules", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, "Every round's crash point is random and independent of previous rounds.\nA long series of low crashes does **not** make a high crash more likely.\n\nThe game has a built-in house edge, so over many rounds **every** strategy\n(martingale, always 2x, waiting for \"patterns\") loses points on average.\n\nPlay for fun, use the limits and the session reminder, and take breaks.", "en", "no-winning-strategy", "Why there is no winning strategy", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Achievements_Code",
                table: "Achievements",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AutoBetConfigs_GameId",
                table: "AutoBetConfigs",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_AutoBetConfigs_UserId_GameId",
                table: "AutoBetConfigs",
                columns: new[] { "UserId", "GameId" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_BalanceAdjustments_AdminUserId",
                table: "BalanceAdjustments",
                column: "AdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BalanceAdjustments_UserId_CreatedAt",
                table: "BalanceAdjustments",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BalanceAdjustments_WalletTransactionId",
                table: "BalanceAdjustments",
                column: "WalletTransactionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_DeletedByUserId",
                table: "ChatMessages",
                column: "DeletedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_SentAt",
                table: "ChatMessages",
                column: "SentAt");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_UserId",
                table: "ChatMessages",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMutes_MutedByUserId",
                table: "ChatMutes",
                column: "MutedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMutes_UserId_MutedUntil",
                table: "ChatMutes",
                columns: new[] { "UserId", "MutedUntil" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailVerificationCodes_UserId_CreatedAt",
                table: "EmailVerificationCodes",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GameRoundEvents_RoundId_OccurredAt",
                table: "GameRoundEvents",
                columns: new[] { "RoundId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Levels_XpRequired",
                table: "Levels",
                column: "XpRequired",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_CreatedAt",
                table: "LoginAttempts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_UserId_CreatedAt",
                table: "LoginAttempts",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetCodes_UserId_CreatedAt",
                table: "PasswordResetCodes",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerStats_BiggestMultiplier",
                table: "PlayerStats",
                column: "BiggestMultiplier");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerStats_BiggestWin",
                table: "PlayerStats",
                column: "BiggestWin");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerStats_LevelNumber",
                table: "PlayerStats",
                column: "LevelNumber");

            migrationBuilder.CreateIndex(
                name: "IX_PlaySessions_UserId_StartedAt",
                table: "PlaySessions",
                columns: new[] { "UserId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RoundCancellations_CancelledByUserId",
                table: "RoundCancellations",
                column: "CancelledByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RoundCancellations_RoundId",
                table: "RoundCancellations",
                column: "RoundId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SelfExclusions_UserId_EndsAt",
                table: "SelfExclusions",
                columns: new[] { "UserId", "EndsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StaticPages_Slug_Language",
                table: "StaticPages",
                columns: new[] { "Slug", "Language" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAchievements_AchievementId",
                table: "UserAchievements",
                column: "AchievementId");

            // Data migration: users created before this migration get the 1-to-1 rows
            // that User.Create() now creates for every new user.
            migrationBuilder.Sql("""
                INSERT INTO UserPreferences (UserId, Theme, SoundEnabled, AnimationsEnabled, Language, UpdatedAt)
                SELECT u.Id, 'System', 1, 1, 'en', SYSUTCDATETIME()
                FROM Users u
                WHERE NOT EXISTS (SELECT 1 FROM UserPreferences p WHERE p.UserId = u.Id);

                INSERT INTO PlayerStats (UserId, TotalBets, CashOutCount, TotalWagered, TotalPayout, BiggestWin, BiggestMultiplier, Xp, LevelNumber, UpdatedAt)
                SELECT u.Id, 0, 0, 0, 0, 0, 0, 0, 1, SYSUTCDATETIME()
                FROM Users u
                WHERE NOT EXISTS (SELECT 1 FROM PlayerStats s WHERE s.UserId = u.Id);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AutoBetConfigs");

            migrationBuilder.DropTable(
                name: "BalanceAdjustments");

            migrationBuilder.DropTable(
                name: "ChatMessages");

            migrationBuilder.DropTable(
                name: "ChatMutes");

            migrationBuilder.DropTable(
                name: "DailyStatistics");

            migrationBuilder.DropTable(
                name: "EmailVerificationCodes");

            migrationBuilder.DropTable(
                name: "GameRoundEvents");

            migrationBuilder.DropTable(
                name: "LoginAttempts");

            migrationBuilder.DropTable(
                name: "PasswordResetCodes");

            migrationBuilder.DropTable(
                name: "PlayerStats");

            migrationBuilder.DropTable(
                name: "PlaySessions");

            migrationBuilder.DropTable(
                name: "RoundCancellations");

            migrationBuilder.DropTable(
                name: "SelfExclusions");

            migrationBuilder.DropTable(
                name: "StaticPages");

            migrationBuilder.DropTable(
                name: "UserAchievements");

            migrationBuilder.DropTable(
                name: "UserPreferences");

            migrationBuilder.DropTable(
                name: "UserTwoFactors");

            migrationBuilder.DropTable(
                name: "Levels");

            migrationBuilder.DropTable(
                name: "Achievements");

            migrationBuilder.DropColumn(
                name: "IsEmailVerified",
                table: "Users");

            migrationBuilder.AddColumn<int>(
                name: "PasswordResetAttempts",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetCodeExpiresAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetCodeHash",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
