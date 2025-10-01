using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Balkana.Migrations
{
    /// <inheritdoc />
    public partial class AddPointsAndPrizeConfigurationToTournament : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PointsConfiguration",
                table: "Tournaments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PrizeConfiguration",
                table: "Tournaments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PointsConfiguration",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "PrizeConfiguration",
                table: "Tournaments");
        }
    }
}
