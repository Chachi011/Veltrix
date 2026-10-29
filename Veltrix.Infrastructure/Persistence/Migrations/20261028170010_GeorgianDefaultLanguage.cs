using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Veltrix.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Georgian becomes the default UI language (spec 4.7). Until now there was no way to choose a language,
    /// so every stored "en" is just the old default, not a choice: those rows switch to "ka".
    /// No schema change (the default lives in the C# entity).
    /// </summary>
    public partial class GeorgianDefaultLanguage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE [UserPreferences] SET [Language] = N'ka' WHERE [Language] = N'en';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Nothing to undo: after this migration "ka" may also be a real choice, which must not be overwritten
        }
    }
}
