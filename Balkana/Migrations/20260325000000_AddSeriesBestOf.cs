using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Balkana.Migrations
{
    /// <inheritdoc />
    public partial class AddSeriesBestOf : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BestOf",
                table: "Series",
                type: "int",
                nullable: true);

            // Backfill existing series rows based on their Round.
            migrationBuilder.Sql(@"
UPDATE [Series]
SET [BestOf] =
    CASE
        WHEN [Round] = 1 THEN 1
        WHEN [Round] = 2 THEN 3
        ELSE 5
    END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BestOf",
                table: "Series");
        }
    }
}

