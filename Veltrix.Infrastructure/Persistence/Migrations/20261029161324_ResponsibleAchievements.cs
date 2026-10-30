using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Veltrix.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ResponsibleAchievements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Achievements",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Kind",
                table: "Achievements",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Play");

            migrationBuilder.UpdateData(
                table: "Achievements",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Description", "IsActive", "Kind", "Title" },
                values: new object[] { "Play your first round of Crash.", true, "Play", "Try Crash" });

            migrationBuilder.UpdateData(
                table: "Achievements",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "IsActive", "Kind" },
                values: new object[] { true, "Play" });

            migrationBuilder.UpdateData(
                table: "Achievements",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "IsActive", "Kind" },
                values: new object[] { false, "Play" });

            migrationBuilder.UpdateData(
                table: "Achievements",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "IsActive", "Kind" },
                values: new object[] { false, "Play" });

            migrationBuilder.UpdateData(
                table: "Achievements",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "IsActive", "Kind" },
                values: new object[] { false, "Play" });

            migrationBuilder.UpdateData(
                table: "Achievements",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "IsActive", "Kind" },
                values: new object[] { false, "Play" });

            migrationBuilder.UpdateData(
                table: "Achievements",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "Description", "IsActive", "Kind", "Title" },
                values: new object[] { "Place a bet with an auto cash-out target.", true, "Play", "Plan ahead" });

            migrationBuilder.InsertData(
                table: "Achievements",
                columns: new[] { "Id", "Code", "Description", "Icon", "IsActive", "Kind", "Title", "VlxReward", "XpReward" },
                values: new object[,]
                {
                    { 8, "READ_RULES", "Read the game rules and why Crash has no winning strategy.", "book", true, "Learn", "Know the rules", 20, 40 },
                    { 9, "VERIFY_ROUND", "Verify a finished round on the Provably fair page.", "shield", true, "Learn", "Check a round", 20, 50 },
                    { 10, "SET_DAILY_LIMIT", "Set a daily bet limit on the Responsible play page.", "limit", true, "Safety", "Set your limit", 20, 60 }
                });

            migrationBuilder.UpdateData(
                table: "Levels",
                keyColumn: "Number",
                keyValue: 2,
                column: "XpRequired",
                value: 10);

            migrationBuilder.UpdateData(
                table: "Levels",
                keyColumn: "Number",
                keyValue: 3,
                column: "XpRequired",
                value: 30);

            migrationBuilder.UpdateData(
                table: "Levels",
                keyColumn: "Number",
                keyValue: 4,
                column: "XpRequired",
                value: 50);

            migrationBuilder.UpdateData(
                table: "Levels",
                keyColumn: "Number",
                keyValue: 5,
                column: "XpRequired",
                value: 70);

            migrationBuilder.UpdateData(
                table: "Levels",
                keyColumn: "Number",
                keyValue: 6,
                column: "XpRequired",
                value: 90);

            migrationBuilder.UpdateData(
                table: "Levels",
                keyColumn: "Number",
                keyValue: 7,
                column: "XpRequired",
                value: 110);

            migrationBuilder.UpdateData(
                table: "Levels",
                keyColumn: "Number",
                keyValue: 8,
                column: "XpRequired",
                value: 140);

            migrationBuilder.UpdateData(
                table: "Levels",
                keyColumn: "Number",
                keyValue: 9,
                column: "XpRequired",
                value: 170);

            migrationBuilder.UpdateData(
                table: "Levels",
                keyColumn: "Number",
                keyValue: 10,
                column: "XpRequired",
                value: 200);

            migrationBuilder.CreateIndex(
                name: "IX_GameRounds_ServerSeedHash",
                table: "GameRounds",
                column: "ServerSeedHash");

            // Data fix (responsible play): XP now comes ONLY from achievements, never from bets. Every player's XP is
            // recalculated from the active achievements they unlocked, and the level from the new thresholds.
            // VLX already paid for retired achievements stays in the ledger (ledger rows are never changed).
            migrationBuilder.Sql("""
                UPDATE s SET Xp = ISNULL(x.Xp, 0)
                FROM PlayerStats s
                OUTER APPLY (
                    SELECT SUM(a.XpReward) AS Xp
                    FROM UserAchievements ua
                    JOIN Achievements a ON a.Id = ua.AchievementId
                    WHERE ua.UserId = s.UserId AND a.IsActive = 1) x;

                UPDATE s SET LevelNumber = (SELECT MAX(l.Number) FROM Levels l WHERE l.XpRequired <= s.Xp)
                FROM PlayerStats s;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The three new achievements are deleted below; their unlocks go first (the foreign key is Restrict)
            migrationBuilder.Sql("DELETE FROM UserAchievements WHERE AchievementId IN (8, 9, 10);");

            migrationBuilder.DropIndex(
                name: "IX_GameRounds_ServerSeedHash",
                table: "GameRounds");

            migrationBuilder.DeleteData(
                table: "Achievements",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Achievements",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Achievements",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Achievements");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "Achievements");

            migrationBuilder.UpdateData(
                table: "Achievements",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Description", "Title" },
                values: new object[] { "Place your first bet.", "First bet" });

            migrationBuilder.UpdateData(
                table: "Achievements",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "Description", "Title" },
                values: new object[] { "Win a bet with auto cash-out.", "Set and forget" });

            migrationBuilder.UpdateData(
                table: "Levels",
                keyColumn: "Number",
                keyValue: 2,
                column: "XpRequired",
                value: 100);

            migrationBuilder.UpdateData(
                table: "Levels",
                keyColumn: "Number",
                keyValue: 3,
                column: "XpRequired",
                value: 250);

            migrationBuilder.UpdateData(
                table: "Levels",
                keyColumn: "Number",
                keyValue: 4,
                column: "XpRequired",
                value: 500);

            migrationBuilder.UpdateData(
                table: "Levels",
                keyColumn: "Number",
                keyValue: 5,
                column: "XpRequired",
                value: 1000);

            migrationBuilder.UpdateData(
                table: "Levels",
                keyColumn: "Number",
                keyValue: 6,
                column: "XpRequired",
                value: 2000);

            migrationBuilder.UpdateData(
                table: "Levels",
                keyColumn: "Number",
                keyValue: 7,
                column: "XpRequired",
                value: 3500);

            migrationBuilder.UpdateData(
                table: "Levels",
                keyColumn: "Number",
                keyValue: 8,
                column: "XpRequired",
                value: 5500);

            migrationBuilder.UpdateData(
                table: "Levels",
                keyColumn: "Number",
                keyValue: 9,
                column: "XpRequired",
                value: 8000);

            migrationBuilder.UpdateData(
                table: "Levels",
                keyColumn: "Number",
                keyValue: 10,
                column: "XpRequired",
                value: 12000);

            // Back to the old rule: 1 XP per settled bet, 2 more per cash-out, plus the XP of every unlocked achievement
            migrationBuilder.Sql("""
                UPDATE s SET Xp = s.TotalBets + 2 * s.CashOutCount + ISNULL(x.Xp, 0)
                FROM PlayerStats s
                OUTER APPLY (
                    SELECT SUM(a.XpReward) AS Xp
                    FROM UserAchievements ua
                    JOIN Achievements a ON a.Id = ua.AchievementId
                    WHERE ua.UserId = s.UserId) x;

                UPDATE s SET LevelNumber = (SELECT MAX(l.Number) FROM Levels l WHERE l.XpRequired <= s.Xp)
                FROM PlayerStats s;
                """);
        }
    }
}
