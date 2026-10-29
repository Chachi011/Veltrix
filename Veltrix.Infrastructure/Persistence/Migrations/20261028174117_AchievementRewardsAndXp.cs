using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Veltrix.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AchievementRewardsAndXp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VlxReward",
                table: "Achievements",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Achievements",
                keyColumn: "Id",
                keyValue: 1,
                column: "VlxReward",
                value: 10);

            migrationBuilder.UpdateData(
                table: "Achievements",
                keyColumn: "Id",
                keyValue: 2,
                column: "VlxReward",
                value: 20);

            migrationBuilder.UpdateData(
                table: "Achievements",
                keyColumn: "Id",
                keyValue: 3,
                column: "VlxReward",
                value: 100);

            migrationBuilder.UpdateData(
                table: "Achievements",
                keyColumn: "Id",
                keyValue: 4,
                column: "VlxReward",
                value: 50);

            migrationBuilder.UpdateData(
                table: "Achievements",
                keyColumn: "Id",
                keyValue: 5,
                column: "VlxReward",
                value: 100);

            migrationBuilder.InsertData(
                table: "Achievements",
                columns: new[] { "Id", "Code", "Description", "Icon", "Title", "VlxReward", "XpReward" },
                values: new object[,]
                {
                    { 6, "CASHOUTS_50", "Cash out 50 times.", "target", "Sharp eye", 50, 75 },
                    { 7, "AUTO_CASHOUT", "Win a bet with auto cash-out.", "timer", "Set and forget", 10, 20 }
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Achievements_VlxReward",
                table: "Achievements",
                sql: "[VlxReward] >= 0");

            // XP was never awarded before: give every player the XP the new rule would have given
            // (Experience: 1 per settled bet + 2 more per cash-out) and the matching level. Achievements are not
            // backfilled here: they unlock at the player's next settled bet, so their VLX reward goes through the ledger.
            migrationBuilder.Sql("UPDATE [PlayerStats] SET [Xp] = [TotalBets] + 2 * [CashOutCount];");
            migrationBuilder.Sql(
                "UPDATE ps SET [LevelNumber] = (SELECT MAX(l.[Number]) FROM [Levels] l WHERE l.[XpRequired] <= ps.[Xp]) FROM [PlayerStats] ps;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Achievements_VlxReward",
                table: "Achievements");

            migrationBuilder.DeleteData(
                table: "Achievements",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Achievements",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DropColumn(
                name: "VlxReward",
                table: "Achievements");
        }
    }
}
