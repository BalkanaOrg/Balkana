using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Balkana.Migrations
{
    /// <inheritdoc />
    public partial class BracketBuilderExpansion2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Series_Teams_TeamBId",
                table: "Series");

            migrationBuilder.DropIndex(
                name: "IX_Series_TeamBId",
                table: "Series");

            migrationBuilder.AlterColumn<int>(
                name: "TeamBId",
                table: "Series",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "TeamAId",
                table: "Series",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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

            migrationBuilder.AlterColumn<int>(
                name: "TeamBId",
                table: "Series",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TeamAId",
                table: "Series",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

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
    }
}
