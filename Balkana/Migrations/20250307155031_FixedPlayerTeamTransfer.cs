using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Balkana.Migrations
{
    public partial class FixedPlayerTeamTransfer : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayerTeamTransfers_TeamPosition_TeamPositionId",
                table: "PlayerTeamTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_Teams_PlayerTeamTransfers_PlayerTeamTransferId",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_Teams_PlayerTeamTransferId",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_PlayerTeamTransfers_TeamPositionId",
                table: "PlayerTeamTransfers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TeamPosition",
                table: "TeamPosition");

            migrationBuilder.DropColumn(
                name: "PlayerTeamTransferId",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "TeamPositionId",
                table: "PlayerTeamTransfers");

            migrationBuilder.RenameTable(
                name: "TeamPosition",
                newName: "Positions");

            migrationBuilder.AddColumn<int>(
                name: "GameId",
                table: "Positions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Positions",
                table: "Positions",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerTeamTransfers_PositionId",
                table: "PlayerTeamTransfers",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_GameId",
                table: "Positions",
                column: "GameId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerTeamTransfers_Positions_PositionId",
                table: "PlayerTeamTransfers",
                column: "PositionId",
                principalTable: "Positions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Positions_Games_GameId",
                table: "Positions",
                column: "GameId",
                principalTable: "Games",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayerTeamTransfers_Positions_PositionId",
                table: "PlayerTeamTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_Positions_Games_GameId",
                table: "Positions");

            migrationBuilder.DropIndex(
                name: "IX_PlayerTeamTransfers_PositionId",
                table: "PlayerTeamTransfers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Positions",
                table: "Positions");

            migrationBuilder.DropIndex(
                name: "IX_Positions_GameId",
                table: "Positions");

            migrationBuilder.DropColumn(
                name: "GameId",
                table: "Positions");

            migrationBuilder.RenameTable(
                name: "Positions",
                newName: "TeamPosition");

            migrationBuilder.AddColumn<int>(
                name: "PlayerTeamTransferId",
                table: "Teams",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TeamPositionId",
                table: "PlayerTeamTransfers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_TeamPosition",
                table: "TeamPosition",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_PlayerTeamTransferId",
                table: "Teams",
                column: "PlayerTeamTransferId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerTeamTransfers_TeamPositionId",
                table: "PlayerTeamTransfers",
                column: "TeamPositionId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerTeamTransfers_TeamPosition_TeamPositionId",
                table: "PlayerTeamTransfers",
                column: "TeamPositionId",
                principalTable: "TeamPosition",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_PlayerTeamTransfers_PlayerTeamTransferId",
                table: "Teams",
                column: "PlayerTeamTransferId",
                principalTable: "PlayerTeamTransfers",
                principalColumn: "Id");
        }
    }
}
