using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Balkana.Migrations
{
    /// <inheritdoc />
    public partial class BracketBuilderExpansion3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Series_Teams_TeamAId1",
                table: "Series");

            migrationBuilder.DropIndex(
                name: "IX_Series_TeamAId1",
                table: "Series");

            migrationBuilder.DropColumn(
                name: "TeamAId1",
                table: "Series");

            migrationBuilder.CreateIndex(
                name: "IX_Series_TeamBId",
                table: "Series",
                column: "TeamBId");

            migrationBuilder.AddForeignKey(
                name: "FK_Series_Teams_TeamBId",
                table: "Series",
                column: "TeamBId",
                principalTable: "Teams",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Series_Teams_TeamBId",
                table: "Series");

            migrationBuilder.DropIndex(
                name: "IX_Series_TeamBId",
                table: "Series");

            migrationBuilder.AddColumn<int>(
                name: "TeamAId1",
                table: "Series",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Series_TeamAId1",
                table: "Series",
                column: "TeamAId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Series_Teams_TeamAId1",
                table: "Series",
                column: "TeamAId1",
                principalTable: "Teams",
                principalColumn: "Id");
        }
    }
}
