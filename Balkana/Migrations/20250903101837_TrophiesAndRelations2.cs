using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Balkana.Migrations
{
    /// <inheritdoc />
    public partial class TrophiesAndRelations2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayerTrophy_Players_PlayerId",
                table: "PlayerTrophy");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerTrophy_Trophies_TrophyId",
                table: "PlayerTrophy");

            migrationBuilder.DropForeignKey(
                name: "FK_TeamTrophy_Teams_TeamId",
                table: "TeamTrophy");

            migrationBuilder.DropForeignKey(
                name: "FK_TeamTrophy_Trophies_TrophyId",
                table: "TeamTrophy");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TeamTrophy",
                table: "TeamTrophy");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PlayerTrophy",
                table: "PlayerTrophy");

            migrationBuilder.RenameTable(
                name: "TeamTrophy",
                newName: "TeamTrophies");

            migrationBuilder.RenameTable(
                name: "PlayerTrophy",
                newName: "PlayerTrophies");

            migrationBuilder.RenameIndex(
                name: "IX_TeamTrophy_TrophyId",
                table: "TeamTrophies",
                newName: "IX_TeamTrophies_TrophyId");

            migrationBuilder.RenameIndex(
                name: "IX_TeamTrophy_TeamId",
                table: "TeamTrophies",
                newName: "IX_TeamTrophies_TeamId");

            migrationBuilder.RenameIndex(
                name: "IX_PlayerTrophy_TrophyId",
                table: "PlayerTrophies",
                newName: "IX_PlayerTrophies_TrophyId");

            migrationBuilder.RenameIndex(
                name: "IX_PlayerTrophy_PlayerId",
                table: "PlayerTrophies",
                newName: "IX_PlayerTrophies_PlayerId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TeamTrophies",
                table: "TeamTrophies",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PlayerTrophies",
                table: "PlayerTrophies",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerTrophies_Players_PlayerId",
                table: "PlayerTrophies",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerTrophies_Trophies_TrophyId",
                table: "PlayerTrophies",
                column: "TrophyId",
                principalTable: "Trophies",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TeamTrophies_Teams_TeamId",
                table: "TeamTrophies",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TeamTrophies_Trophies_TrophyId",
                table: "TeamTrophies",
                column: "TrophyId",
                principalTable: "Trophies",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayerTrophies_Players_PlayerId",
                table: "PlayerTrophies");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerTrophies_Trophies_TrophyId",
                table: "PlayerTrophies");

            migrationBuilder.DropForeignKey(
                name: "FK_TeamTrophies_Teams_TeamId",
                table: "TeamTrophies");

            migrationBuilder.DropForeignKey(
                name: "FK_TeamTrophies_Trophies_TrophyId",
                table: "TeamTrophies");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TeamTrophies",
                table: "TeamTrophies");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PlayerTrophies",
                table: "PlayerTrophies");

            migrationBuilder.RenameTable(
                name: "TeamTrophies",
                newName: "TeamTrophy");

            migrationBuilder.RenameTable(
                name: "PlayerTrophies",
                newName: "PlayerTrophy");

            migrationBuilder.RenameIndex(
                name: "IX_TeamTrophies_TrophyId",
                table: "TeamTrophy",
                newName: "IX_TeamTrophy_TrophyId");

            migrationBuilder.RenameIndex(
                name: "IX_TeamTrophies_TeamId",
                table: "TeamTrophy",
                newName: "IX_TeamTrophy_TeamId");

            migrationBuilder.RenameIndex(
                name: "IX_PlayerTrophies_TrophyId",
                table: "PlayerTrophy",
                newName: "IX_PlayerTrophy_TrophyId");

            migrationBuilder.RenameIndex(
                name: "IX_PlayerTrophies_PlayerId",
                table: "PlayerTrophy",
                newName: "IX_PlayerTrophy_PlayerId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TeamTrophy",
                table: "TeamTrophy",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PlayerTrophy",
                table: "PlayerTrophy",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerTrophy_Players_PlayerId",
                table: "PlayerTrophy",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerTrophy_Trophies_TrophyId",
                table: "PlayerTrophy",
                column: "TrophyId",
                principalTable: "Trophies",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TeamTrophy_Teams_TeamId",
                table: "TeamTrophy",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TeamTrophy_Trophies_TrophyId",
                table: "TeamTrophy",
                column: "TrophyId",
                principalTable: "Trophies",
                principalColumn: "Id");
        }
    }
}
