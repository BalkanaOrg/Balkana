using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Balkana.Migrations
{
    /// <inheritdoc />
    public partial class AddWinnerTeamToSeries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WinnerTeamId",
                table: "Series",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Series_WinnerTeamId",
                table: "Series",
                column: "WinnerTeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_Series_Teams_WinnerTeamId",
                table: "Series",
                column: "WinnerTeamId",
                principalTable: "Teams",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Series_Teams_WinnerTeamId",
                table: "Series");

            migrationBuilder.DropIndex(
                name: "IX_Series_WinnerTeamId",
                table: "Series");

            migrationBuilder.DropColumn(
                name: "WinnerTeamId",
                table: "Series");
        }
    }
}
