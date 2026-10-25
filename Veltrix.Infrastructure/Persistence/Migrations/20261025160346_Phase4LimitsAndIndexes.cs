using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Veltrix.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase4LimitsAndIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PendingDailyBetLimit",
                table: "PlayerLimits",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PendingLimitAppliesAt",
                table: "PlayerLimits",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_PlayerLimits_PendingDailyBetLimit",
                table: "PlayerLimits",
                sql: "[PendingDailyBetLimit] IS NULL OR [PendingDailyBetLimit] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PlayerLimits_PendingHasDate",
                table: "PlayerLimits",
                sql: "[PendingDailyBetLimit] IS NULL OR [PendingLimitAppliesAt] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_CreatedAt",
                table: "Notifications",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Bets_PlacedAt",
                table: "Bets",
                column: "PlacedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_PlayerLimits_PendingDailyBetLimit",
                table: "PlayerLimits");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PlayerLimits_PendingHasDate",
                table: "PlayerLimits");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_CreatedAt",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Bets_PlacedAt",
                table: "Bets");

            migrationBuilder.DropColumn(
                name: "PendingDailyBetLimit",
                table: "PlayerLimits");

            migrationBuilder.DropColumn(
                name: "PendingLimitAppliesAt",
                table: "PlayerLimits");
        }
    }
}
