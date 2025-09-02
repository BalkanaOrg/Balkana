using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Balkana.Migrations
{
    /// <inheritdoc />
    public partial class PlayerStatisticUpdate2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Flashes",
                table: "PlayerStats",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KnifeKills",
                table: "PlayerStats",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PistolKills",
                table: "PlayerStats",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RoundsPlayed",
                table: "PlayerStats",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SniperKills",
                table: "PlayerStats",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UtilityUsage",
                table: "PlayerStats",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Flashes",
                table: "PlayerStats");

            migrationBuilder.DropColumn(
                name: "KnifeKills",
                table: "PlayerStats");

            migrationBuilder.DropColumn(
                name: "PistolKills",
                table: "PlayerStats");

            migrationBuilder.DropColumn(
                name: "RoundsPlayed",
                table: "PlayerStats");

            migrationBuilder.DropColumn(
                name: "SniperKills",
                table: "PlayerStats");

            migrationBuilder.DropColumn(
                name: "UtilityUsage",
                table: "PlayerStats");
        }
    }
}
