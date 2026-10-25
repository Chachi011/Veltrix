using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Veltrix.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefreshTokenRevokedReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RevokedReason",
                table: "RefreshTokens",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RevokedReason",
                table: "RefreshTokens");
        }
    }
}
